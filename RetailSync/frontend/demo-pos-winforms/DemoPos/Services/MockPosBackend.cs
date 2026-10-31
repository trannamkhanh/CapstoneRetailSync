using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using DemoPos.Data;
using DemoPos.Models;

namespace DemoPos.Services;

/// <summary>
/// Backend giả lập in-memory, giữ đúng luật của bản web frontend/demo-pos (src/services/posApi.js).
/// Tồn kho, đơn đã bán, số lượng đã trả và đơn đã hủy đều nằm trong RAM cho tới khi Reset.
/// </summary>
public sealed class MockPosBackend : IPosBackend
{
    private readonly object _gate = new();
    private readonly AppConfig _config;
    private readonly Random _random = new();
    private readonly List<Product> _products;

    private Dictionary<string, int> _stock = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, StoredTransaction> _transactions = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, List<PosEventItem>> _sold = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, List<PosEventItem>> _returned = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _cancelled = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _unavailableAttempts = new(StringComparer.OrdinalIgnoreCase);

    public MockPosBackend(AppConfig config)
    {
        _config = config;
        _products = ProductCatalog.CreateProducts().ToList();
        Reset();
    }

    public IReadOnlyList<Product> GetProducts()
    {
        lock (_gate)
        {
            return _products.Select(p => new Product
            {
                Sku = p.Sku,
                Barcode = p.Barcode,
                Name = p.Name,
                UnitPrice = p.UnitPrice,
                InitialStock = p.InitialStock,
                CurrentStock = _stock.TryGetValue(p.Sku, out var stock) ? stock : p.InitialStock
            }).ToList();
        }
    }

    /// <summary>Xóa tồn kho giả, lịch sử giao dịch, đơn đã bán/đã trả và đơn đã hủy.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            _stock = _products.ToDictionary(p => p.Sku, p => p.InitialStock, StringComparer.OrdinalIgnoreCase);
            _transactions = new Dictionary<string, StoredTransaction>(StringComparer.OrdinalIgnoreCase);
            _sold = new Dictionary<string, List<PosEventItem>>(StringComparer.OrdinalIgnoreCase);
            _returned = new Dictionary<string, List<PosEventItem>>(StringComparer.OrdinalIgnoreCase);
            _cancelled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _unavailableAttempts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public async Task<PosResult> SendAsync(PosEvent posEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(posEvent);

        var stopwatch = Stopwatch.StartNew();
        var delay = Random.Shared.Next(50, 301); // độ trễ giả 50–300 ms
        try
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new PosResult
            {
                Outcome = PosOutcome.ERROR,
                Code = "CANCELLED",
                Message = "Giao dịch đã bị hủy.",
                LatencyMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                HttpStatus = 0
            };
        }

        var result = Process(posEvent);
        result.LatencyMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);
        return result;
    }

    /// <summary>Xử lý một sự kiện theo đúng luật của bản web (giữ nguyên thứ tự kiểm tra).</summary>
    private PosResult Process(PosEvent posEvent)
    {
        lock (_gate)
        {
            var signature = CanonicalSignature(posEvent);

            // 1) Idempotency: trùng transaction_id.
            if (_transactions.TryGetValue(posEvent.TransactionId, out var existing))
            {
                return existing.Signature == signature
                    ? Make(PosOutcome.DUPLICATE, "DUPLICATE",
                        "Giao dịch đã được ghi nhận trước đó (idempotent), tồn kho không đổi.",
                        existing.CurrentStock, 200)
                    : Make(PosOutcome.REJECTED, "CONFLICT",
                        "Transaction ID đã tồn tại nhưng nội dung giao dịch khác.",
                        existing.CurrentStock, 409);
            }

            // 2) Token sai (chứa "invalid") => 401 UNAUTHORIZED (web trả outcome ERROR).
            if (posEvent.Token.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            {
                return Make(PosOutcome.ERROR, "UNAUTHORIZED",
                    "Token POS không hợp lệ. Kiểm tra JWT của terminal đang chọn.", null, 401);
            }

            // 3) Mô phỏng 503: lần đầu với transaction_id này lỗi, lần gửi lại sẽ thành công.
            if (posEvent.ForceUnavailable || posEvent.Token.Contains("unavailable", StringComparison.OrdinalIgnoreCase))
            {
                if (_unavailableAttempts.Add(posEvent.TransactionId))
                {
                    return Make(PosOutcome.ERROR, "SERVICE_UNAVAILABLE",
                        "Dịch vụ tạm thời không khả dụng. Giữ nguyên giỏ hàng và thử gửi lại.", null, 503);
                }
            }

            // 4) Terminal lạ => 403, token không khớp terminal => 401 (validateTerminal của web).
            var terminal = _config.FindTerminal(posEvent.StoreId, posEvent.PosId);
            if (terminal is null)
            {
                return Make(PosOutcome.REJECTED, "TERMINAL_NOT_REGISTERED",
                    "Store/POS chưa được đăng ký trong cấu hình terminal.", null, 403);
            }

            if (!string.Equals(terminal.Token, posEvent.Token, StringComparison.Ordinal))
            {
                return Make(PosOutcome.REJECTED, "UNAUTHORIZED",
                    "Token của POS không hợp lệ hoặc không thuộc terminal đang chọn.", null, 401);
            }

            // 5) Kiểm tra dòng hàng (CANCELLATION không cần items).
            if (!string.Equals(posEvent.EventType, "CANCELLATION", StringComparison.OrdinalIgnoreCase))
            {
                var itemsError = ValidateItems(posEvent.Items);
                if (itemsError is not null)
                {
                    return itemsError;
                }
            }

            return posEvent.EventType.ToUpperInvariant() switch
            {
                "SALE" => ProcessSale(posEvent, signature),
                "RETURN" => ProcessReturn(posEvent, signature),
                "CANCELLATION" => ProcessCancellation(posEvent, signature),
                _ => Make(PosOutcome.REJECTED, "INVALID_EVENT_TYPE",
                    "Loại sự kiện POS không hợp lệ.", null, 422)
            };
        }
    }

    private PosResult ProcessSale(PosEvent posEvent, string signature)
    {
        var insufficient = posEvent.Items.FirstOrDefault(item => item.Quantity > _stock[item.Sku]);
        if (insufficient is not null)
        {
            return Make(PosOutcome.REJECTED, "INSUFFICIENT_STOCK",
                $"Không đủ tồn kho cho SKU {insufficient.Sku}.",
                CurrentStockFor(posEvent.Items.Select(i => i.Sku)), 409);
        }

        foreach (var item in posEvent.Items)
        {
            _stock[item.Sku] -= item.Quantity;
        }

        _sold[posEvent.TransactionId] = posEvent.Items
            .Select(i => new PosEventItem { Sku = i.Sku, Quantity = i.Quantity, UnitPrice = i.UnitPrice })
            .ToList();

        var currentStock = CurrentStockFor(posEvent.Items.Select(i => i.Sku));
        _transactions[posEvent.TransactionId] = new StoredTransaction(signature, currentStock);

        return Make(PosOutcome.OK, "SALE_ACCEPTED", "Giao dịch thành công.", currentStock, 201);
    }

    private PosResult ProcessReturn(PosEvent posEvent, string signature)
    {
        var originalId = posEvent.OriginalTransactionId;
        if (string.IsNullOrWhiteSpace(originalId) ||
            !_transactions.ContainsKey(originalId) ||
            !_sold.ContainsKey(originalId))
        {
            return Make(PosOutcome.REJECTED, "ORIGINAL_NOT_FOUND",
                "Không tìm thấy đơn bán gốc hoặc đơn gốc chưa được ghi nhận.", null, 404);
        }

        if (_cancelled.Contains(originalId))
        {
            return Make(PosOutcome.REJECTED, "ALREADY_CANCELLED",
                "Đơn gốc đã bị hủy, không thể trả hàng.", null, 409);
        }

        var soldItems = _sold[originalId];
        var priorReturns = _returned.TryGetValue(originalId, out var returns) ? returns : new List<PosEventItem>();

        foreach (var item in posEvent.Items)
        {
            var sold = soldItems.FirstOrDefault(entry => entry.Sku == item.Sku);
            var alreadyReturned = priorReturns
                .Where(entry => entry.Sku == item.Sku)
                .Sum(entry => entry.Quantity);

            if (sold is null || item.Quantity > sold.Quantity - alreadyReturned)
            {
                return Make(PosOutcome.REJECTED, "RETURN_EXCEEDS_SOLD",
                    $"Số lượng trả vượt quá số lượng còn có thể trả của SKU {item.Sku}.", null, 409);
            }
        }

        foreach (var item in posEvent.Items)
        {
            _stock[item.Sku] += item.Quantity;
            priorReturns.Add(new PosEventItem { Sku = item.Sku, Quantity = item.Quantity, UnitPrice = item.UnitPrice });
        }

        _returned[originalId] = priorReturns;
        var currentStock = CurrentStockFor(posEvent.Items.Select(i => i.Sku));
        _transactions[posEvent.TransactionId] = new StoredTransaction(signature, currentStock);

        return Make(PosOutcome.OK, "RETURN_ACCEPTED", "Trả hàng thành công.", currentStock, 201);
    }

    private PosResult ProcessCancellation(PosEvent posEvent, string signature)
    {
        var originalId = posEvent.OriginalTransactionId;
        if (string.IsNullOrWhiteSpace(originalId) ||
            !_transactions.ContainsKey(originalId) ||
            !_sold.ContainsKey(originalId))
        {
            return Make(PosOutcome.REJECTED, "ORIGINAL_NOT_FOUND",
                "Không tìm thấy đơn bán gốc hoặc đơn gốc chưa được ghi nhận.", null, 404);
        }

        if (_cancelled.Contains(originalId))
        {
            return Make(PosOutcome.REJECTED, "ALREADY_CANCELLED",
                "Đơn gốc đã được hủy trước đó.", null, 409);
        }

        var soldItems = _sold[originalId];
        var priorReturns = _returned.TryGetValue(originalId, out var returns) ? returns : new List<PosEventItem>();

        foreach (var item in soldItems)
        {
            var alreadyReturned = priorReturns
                .Where(entry => entry.Sku == item.Sku)
                .Sum(entry => entry.Quantity);
            _stock[item.Sku] += item.Quantity - alreadyReturned;
        }

        _cancelled.Add(originalId);
        var currentStock = CurrentStockFor(soldItems.Select(i => i.Sku));
        _transactions[posEvent.TransactionId] = new StoredTransaction(signature, currentStock);

        return Make(PosOutcome.OK, "CANCELLATION_ACCEPTED", "Hủy đơn thành công.", currentStock, 201);
    }

    private PosResult? ValidateItems(IReadOnlyList<PosEventItem> items)
    {
        if (items.Count == 0)
        {
            return Make(PosOutcome.REJECTED, "INVALID_ITEMS",
                "Giao dịch phải có ít nhất một dòng hàng.", null, 422);
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Sku) || !_stock.ContainsKey(item.Sku))
            {
                return Make(PosOutcome.REJECTED, "SKU_NOT_FOUND",
                    $"SKU {(string.IsNullOrWhiteSpace(item.Sku) ? "(trống)" : item.Sku)} không tồn tại trong danh mục.",
                    null, 422);
            }

            if (item.Quantity <= 0 || item.UnitPrice < 0)
            {
                return Make(PosOutcome.REJECTED, "INVALID_QUANTITY",
                    "Số lượng hoặc giá của một dòng hàng không hợp lệ.", null, 422);
            }
        }

        return null;
    }

    private Dictionary<string, int> CurrentStockFor(IEnumerable<string> skus)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var sku in skus)
        {
            result[sku] = _stock.TryGetValue(sku, out var stock) ? stock : 0;
        }

        return result;
    }

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

    private sealed record StoredTransaction(string Signature, Dictionary<string, int> CurrentStock);
}
