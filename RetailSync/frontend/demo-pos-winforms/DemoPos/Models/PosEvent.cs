using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DemoPos.Models;

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
	public List<PosEventItem> Items { get; set; } = new List<PosEventItem>();

	[JsonPropertyName("timestamp")]
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;

	[JsonIgnore]
	public string Token { get; set; } = string.Empty;

	[JsonIgnore]
	public bool ForceUnavailable { get; set; }
}
