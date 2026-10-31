using System.Text.Json.Serialization;

namespace DemoPos.Models;

/// <summary>Sự kiện POS gửi tới Middleware (POST {BaseUrl}/pos/events).</summary>
public sealed class PosEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = "SALE";

    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("original_transaction_id")]
    public string? OriginalTransactionId { get; set; }

    [JsonPropertyName("store_id")]
    public string StoreId { get; set; } = string.Empty;

    [JsonPropertyName("pos_id")]
    public string PosId { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<PosEventItem> Items { get; set; } = new();

    /// <summary>ISO 8601 UTC — mặc định DateTimeKind.Utc để serialize thành "...Z".</summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Chỉ dùng cho header Authorization: Bearer (không nằm trong body).</summary>
    [JsonIgnore]
    public string Token { get; set; } = string.Empty;

    /// <summary>Cờ riêng cho Mock: buộc trả 503 ở lần gửi đầu tiên (dùng ở Dev Tools).</summary>
    [JsonIgnore]
    public bool ForceUnavailable { get; set; }
}

public sealed class PosEventItem
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }
}
