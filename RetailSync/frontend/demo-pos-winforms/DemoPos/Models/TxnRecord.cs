namespace DemoPos.Models;

/// <summary>Một dòng lịch sử giao dịch trong phiên làm việc.</summary>
public sealed class TxnRecord
{
    public DateTime Time { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;

    public string? OriginalTransactionId { get; set; }

    public string StoreId { get; set; } = string.Empty;

    public string PosId { get; set; } = string.Empty;

    public PosOutcome Outcome { get; set; }

    public string? Code { get; set; }

    public double LatencyMs { get; set; }

    public List<PosEventItem> Items { get; set; } = new();

    public string TypeLabel => EventType switch
    {
        "SALE" => "Bán",
        "RETURN" => "Trả",
        "CANCELLATION" => "Hủy",
        _ => EventType
    };
}
