namespace DemoPos.Models;

/// <summary>Chế độ backend POS.</summary>
public enum BackendMode
{
    /// <summary>Mock in-memory (cho demo/test, mất dữ liệu khi tắt app).</summary>
    Mock,

    /// <summary>SQLite local database (persist dữ liệu thật vào pos.db).</summary>
    SQLite,

    /// <summary>FastAPI Middleware qua HTTP.</summary>
    Api
}

/// <summary>Một terminal POS (role = POS, JWT cấp sẵn, không có login).</summary>
public sealed class TerminalConfig
{
    public string StoreId { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string PosId { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string DisplayName => $"{StoreName} · {PosId}";
}