using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DemoPos.Models;
using DemoPos.Services;

namespace DemoPos.SelfCheck;

internal static class SqliteBackendSelfCheck
{
	public static async Task<int> RunAsync(AppConfig config)
	{
		string dbPath = Path.Combine(Path.GetTempPath(), $"demopos-selfcheck-{Guid.NewGuid():N}.db");
		string connectionString = "Data Source=" + dbPath;
		Console.WriteLine($"Config: BaseUrl={config.BaseUrl}, BackendMode={config.BackendMode}, Terminals={config.Terminals?.Count ?? 0}");
		Console.WriteLine("Config file: " + (Program.ResolveConfigPathForDiagnostics() ?? "(không tìm thấy)"));
		if (config.Terminals == null || config.Terminals.Count == 0)
		{
			Console.Error.WriteLine("AppConfig không có terminal nào — kiểm tra appsettings.json.");
			return 2;
		}
		TerminalConfig terminal = config.Terminals[0];
		SqlitePosBackend backend = new SqlitePosBackend(config, connectionString);
		int failures;
		try
		{
			await backend.InitializeAsync();
			failures = 0;
			Console.WriteLine("DemoPos — SQLite backend self-check");
			Console.WriteLine("  DB file: " + dbPath);
			Check("Seed 10 sản phẩm", backend.GetProducts().Count == 10);
			string saleId = Guid.NewGuid().ToString();
			int before = Stock("VIN-MLK-1L");
			PosResult sale = await backend.SendAsync(NewEvent("SALE", saleId, "VIN-MLK-1L", 3));
			Check("SALE trả OK", sale.Outcome == PosOutcome.OK, $"{sale.Outcome}/{sale.Code}");
			Check("SALE trừ tồn kho 3", Stock("VIN-MLK-1L") == before - 3, $"{before} -> {Stock("VIN-MLK-1L")}");
			PosResult dup = await backend.SendAsync(NewEvent("SALE", saleId, "VIN-MLK-1L", 3));
			Check("Gửi lại giống hệt -> DUPLICATE", dup.Outcome == PosOutcome.DUPLICATE, $"{dup.Outcome}/{dup.Code}");
			Check("DUPLICATE không trừ kho", Stock("VIN-MLK-1L") == before - 3);
			PosResult conflict = await backend.SendAsync(NewEvent("SALE", saleId, "VIN-MLK-1L", 4));
			Check("Sửa nội dung cùng ID -> CONFLICT", conflict.Code == "CONFLICT", $"{conflict.Outcome}/{conflict.Code}");
			PosResult over = await backend.SendAsync(NewEvent("SALE", Guid.NewGuid().ToString(), "HH-TOM-01", 5));
			Check("Vượt tồn kho -> INSUFFICIENT_STOCK", over.Code == "INSUFFICIENT_STOCK", $"{over.Outcome}/{over.Code}");
			PosResult badToken = await backend.SendAsync(NewEvent("SALE", Guid.NewGuid().ToString(), "VIN-MLK-1L", 1, null, "sai-token"));
			Check("Token sai -> UNAUTHORIZED/401", badToken.Code == "UNAUTHORIZED" && badToken.HttpStatus == 401, $"{badToken.Outcome}/{badToken.Code}/{badToken.HttpStatus}");
			PosResult unknownTerminal = await backend.SendAsync(NewEvent("SALE", Guid.NewGuid().ToString(), "VIN-MLK-1L", 1, null, null, null, "POS-99"));
			Check("Terminal lạ -> TERMINAL_NOT_REGISTERED/403", unknownTerminal.Code == "TERMINAL_NOT_REGISTERED" && unknownTerminal.HttpStatus == 403, $"{unknownTerminal.Outcome}/{unknownTerminal.Code}/{unknownTerminal.HttpStatus}");
			PosResult ret = await backend.SendAsync(NewEvent("RETURN", Guid.NewGuid().ToString(), "VIN-MLK-1L", 2, saleId));
			Check("RETURN 2 -> OK", ret.Outcome == PosOutcome.OK, $"{ret.Outcome}/{ret.Code}");
			Check("RETURN hoàn kho 2", Stock("VIN-MLK-1L") == before - 1, $"{Stock("VIN-MLK-1L")}");
			PosResult overReturn = await backend.SendAsync(NewEvent("RETURN", Guid.NewGuid().ToString(), "VIN-MLK-1L", 5, saleId));
			Check("Trả vượt số đã bán -> INVALID_RETURN_QUANTITY", overReturn.Code == "INVALID_RETURN_QUANTITY", $"{overReturn.Outcome}/{overReturn.Code}");
			PosResult cancel = await backend.SendAsync(NewEvent("CANCELLATION", Guid.NewGuid().ToString(), "VIN-MLK-1L", 0, saleId));
			Check("CANCELLATION -> OK", cancel.Outcome == PosOutcome.OK, $"{cancel.Outcome}/{cancel.Code}");
			Check("CANCELLATION hoàn nốt 1 sản phẩm", Stock("VIN-MLK-1L") == before, $"{Stock("VIN-MLK-1L")}");
			PosResult cancelAgain = await backend.SendAsync(NewEvent("CANCELLATION", Guid.NewGuid().ToString(), "VIN-MLK-1L", 0, saleId));
			Check("Hủy lần 2 -> ALREADY_CANCELLED", cancelAgain.Code == "ALREADY_CANCELLED", $"{cancelAgain.Outcome}/{cancelAgain.Code}");
			using (SqlitePosBackend reopened = new SqlitePosBackend(config, connectionString))
			{
				await reopened.InitializeAsync();
				Check("Dữ liệu còn sau khi mở lại DB", reopened.GetProducts().First((Product p) => p.Sku == "VIN-MLK-1L").CurrentStock == before);
			}
			backend.Reset();
			Check("Reset đưa tồn kho về ban đầu", backend.GetProducts().First((Product p) => p.Sku == "VIN-MLK-1L").CurrentStock == backend.GetProducts().First((Product p) => p.Sku == "VIN-MLK-1L").InitialStock);
			try
			{
				File.Delete(dbPath);
			}
			catch
			{
			}
			Console.WriteLine((failures == 0) ? "==> TẤT CẢ KIỂM TRA ĐỀU PASS" : $"==> CÓ {failures} KIỂM TRA FAIL");
			return (failures != 0) ? 1 : 0;
		}
		finally
		{
			if (backend != null)
			{
				((IDisposable)backend).Dispose();
			}
		}
		void Check(string name, bool ok, string detail = "")
		{
			Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}{((ok || detail.Length == 0) ? string.Empty : (" — " + detail))}");
			if (!ok)
			{
				failures++;
			}
		}
		PosEvent NewEvent(string type, string txnId, string sku, int qty, string? originalId = null, string? token = null, string? storeId = null, string? posId = null)
		{
			return new PosEvent
			{
				EventType = type,
				TransactionId = txnId,
				OriginalTransactionId = originalId,
				StoreId = (storeId ?? terminal.StoreId),
				PosId = (posId ?? terminal.PosId),
				Token = (token ?? terminal.Token),
				Items = new List<PosEventItem>
				{
					new PosEventItem
					{
						Sku = sku,
						Quantity = qty,
						UnitPrice = 1000m
					}
				}
			};
		}
		int Stock(string sku)
		{
			return backend.GetProducts().First((Product p) => p.Sku == sku).CurrentStock;
		}
	}
}
