using System;
using System.Collections.Generic;

namespace DemoPos.Models;

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

	public List<PosEventItem> Items { get; set; } = new List<PosEventItem>();

	public string TypeLabel
	{
		get
		{
			string eventType = EventType;
			if (1 == 0)
			{
			}
			string result = eventType switch
			{
				"SALE" => "Bán", 
				"RETURN" => "Trả", 
				"CANCELLATION" => "Hủy", 
				_ => EventType, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}
}
