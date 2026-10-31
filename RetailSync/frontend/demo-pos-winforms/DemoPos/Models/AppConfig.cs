namespace DemoPos.Models;

/// <summary>Cấu hình ứng dụng đọc từ appsettings.json.</summary>
public sealed class AppConfig
{
    public string BaseUrl { get; set; } = "http://localhost:8000/api/v1";

    public bool UseMock { get; set; } = true;

    public List<TerminalConfig> Terminals { get; set; } = new();

    public TerminalConfig? FindTerminal(string storeId, string posId) =>
        Terminals.FirstOrDefault(t =>
            string.Equals(t.StoreId, storeId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.PosId, posId, StringComparison.OrdinalIgnoreCase));
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
