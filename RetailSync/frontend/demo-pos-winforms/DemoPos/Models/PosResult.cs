using System.Collections.Generic;

namespace DemoPos.Models;

public sealed class PosResult
{
	public PosOutcome Outcome { get; set; }

	public string? Code { get; set; }

	public string Message { get; set; } = string.Empty;

	public Dictionary<string, int>? CurrentStock { get; set; }

	public double LatencyMs { get; set; }

	public int HttpStatus { get; set; }

	public bool CanRetry
	{
		get
		{
			bool flag = HttpStatus == 503 || HttpStatus == 0;
			bool flag2 = flag;
			if (!flag2)
			{
				bool flag3;
				switch (Code)
				{
				case "SERVICE_UNAVAILABLE":
				case "TIMEOUT":
				case "NETWORK_ERROR":
					flag3 = true;
					break;
				default:
					flag3 = false;
					break;
				}
				flag2 = flag3;
			}
			return flag2;
		}
	}
}
