using DemoPos.Models;
using DemoPos.Services;

namespace DemoPos.SelfCheck;

/// <summary>
/// Kiểm tra nhanh backend SQLite bằng console (không mở UI).
/// Chạy: dotnet run -- selfcheck
/// </summary>
internal static class SqliteBackendSelfCheck
{
    public static async Task<int> RunAsync(AppConfig config)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"demopos-selfcheck-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        // Chẩn đoán: in ra những gì đã đọc được từ appsettings.json
        Console.WriteLine($"Config: BaseUrl={config.BaseUrl}, BackendMode={config.BackendMode}, Terminals={config.Terminals?.Count ?? 0}");
        Console.WriteLine($"Config file: {Program.ResolveConfigPathForDiagnostics() ?? "(không tìm thấy)"}");

        if (config.Terminals is null || config.Terminals.Count == 0)
        {
            Console.Error.WriteLine("AppConfig không có terminal nào — kiểm tra appsettings.json.");
            return 2;
        }

        var terminal = config.Terminals[0];

        using var backend = new SqlitePosBackend(config, connectionString);
        await backend.InitializeAsync();

        var failures = 0;

        void Check(string name, bool ok, string detail = "")
        {
            Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}{(ok || detail.Length == 0 ? string.Empty : $" — {detail}")}");
            if (!ok)
            {
                failures++;
            }
        }

        PosEvent NewEvent(string type, string txnId, string sku, int qty, string? originalId = null,
            string? token = null, string? storeId = null, string? posId = null) => new()
            {
                EventType = type,
                TransactionId = txnId,
                OriginalTransactionId = originalId,
                StoreId = storeId ?? terminal.StoreId,
                PosId = posId ?? terminal.PosId,
                Token = token ?? terminal.Token,
                Items = new List<PosEventItem>
                {
                    new() { Sku = sku, Quantity = qty, UnitPrice = 1000m }
                }
            };

        int Stock(string sku) => backend.GetProducts().First(p => p.Sku == sku).CurrentStock;

        Console.WriteLine("DemoPos — SQLite backend self-check");
        Console.WriteLine($"  DB file: {dbPath}");

        Check("Seed 10 sản phẩm", backend.GetProducts().Count == 10);

        var saleId = Guid.NewGuid().ToString();
        var before = Stock("VIN-MLK-1L");
        var sale = await backend.SendAsync(NewEvent("SALE", saleId, "VIN-MLK-1L", 3));
        Check("SALE trả OK", sale.Outcome == PosOutcome.OK, $"{sale.Outcome}/{sale.Code}");
        Check("SALE trừ tồn kho 3", Stock("VIN-MLK-1L") == before - 3, $"{before} -> {Stock("VIN-MLK-1L")}");

        var dup = await backend.SendAsync(NewEvent("SALE", saleId, "VIN-MLK-1L", 3));
        Check("Gửi lại giống hệt -> DUPLICATE", dup.Outcome == PosOutcome.DUPLICATE, $"{dup.Outcome}/{dup.Code}");
        Check("DUPLICATE không trừ kho", Stock("VIN-MLK-1L") == before - 3);

        var conflict = await backend.SendAsync(NewEvent("SALE", saleId, "VIN-MLK-1L", 4));
        Check("Sửa nội dung cùng ID -> CONFLICT", conflict.Code == "CONFLICT", $"{conflict.Outcome}/{conflict.Code}");

        var over = await backend.SendAsync(NewEvent("SALE", Guid.NewGuid().ToString(), "HH-TOM-01", 5));
        Check("Vượt tồn kho -> INSUFFICIENT_STOCK", over.Code == "INSUFFICIENT_STOCK", $"{over.Outcome}/{over.Code}");

        var badToken = await backend.SendAsync(NewEvent("SALE", Guid.NewGuid().ToString(), "VIN-MLK-1L", 1, token: "sai-token"));
        Check("Token sai -> UNAUTHORIZED/401", badToken.Code == "UNAUTHORIZED" && badToken.HttpStatus == 401,
            $"{badToken.Outcome}/{badToken.Code}/{badToken.HttpStatus}");

        var unknownTerminal = await backend.SendAsync(NewEvent("SALE", Guid.NewGuid().ToString(), "VIN-MLK-1L", 1, posId: "POS-99"));
        Check("Terminal lạ -> TERMINAL_NOT_REGISTERED/403",
            unknownTerminal.Code == "TERMINAL_NOT_REGISTERED" && unknownTerminal.HttpStatus == 403,
            $"{unknownTerminal.Outcome}/{unknownTerminal.Code}/{unknownTerminal.HttpStatus}");

        var ret = await backend.SendAsync(NewEvent("RETURN", Guid.NewGuid().ToString(), "VIN-MLK-1L", 2, saleId));
        Check("RETURN 2 -> OK", ret.Outcome == PosOutcome.OK, $"{ret.Outcome}/{ret.Code}");
        Check("RETURN hoàn kho 2", Stock("VIN-MLK-1L") == before - 1, $"{Stock("VIN-MLK-1L")}");

        var overReturn = await backend.SendAsync(NewEvent("RETURN", Guid.NewGuid().ToString(), "VIN-MLK-1L", 5, saleId));
        Check("Trả vượt số đã bán -> INVALID_RETURN_QUANTITY", overReturn.Code == "INVALID_RETURN_QUANTITY",
            $"{overReturn.Outcome}/{overReturn.Code}");

        var cancel = await backend.SendAsync(NewEvent("CANCELLATION", Guid.NewGuid().ToString(), "VIN-MLK-1L", 0, saleId));
        Check("CANCELLATION -> OK", cancel.Outcome == PosOutcome.OK, $"{cancel.Outcome}/{cancel.Code}");
        Check("CANCELLATION hoàn nốt 1 sản phẩm", Stock("VIN-MLK-1L") == before, $"{Stock("VIN-MLK-1L")}");

        var cancelAgain = await backend.SendAsync(NewEvent("CANCELLATION", Guid.NewGuid().ToString(), "VIN-MLK-1L", 0, saleId));
        Check("Hủy lần 2 -> ALREADY_CANCELLED", cancelAgain.Code == "ALREADY_CANCELLED", $"{cancelAgain.Outcome}/{cancelAgain.Code}");

        // Persistence: mở lại DB bằng instance mới, trạng thái phải còn nguyên.
        using (var reopened = new SqlitePosBackend(config, connectionString))
        {
            await reopened.InitializeAsync();
            Check("Dữ liệu còn sau khi mở lại DB", reopened.GetProducts().First(p => p.Sku == "VIN-MLK-1L").CurrentStock == before);
        }

        backend.Reset();
        Check("Reset đưa tồn kho về ban đầu",
            backend.GetProducts().First(p => p.Sku == "VIN-MLK-1L").CurrentStock
            == backend.GetProducts().First(p => p.Sku == "VIN-MLK-1L").InitialStock);

        try
        {
            File.Delete(dbPath);
        }
        catch
        {
            // Bỏ qua nếu file còn bị khóa.
        }

        Console.WriteLine(failures == 0
            ? "==> TẤT CẢ KIỂM TRA ĐỀU PASS"
            : $"==> CÓ {failures} KIỂM TRA FAIL");

        return failures == 0 ? 0 : 1;
    }
}