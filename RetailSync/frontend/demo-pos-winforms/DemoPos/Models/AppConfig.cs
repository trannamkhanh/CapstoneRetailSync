using System;
using System.Collections.Generic;
using System.Linq;

namespace DemoPos.Models;

/// <summary>Cấu hình ứng dụng đọc từ appsettings.json.</summary>
public sealed class AppConfig
{
    public string BaseUrl { get; set; } = "http://localhost:8000/api/v1";

    /// <summary>Chế độ backend (Mock/SQLite/Api). Mặc định: Mock.</summary>
    public BackendMode BackendMode { get; set; } = BackendMode.Mock;

    /// <summary>Connection string cho SQLite (mặc định: pos.db trong thư mục chạy).</summary>
    public string SqliteConnectionString { get; set; } = "Data Source=pos.db";

    public List<TerminalConfig> Terminals { get; set; } = new();

    public TerminalConfig? FindTerminal(string storeId, string posId) =>
        Terminals.FirstOrDefault(t =>
            string.Equals(t.StoreId, storeId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.PosId, posId, StringComparison.OrdinalIgnoreCase));
}
