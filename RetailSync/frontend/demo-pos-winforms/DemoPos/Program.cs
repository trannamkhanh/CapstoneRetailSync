using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using DemoPos.Data;
using DemoPos.Models;
using DemoPos.SelfCheck;
using DemoPos.Services;

namespace DemoPos;

internal static class Program
{
	private static readonly JsonSerializerOptions ConfigJsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		Converters = { (JsonConverter)new JsonStringEnumConverter(null, allowIntegerValues: false) }
	};

	[STAThread]
	private static int Main(string[] args)
	{
		AppConfig config = LoadConfig();
		if (args.Any((string a) => string.Equals(a, "selfcheck", StringComparison.OrdinalIgnoreCase)))
		{
			return SqliteBackendSelfCheck.RunAsync(config).GetAwaiter().GetResult();
		}
		if (args.Any((string a) => string.Equals(a, "initdb", StringComparison.OrdinalIgnoreCase)))
		{
			string text = Path.Combine(AppContext.BaseDirectory, "pos.db");
			AppDbContext appDbContext = new AppDbContext("Data Source=" + text);
			try
			{
				appDbContext.EnsureCreatedAndSeedAsync(config).GetAwaiter().GetResult();
				appDbContext.Compact();
				Console.WriteLine($"Đã tạo database: {text} ({new FileInfo(text).Length} bytes)");
				return 0;
			}
			finally
			{
				((IDisposable)appDbContext)?.Dispose();
			}
		}
		ApplicationConfiguration.Initialize();
		Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
		IPosBackend posBackend = CreateBackend(config);
		if (posBackend is SqlitePosBackend sqlitePosBackend)
		{
			sqlitePosBackend.InitializeAsync().GetAwaiter().GetResult();
		}
		using MainForm mainForm = new MainForm(config, posBackend);
		Application.Run(mainForm);
		return 0;
	}

	public static IPosBackend CreateBackend(AppConfig config)
	{
		BackendMode backendMode = config.BackendMode;
		if (1 == 0)
		{
		}
		IPosBackend result = backendMode switch
		{
			BackendMode.SQLite => (IPosBackend)new SqlitePosBackend(config, config.SqliteConnectionString), 
			BackendMode.Api => new PosApiClient(config, config.BaseUrl), 
			_ => new MockPosBackend(config), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static AppConfig LoadConfig(string? path = null)
	{
		if (path == null)
		{
			path = ResolveConfigPath();
		}
		if (path == null || !File.Exists(path))
		{
			return new AppConfig();
		}
		try
		{
			string json = File.ReadAllText(path, Encoding.UTF8);
			AppConfig appConfig = JsonSerializer.Deserialize<AppConfig>(json, ConfigJsonOptions) ?? new AppConfig();
			AppConfig appConfig2 = appConfig;
			if (appConfig2.Terminals == null)
			{
				List<TerminalConfig> list = (appConfig2.Terminals = new List<TerminalConfig>());
			}
			return appConfig;
		}
		catch (JsonException ex)
		{
			Console.Error.WriteLine("appsettings.json không hợp lệ (" + path + "): " + ex.Message);
			return new AppConfig();
		}
	}

	internal static string? ResolveConfigPathForDiagnostics()
	{
		return ResolveConfigPath();
	}

	private static string? ResolveConfigPath()
	{
		string[] array = new string[3]
		{
			Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
			Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
			Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "appsettings.json")
		};
		string[] array2 = array;
		foreach (string path in array2)
		{
			string fullPath = Path.GetFullPath(path);
			if (File.Exists(fullPath))
			{
				return fullPath;
			}
		}
		return null;
	}
}
