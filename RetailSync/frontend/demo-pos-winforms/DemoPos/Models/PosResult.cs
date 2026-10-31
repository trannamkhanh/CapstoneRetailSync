namespace DemoPos.Models;

public enum PosOutcome
{
    OK,
    DUPLICATE,
    REJECTED,
    ERROR
}

/// <summary>Kết quả chuẩn hóa của một lần gọi Middleware / mock backend.</summary>
public sealed class PosResult
{
    public PosOutcome Outcome { get; set; }

    public string? Code { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>Tồn kho hiện tại theo SKU của đơn vừa gửi.</summary>
    public Dictionary<string, int>? CurrentStock { get; set; }

    public double LatencyMs { get; set; }

    public int HttpStatus { get; set; }

    /// <summary>ERROR do 503/timeout/mất kết nối thì cho phép gửi lại cùng transaction_id.</summary>
    public bool CanRetry => HttpStatus == 503
                            || HttpStatus == 0
                            || Code is "SERVICE_UNAVAILABLE" or "TIMEOUT" or "NETWORK_ERROR";
}
