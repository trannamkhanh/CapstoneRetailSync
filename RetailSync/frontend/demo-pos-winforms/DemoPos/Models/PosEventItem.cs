using System.Text.Json.Serialization;

namespace DemoPos.Models;

/// <summary>Một dòng hàng trong sự kiện POS (serialize thành sku/quantity/unit_price).</summary>
public sealed class PosEventItem
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }
}