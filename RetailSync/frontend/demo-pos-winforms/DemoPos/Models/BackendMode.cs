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