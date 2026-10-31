using DemoPos.Models;
using DemoPos.Services;
using Xunit;

namespace DemoPos.Tests;

/// <summary>
/// Kiểm thử luật nghiệp vụ của mock backend — phải khớp với bản web frontend/demo-pos.
/// </summary>
public sealed class MockPosBackendTests
{
    private const string StoreId = "S-01";
    private const string PosId = "POS-01";
    private const string Token = "demo-pos-jwt-store-01-pos-01";

    private static AppConfig BuildConfig() => new()
    {
        UseMock = true,
        BaseUrl = "http://localhost:8000/api/v1",
        Terminals = new List<TerminalConfig>
        {
            new()
            {
                StoreId = StoreId,
                StoreName = "Cửa hàng Nguyễn Văn A",
                PosId = PosId,
                Token = Token
            }
        }
    };

    private static PosEvent BuildSale(string transactionId, string sku = "VIN-MLK-1L", int quantity = 1, decimal unitPrice = 18500m) => new()
    {
        EventType = "SALE",
        TransactionId = transactionId,
        StoreId = StoreId,
        PosId = PosId,
        Token = Token,
        Timestamp = DateTime.UtcNow,
        Items = new List<PosEventItem>
        {
            new() { Sku = sku, Quantity = quantity, UnitPrice = unitPrice }
        }
    };

    private static PosEvent BuildReturn(string transactionId, string originalId, string sku, int quantity, decimal unitPrice) => new()
    {
        EventType = "RETURN",
        TransactionId = transactionId,
        OriginalTransactionId = originalId,
        StoreId = StoreId,
        PosId = PosId,
        Token = Token,
        Timestamp = DateTime.UtcNow,
        Items = new List<PosEventItem>
        {
            new() { Sku = sku, Quantity = quantity, UnitPrice = unitPrice }
        }
    };

    private static PosEvent BuildCancellation(string transactionId, string originalId) => new()
    {
        EventType = "CANCELLATION",
        TransactionId = transactionId,
        OriginalTransactionId = originalId,
        StoreId = StoreId,
        PosId = PosId,
        Token = Token,
        Timestamp = DateTime.UtcNow,
        Items = new List<PosEventItem>()
    };

    private static int Stock(MockPosBackend backend, string sku) =>
        backend.GetProducts().First(p => p.Sku == sku).CurrentStock;

    [Fact]
    public async Task Sale_Thanh_cong_Tru_ton_kho()
    {
        var backend = new MockPosBackend(BuildConfig());
        var before = Stock(backend, "VIN-MLK-1L");

        var result = await backend.SendAsync(BuildSale("txn-sale-1"));

        Assert.Equal(PosOutcome.OK, result.Outcome);
        Assert.Equal("SALE_ACCEPTED", result.Code);
        Assert.Equal(201, result.HttpStatus);
        Assert.Equal(before - 1, Stock(backend, "VIN-MLK-1L"));
        Assert.True(result.LatencyMs > 0);
    }

    [Fact]
    public async Task Sale_Trung_transaction_id_va_noi_dung_giong_nhau_Returns_DUPLICATE()
    {
        var backend = new MockPosBackend(BuildConfig());
        var posEvent = BuildSale("txn-dup-1");

        var first = await backend.SendAsync(posEvent);
        var stockAfterFirst = Stock(backend, "VIN-MLK-1L");

        var second = await backend.SendAsync(posEvent);

        Assert.Equal(PosOutcome.OK, first.Outcome);
        Assert.Equal(PosOutcome.DUPLICATE, second.Outcome);
        Assert.Equal("DUPLICATE", second.Code);
        Assert.Equal(200, second.HttpStatus);
        Assert.Equal(stockAfterFirst, Stock(backend, "VIN-MLK-1L"));
    }

    [Fact]
    public async Task Sale_Trung_transaction_id_nhung_noi_dung_khac_Returns_CONFLICT()
    {
        var backend = new MockPosBackend(BuildConfig());
        await backend.SendAsync(BuildSale("txn-conflict-1", quantity: 1));

        var changed = BuildSale("txn-conflict-1", quantity: 2);
        var result = await backend.SendAsync(changed);

        Assert.Equal(PosOutcome.REJECTED, result.Outcome);
        Assert.Equal("CONFLICT", result.Code);
        Assert.Equal(409, result.HttpStatus);
    }

    [Fact]
    public async Task Sale_Vuot_ton_kho_Returns_INSUFFICIENT_STOCK()
    {
        var backend = new MockPosBackend(BuildConfig());
        var before = Stock(backend, "HH-TOM-01");

        var result = await backend.SendAsync(BuildSale("txn-stock-1", "HH-TOM-01", 2, 5200m));

        Assert.Equal(PosOutcome.REJECTED, result.Outcome);
        Assert.Equal("INSUFFICIENT_STOCK", result.Code);
        Assert.Equal(before, Stock(backend, "HH-TOM-01"));
    }

    [Fact]
    public async Task Return_Hoan_ton_kho_va_khong_cho_tra_qua_so_luong_da_ban()
    {
        var backend = new MockPosBackend(BuildConfig());
        await backend.SendAsync(BuildSale("txn-orig-1", quantity: 2));
        var afterSale = Stock(backend, "VIN-MLK-1L");

        var rejected = await backend.SendAsync(BuildReturn("txn-ret-bad", "txn-orig-1", "VIN-MLK-1L", 3, 18500m));
        Assert.Equal(PosOutcome.REJECTED, rejected.Outcome);
        Assert.Equal("RETURN_EXCEEDS_SOLD", rejected.Code);

        var accepted = await backend.SendAsync(BuildReturn("txn-ret-ok", "txn-orig-1", "VIN-MLK-1L", 1, 18500m));
        Assert.Equal(PosOutcome.OK, accepted.Outcome);
        Assert.Equal("RETURN_ACCEPTED", accepted.Code);
        Assert.Equal(afterSale + 1, Stock(backend, "VIN-MLK-1L"));
    }

    [Fact]
    public async Task Cancellation_Hoan_ton_kho_va_khong_cho_huy_lan_hai()
    {
        var backend = new MockPosBackend(BuildConfig());
        await backend.SendAsync(BuildSale("txn-cancel-1", quantity: 2));
        var afterSale = Stock(backend, "VIN-MLK-1L");

        var first = await backend.SendAsync(BuildCancellation("txn-cancel-1-c", "txn-cancel-1"));
        Assert.Equal(PosOutcome.OK, first.Outcome);
        Assert.Equal("CANCELLATION_ACCEPTED", first.Code);
        Assert.Equal(afterSale + 2, Stock(backend, "VIN-MLK-1L"));

        var repeated = await backend.SendAsync(BuildCancellation("txn-cancel-1-c2", "txn-cancel-1"));
        Assert.Equal(PosOutcome.REJECTED, repeated.Outcome);
        Assert.Equal("ALREADY_CANCELLED", repeated.Code);
    }

    [Fact]
    public async Task Terminal_La_va_token_sai_Tra_403_va_401()
    {
        var backend = new MockPosBackend(BuildConfig());

        var unknownTerminal = BuildSale("txn-403");
        unknownTerminal.StoreId = "S-99";
        unknownTerminal.PosId = "POS-99";
        var forbidden = await backend.SendAsync(unknownTerminal);
        Assert.Equal(PosOutcome.REJECTED, forbidden.Outcome);
        Assert.Equal("TERMINAL_NOT_REGISTERED", forbidden.Code);
        Assert.Equal(403, forbidden.HttpStatus);

        var badToken = BuildSale("txn-401");
        badToken.Token = "invalid-token";
        var unauthorized = await backend.SendAsync(badToken);
        Assert.Equal(PosOutcome.ERROR, unauthorized.Outcome);
        Assert.Equal("UNAUTHORIZED", unauthorized.Code);
        Assert.Equal(401, unauthorized.HttpStatus);
    }

    [Fact]
    public async Task Forced_503_Cho_phep_gui_lai_cung_transaction_id()
    {
        var backend = new MockPosBackend(BuildConfig());
        var posEvent = BuildSale("txn-503");
        posEvent.ForceUnavailable = true;

        var first = await backend.SendAsync(posEvent);
        Assert.Equal(PosOutcome.ERROR, first.Outcome);
        Assert.Equal("SERVICE_UNAVAILABLE", first.Code);
        Assert.Equal(503, first.HttpStatus);
        Assert.True(first.CanRetry);

        var second = await backend.SendAsync(posEvent);
        Assert.Equal(PosOutcome.OK, second.Outcome);
    }

    [Fact]
    public async Task Reset_Khoi_phuc_ton_kho_va_lich_su()
    {
        var backend = new MockPosBackend(BuildConfig());
        await backend.SendAsync(BuildSale("txn-reset-1"));

        backend.Reset();

        Assert.Equal(24, Stock(backend, "VIN-MLK-1L"));
    }
}
