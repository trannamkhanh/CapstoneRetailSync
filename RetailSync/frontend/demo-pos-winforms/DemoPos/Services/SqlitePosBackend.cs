using System.Diagnostics;
using System.Text.Json;
using DemoPos.Data;
using DemoPos.Data.Entities;
using DemoPos.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoPos.Services;

/// <summary>
/// Backend POS dùng SQLite (file pos.db) thay cho RAM.
/// Giữ nguyên hợp đồng IPosBackend và luật nghiệp vụ của MockPosBackend
/// nhưng mọi thay đổi tồn kho / giao dịch / trả hàng / hủy đơn đều được lưu xuống đĩa.
/// </summary>
public sealed class SqlitePosBackend : IPosBackend, IDisposable
{
    private readonly AppDbContext _db;
    private readonly AppConfig _config;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private bool _disposed;

    public SqlitePosBackend(AppConfig config, string? connectionString = null)
    {
        _config = config;

        // "Data Source=pos.db" là đường dẫn tương đối → neo vào thư mục exe để không phụ thuộc
        // thư mục làm việc (khi chạy từ shortcut, Task Scheduler hay Start-Process).
        var effective = string.IsNullOrWhiteSpace(connectionString)
            ? AppDbContext.DefaultConnectionString
            : ResolveConnectionString(connectionString);

        _db = new AppDbContext(effective);
    }

    /// <summary>Chuyển "Data Source=pos.db" thành đường dẫn tuyệt đối cạnh file exe.</summary>
    internal static string ResolveConnectionString(string connectionString)
    {
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(builder.DataSource) && !Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);
        }

        return builder.ToString();
    }

    /// <summary>Đường dẫn file database đang dùng.</summary>
    public string DatabasePath => _db.Database.GetDbConnection().DataSource;

    /// <summary>Tạo database + seed dữ liệu (gọi 1 lần lúc khởi động).</summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        _db.EnsureCreatedAndSeedAsync(_config, cancellationToken);

    public IReadOnlyList<Product> GetProducts()
    {
        _mutex.Wait();
        try
        {
            return _db.Products
                .AsNoTracking()
                .OrderBy(p => p.Sku)
                .Select(p => new Product
                {
                    Sku = p.Sku,
                    Barcode = p.Barcode,
                    Name = p.Name,
                    UnitPrice = p.UnitPrice,
                    InitialStock = p.InitialStock,
                    CurrentStock = p.CurrentStock
                })
                .ToList();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<PosResult> SendAsync(PosEvent posEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(posEvent);

        var stopwatch = Stopwatch.StartNew();
        var requestJson = JsonSerializer.Serialize(posEvent);
        PosResult result;

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            result = await ProcessEventAsync(posEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result = new PosResult
            {
                Outcome = PosOutcome.ERROR,
                Code = "CANCELLED",
                Message = "Giao dịch đã bị hủy.",
                HttpStatus = 0
            };
        }
        catch (Exception ex)
        {
            result = new PosResult
            {
                Outcome = PosOutcome.ERROR,
                Code = "INTERNAL_ERROR",
                Message = $"Lỗi lưu database: {ex.Message}",
                HttpStatus = 500
            };
        }
        finally
        {
            _mutex.Release();
        }

        stopwatch.Stop();
        result.LatencyMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);

        await LogEventAsync(posEvent, result, requestJson, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>Xóa toàn bộ giao dịch và đưa tồn kho về giá trị ban đầu (chỉ hoạt động với backend SQLite).</summary>
    public void Reset()
    {
        _mutex.Wait();
        try
        {
            _db.SoldItems.ExecuteDelete();
            _db.ReturnedItems.ExecuteDelete();
            _db.CancelledTransactions.ExecuteDelete();
            _db.Transactions.ExecuteDelete();
            _db.EventLogs.ExecuteDelete();

            foreach (var product in _db.Products)
            {
                product.CurrentStock = product.InitialStock;
                product.UpdatedAt = DateTime.UtcNow;
            }

            _db.SaveChanges();
        }
        finally
        {
            _mutex.Release();
        }
    }

    // ---------------------------------------------------------------------
    // Xử lý nghiệp vụ (giữ đúng luật của MockPosBackend / middleware)
    // ---------------------------------------------------------------------

    private async Task<PosResult> ProcessEventAsync(PosEvent posEvent, CancellationToken ct)
    {
        var terminal = await _db.Terminals
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.StoreId == posEvent.StoreId && t.PosId == posEvent.PosId, ct);

        if (terminal is null || !terminal.IsActive)
        {
            return Make(PosOutcome.REJECTED, "TERMINAL_NOT_REGISTERED",
                "Store/POS chưa được đăng ký trong cấu hình terminal.", null, 403);
        }

        if (!string.Equals(terminal.Token, posEvent.Token, StringComparison.Ordinal))
        {
            return Make(PosOutcome.REJECTED, "UNAUTHORIZED",
                "Token POS không hợp lệ hoặc không thuộc terminal đang chọn.", null, 401);
        }

        // CANCELLATION không cần items (theo hợp đồng của MockPosBackend / middleware):
        // hủy đơn hoàn toàn dựa trên đơn gốc, nên bỏ qua kiểm tra dòng hàng.
        if (!string.Equals(posEvent.EventType, "CANCELLATION", StringComparison.OrdinalIgnoreCase))
        {
            var validation = await ValidateItemsAsync(posEvent.Items, ct);
            if (validation is not null)
            {
                return validation;
            }
        }

        var signature = CanonicalSignature(posEvent);
        var existing = await _db.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TransactionId == posEvent.TransactionId, ct);

        if (existing is not null)
        {
            return string.Equals(existing.Signature, signature, StringComparison.Ordinal)
                ? Make(PosOutcome.DUPLICATE, "DUPLICATE",
                    "Giao dịch đã được ghi nhận trước đó (idempotent), tồn kho không đổi.",
                    existing.CurrentStock, 200)
                : Make(PosOutcome.REJECTED, "CONFLICT",
                    "Transaction ID đã tồn tại nhưng nội dung giao dịch khác.", null, 409);
        }

        return posEvent.EventType switch
        {
            "SALE" => await ProcessSaleAsync(posEvent, signature, ct),
            "RETURN" => await ProcessReturnAsync(posEvent, signature, ct),
            "CANCELLATION" => await ProcessCancellationAsync(posEvent, signature, ct),
            _ => Make(PosOutcome.REJECTED, "INVALID_EVENT",
                $"Event type '{posEvent.EventType}' không được hỗ trợ.", null, 422)
        };
    }

    private async Task<PosResult?> ValidateItemsAsync(IReadOnlyList<PosEventItem> items, CancellationToken ct)
    {
        if (items.Count == 0)
        {
            return Make(PosOutcome.REJECTED, "INVALID_ITEMS",
                "Giao dịch phải có ít nhất một dòng hàng.", null, 422);
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Sku))
            {
                return Make(PosOutcome.REJECTED, "SKU_NOT_FOUND",
                    "SKU không hợp lệ (trống).", null, 422);
            }

            var exists = await _db.Products.AsNoTracking().AnyAsync(p => p.Sku == item.Sku, ct);
            if (!exists)
            {
                return Make(PosOutcome.REJECTED, "SKU_NOT_FOUND",
                    $"SKU {item.Sku} không tồn tại trong danh mục.", null, 422);
            }

            if (item.Quantity <= 0 || item.UnitPrice < 0)
            {
                return Make(PosOutcome.REJECTED, "INVALID_QUANTITY",
                    "Số lượng hoặc giá của một dòng hàng không hợp lệ.", null, 422);
            }
        }

        return null;
    }

    private async Task<PosResult> ProcessSaleAsync(PosEvent posEvent, string signature, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        foreach (var item in posEvent.Items)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Sku == item.Sku, ct);
            if (product is null)
            {
                return Make(PosOutcome.REJECTED, "SKU_NOT_FOUND",
                    $"SKU {item.Sku} không tồn tại trong danh mục.", null, 422);
            }

            if (product.CurrentStock < item.Quantity)
            {
                var stockNow = await CurrentStockAsync(posEvent.Items.Select(i => i.Sku), ct);
                return Make(PosOutcome.REJECTED, "INSUFFICIENT_STOCK",
                    $"SKU {item.Sku} chỉ còn {product.CurrentStock}, yêu cầu {item.Quantity}.", stockNow, 422);
            }
        }

        foreach (var item in posEvent.Items)
        {
            var product = await _db.Products.FirstAsync(p => p.Sku == item.Sku, ct);
            product.CurrentStock -= item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            _db.SoldItems.Add(new SoldItemEntity
            {
                TransactionId = posEvent.TransactionId,
                Sku = item.Sku,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        await _db.SaveChangesAsync(ct);
        var currentStock = await CurrentStockAsync(posEvent.Items.Select(i => i.Sku), ct);

        _db.Transactions.Add(BuildTransaction(posEvent, signature, currentStock));
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Make(PosOutcome.OK, "SALE_ACCEPTED", "Ghi nhận bán hàng thành công.", currentStock, 201);
    }

    private async Task<PosResult> ProcessReturnAsync(PosEvent posEvent, string signature, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(posEvent.OriginalTransactionId))
        {
            return Make(PosOutcome.REJECTED, "ORIGINAL_NOT_FOUND",
                "Thiếu original_transaction_id cho giao dịch trả hàng.", null, 422);
        }

        var originalId = posEvent.OriginalTransactionId;
        var guard = await CheckOriginalSaleAsync(originalId, ct);
        if (guard is not null)
        {
            return guard;
        }

        var soldItems = await _db.SoldItems.AsNoTracking()
            .Where(s => s.TransactionId == originalId)
            .ToListAsync(ct);

        var alreadyReturned = await _db.ReturnedItems.AsNoTracking()
            .Where(r => r.OriginalTransactionId == originalId)
            .GroupBy(r => r.Sku)
            .Select(g => new { Sku = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Sku, x => x.Quantity, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var item in posEvent.Items)
        {
            var sold = soldItems.Where(s => string.Equals(s.Sku, item.Sku, StringComparison.OrdinalIgnoreCase))
                .Sum(s => s.Quantity);
            var returnedBefore = alreadyReturned.TryGetValue(item.Sku, out var qty) ? qty : 0;
            var remaining = sold - returnedBefore;

            if (remaining <= 0 || item.Quantity > remaining)
            {
                return Make(PosOutcome.REJECTED, "INVALID_RETURN_QUANTITY",
                    $"SKU {item.Sku}: chỉ còn {Math.Max(remaining, 0)} sản phẩm có thể trả.", null, 422);
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        foreach (var item in posEvent.Items)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Sku == item.Sku, ct);
            if (product is null)
            {
                return Make(PosOutcome.REJECTED, "SKU_NOT_FOUND",
                    $"SKU {item.Sku} không tồn tại trong danh mục.", null, 422);
            }

            product.CurrentStock += item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            _db.ReturnedItems.Add(new ReturnedItemEntity
            {
                OriginalTransactionId = originalId,
                Sku = item.Sku,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                ReturnTransactionId = posEvent.TransactionId
            });
        }

        await _db.SaveChangesAsync(ct);
        var currentStock = await CurrentStockAsync(posEvent.Items.Select(i => i.Sku), ct);

        _db.Transactions.Add(BuildTransaction(posEvent, signature, currentStock));
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Make(PosOutcome.OK, "RETURN_ACCEPTED", "Ghi nhận trả hàng thành công.", currentStock, 201);
    }

    private async Task<PosResult> ProcessCancellationAsync(PosEvent posEvent, string signature, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(posEvent.OriginalTransactionId))
        {
            return Make(PosOutcome.REJECTED, "ORIGINAL_NOT_FOUND",
                "Thiếu original_transaction_id cho giao dịch hủy đơn.", null, 422);
        }

        var originalId = posEvent.OriginalTransactionId;
        var guard = await CheckOriginalSaleAsync(originalId, ct);
        if (guard is not null)
        {
            return guard;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var soldItems = await _db.SoldItems.AsNoTracking()
            .Where(s => s.TransactionId == originalId)
            .ToListAsync(ct);

        var alreadyReturned = await _db.ReturnedItems.AsNoTracking()
            .Where(r => r.OriginalTransactionId == originalId)
            .GroupBy(r => r.Sku)
            .Select(g => new { Sku = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Sku, x => x.Quantity, StringComparer.OrdinalIgnoreCase, ct);

        // Hoàn tồn kho phần chưa được trả lại trước đó.
        foreach (var group in soldItems.GroupBy(s => s.Sku, StringComparer.OrdinalIgnoreCase))
        {
            var returnedBefore = alreadyReturned.TryGetValue(group.Key, out var qty) ? qty : 0;
            var remaining = group.Sum(s => s.Quantity) - returnedBefore;
            if (remaining <= 0)
            {
                continue;
            }

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Sku == group.Key, ct);
            if (product is null)
            {
                continue;
            }

            product.CurrentStock += remaining;
            product.UpdatedAt = DateTime.UtcNow;
        }

        _db.CancelledTransactions.Add(new CancelledTransactionEntity
        {
            TransactionId = originalId,
            CancellationTransactionId = posEvent.TransactionId
        });

        await _db.SaveChangesAsync(ct);
        var currentStock = await CurrentStockAsync(soldItems.Select(s => s.Sku), ct);

        _db.Transactions.Add(BuildTransaction(posEvent, signature, currentStock));
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Make(PosOutcome.OK, "CANCELLATION_ACCEPTED", "Hủy đơn thành công.", currentStock, 201);
    }

    /// <summary>Kiểm tra đơn gốc tồn tại và chưa bị hủy. Trả null nếu hợp lệ.</summary>
    private async Task<PosResult?> CheckOriginalSaleAsync(string originalId, CancellationToken ct)
    {
        var originalSale = await _db.Transactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TransactionId == originalId && t.EventType == "SALE", ct);

        if (originalSale is null)
        {
            return Make(PosOutcome.REJECTED, "ORIGINAL_NOT_FOUND",
                "Không tìm thấy đơn bán gốc hoặc đơn gốc chưa được ghi nhận.", null, 404);
        }

        var cancelled = await _db.CancelledTransactions.AsNoTracking()
            .AnyAsync(c => c.TransactionId == originalId, ct);

        return cancelled
            ? Make(PosOutcome.REJECTED, "ALREADY_CANCELLED", "Đơn gốc đã được hủy trước đó.", null, 409)
            : null;
    }

    /// <summary>Tồn kho hiện tại của các SKU (dùng ở phản hồi và trong bảng Transactions).</summary>
    private async Task<Dictionary<string, int>> CurrentStockAsync(IEnumerable<string> skus, CancellationToken ct)
    {
        var list = skus.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var rows = await _db.Products.AsNoTracking()
            .Where(p => list.Contains(p.Sku))
            .Select(p => new { p.Sku, p.CurrentStock })
            .ToListAsync(ct);

        var map = rows.ToDictionary(r => r.Sku, r => r.CurrentStock, StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var sku in list)
        {
            result[sku] = map.TryGetValue(sku, out var stock) ? stock : 0;
        }

        return result;
    }

    private static TransactionEntity BuildTransaction(PosEvent posEvent, string signature,
        Dictionary<string, int> currentStock) => new()
    {
        TransactionId = posEvent.TransactionId,
        StoreId = posEvent.StoreId,
        PosId = posEvent.PosId,
        EventType = posEvent.EventType,
        OriginalTransactionId = posEvent.OriginalTransactionId,
        Signature = signature,
        CurrentStock = currentStock,
        Items = posEvent.Items.Select(i => new PosEventItemEntity
        {
            Sku = i.Sku,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        Timestamp = posEvent.Timestamp,
        CreatedAt = DateTime.UtcNow
    };

    /// <summary>Chữ ký nội dung giao dịch (không tính timestamp) để so sánh idempotency.</summary>
    private static string CanonicalSignature(PosEvent posEvent) => JsonSerializer.Serialize(new
    {
        event_type = posEvent.EventType,
        transaction_id = posEvent.TransactionId,
        original_transaction_id = posEvent.OriginalTransactionId,
        store_id = posEvent.StoreId,
        pos_id = posEvent.PosId,
        items = posEvent.Items
            .Select(item => new { sku = item.Sku, quantity = item.Quantity, unit_price = item.UnitPrice })
            .ToList()
    });

    private static PosResult Make(PosOutcome outcome, string? code, string message,
        Dictionary<string, int>? currentStock, int httpStatus) => new()
    {
        Outcome = outcome,
        Code = code,
        Message = message,
        CurrentStock = currentStock,
        LatencyMs = 0,
        HttpStatus = httpStatus
    };

    /// <summary>Ghi audit log; lỗi logging không được làm hỏng kết quả giao dịch.</summary>
    private async Task LogEventAsync(PosEvent posEvent, PosResult result, string requestJson, CancellationToken ct)
    {
        try
        {
            _db.EventLogs.Add(new EventLogEntity
            {
                TransactionId = posEvent.TransactionId,
                EventType = posEvent.EventType,
                StoreId = posEvent.StoreId,
                PosId = posEvent.PosId,
                Outcome = result.Outcome.ToString(),
                Code = result.Code,
                Message = result.Message,
                HttpStatus = result.HttpStatus,
                LatencyMs = result.LatencyMs,
                RequestJson = requestJson,
                ResponseJson = JsonSerializer.Serialize(result)
            });

            await _mutex.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                _mutex.Release();
            }
        }
        catch
        {
            // Bỏ qua lỗi logging.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _db.Dispose();
        _mutex.Dispose();
    }
}