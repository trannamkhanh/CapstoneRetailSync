using System.Text.Json;
using System.Text.Json.Serialization;
using DemoPos.Models;
using DemoPos.Services;

namespace DemoPos;

internal static class Program
{
    private static readonly JsonSerializerOptions ConfigJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        var config = LoadConfig();
        IPosBackend backend = config.UseMock
            ? new MockPosBackend(config)
            : new PosApiClient(config, config.BaseUrl);

        using var form = new MainForm(config, backend);
        Application.Run(form);
    }

    /// <summary>Đọc appsettings.json (BaseUrl, UseMock, Terminals[6]).</summary>
    public static AppConfig LoadConfig(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return new AppConfig();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppConfig>(json, ConfigJsonOptions) ?? new AppConfig();
        }
        catch (JsonException ex)
        {
            MessageBox.Show($"appsettings.json không hợp lệ: {ex.Message}", "Demo POS Terminal",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return new AppConfig();
        }
    }
}
