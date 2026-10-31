using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DemoPos.Data;
using DemoPos.Models;

namespace DemoPos.Services;

public sealed class MockPosBackend : IPosBackend
{
	private sealed record StoredTransaction(string Signature, Dictionary<string, int> CurrentStock);

	private readonly object _gate = new object();

	private readonly AppConfig _config;

	private readonly Random _random = new Random();

	private readonly List<Product> _products;

	private Dictionary<string, int> _stock = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, StoredTransaction> _transactions = new Dictionary<string, StoredTransaction>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, List<PosEventItem>> _sold = new Dictionary<string, List<PosEventItem>>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, List<PosEventItem>> _returned = new Dictionary<string, List<PosEventItem>>(StringComparer.OrdinalIgnoreCase);

	private HashSet<string> _cancelled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private HashSet<string> _unavailableAttempts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public MockPosBackend(AppConfig config)
	{
		_config = config;
		_products = ProductCatalog.CreateProducts().ToList();
		Reset();
	}

	public IReadOnlyList<Product> GetProducts()
	{
		lock (_gate)
		{
			return _products.Select((Product p) => new Product
			{
				Sku = p.Sku,
				Barcode = p.Barcode,
				Name = p.Name,
				UnitPrice = p.UnitPrice,
				InitialStock = p.InitialStock,
				CurrentStock = (_stock.TryGetValue(p.Sku, out var value) ? value : p.InitialStock)
			}).ToList();
		}
	}

	public void Reset()
	{
		lock (_gate)
		{
			_stock = _products.ToDictionary((Product p) => p.Sku, (Product p) => p.InitialStock, StringComparer.OrdinalIgnoreCase);
			_transactions = new Dictionary<string, StoredTransaction>(StringComparer.OrdinalIgnoreCase);
			_sold = new Dictionary<string, List<PosEventItem>>(StringComparer.OrdinalIgnoreCase);
			_returned = new Dictionary<string, List<PosEventItem>>(StringComparer.OrdinalIgnoreCase);
			_cancelled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			_unavailableAttempts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	public async Task<PosResult> SendAsync(PosEvent posEvent, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentNullException.ThrowIfNull(posEvent, "posEvent");
		Stopwatch stopwatch = Stopwatch.StartNew();
		int delay = Random.Shared.Next(50, 301);
		try
		{
			await Task.Delay(delay, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
			stopwatch.Stop();
			return new PosResult
			{
				Outcome = PosOutcome.ERROR,
				Code = "CANCELLED",
				Message = "Giao dịch đã bị hủy.",
				LatencyMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
				HttpStatus = 0
			};
		}
		PosResult result = Process(posEvent);
		result.LatencyMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);
		return result;
	}

	private PosResult Process(PosEvent posEvent)
	{
		lock (_gate)
		{
			string text = CanonicalSignature(posEvent);
			if (_transactions.TryGetValue(posEvent.TransactionId, out StoredTransaction value))
			{
				return (value.Signature == text) ? Make(PosOutcome.DUPLICATE, "DUPLICATE", "Giao dịch đã được ghi nhận trước đó (idempotent), tồn kho không đổi.", value.CurrentStock, 200) : Make(PosOutcome.REJECTED, "CONFLICT", "Transaction ID đã tồn tại nhưng nội dung giao dịch khác.", value.CurrentStock, 409);
			}
			if (posEvent.Token.Contains("invalid", StringComparison.OrdinalIgnoreCase))
			{
				return Make(PosOutcome.ERROR, "UNAUTHORIZED", "Token POS không hợp lệ. Kiểm tra JWT của terminal đang chọn.", null, 401);
			}
			if ((posEvent.ForceUnavailable || posEvent.Token.Contains("unavailable", StringComparison.OrdinalIgnoreCase)) && _unavailableAttempts.Add(posEvent.TransactionId))
			{
				return Make(PosOutcome.ERROR, "SERVICE_UNAVAILABLE", "Dịch vụ tạm thời không khả dụng. Giữ nguyên giỏ hàng và thử gửi lại.", null, 503);
			}
			TerminalConfig terminalConfig = _config.FindTerminal(posEvent.StoreId, posEvent.PosId);
			if (terminalConfig == null)
			{
				return Make(PosOutcome.REJECTED, "TERMINAL_NOT_REGISTERED", "Store/POS chưa được đăng ký trong cấu hình terminal.", null, 403);
			}
			if (!string.Equals(terminalConfig.Token, posEvent.Token, StringComparison.Ordinal))
			{
				return Make(PosOutcome.REJECTED, "UNAUTHORIZED", "Token của POS không hợp lệ hoặc không thuộc terminal đang chọn.", null, 401);
			}
			if (!string.Equals(posEvent.EventType, "CANCELLATION", StringComparison.OrdinalIgnoreCase))
			{
				PosResult posResult = ValidateItems(posEvent.Items);
				if (posResult != null)
				{
					return posResult;
				}
			}
			string text2 = posEvent.EventType.ToUpperInvariant();
			if (1 == 0)
			{
			}
			PosResult result = text2 switch
			{
				"SALE" => ProcessSale(posEvent, text), 
				"RETURN" => ProcessReturn(posEvent, text), 
				"CANCELLATION" => ProcessCancellation(posEvent, text), 
				_ => Make(PosOutcome.REJECTED, "INVALID_EVENT_TYPE", "Loại sự kiện POS không hợp lệ.", null, 422), 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}

	private PosResult ProcessSale(PosEvent posEvent, string signature)
	{
		PosEventItem posEventItem = posEvent.Items.FirstOrDefault((PosEventItem item) => item.Quantity > _stock[item.Sku]);
		if (posEventItem != null)
		{
			return Make(PosOutcome.REJECTED, "INSUFFICIENT_STOCK", "Không đủ tồn kho cho SKU " + posEventItem.Sku + ".", CurrentStockFor(posEvent.Items.Select((PosEventItem i) => i.Sku)), 409);
		}
		foreach (PosEventItem item in posEvent.Items)
		{
			_stock[item.Sku] -= item.Quantity;
		}
		_sold[posEvent.TransactionId] = posEvent.Items.Select((PosEventItem i) => new PosEventItem
		{
			Sku = i.Sku,
			Quantity = i.Quantity,
			UnitPrice = i.UnitPrice
		}).ToList();
		Dictionary<string, int> currentStock = CurrentStockFor(posEvent.Items.Select((PosEventItem i) => i.Sku));
		_transactions[posEvent.TransactionId] = new StoredTransaction(signature, currentStock);
		return Make(PosOutcome.OK, "SALE_ACCEPTED", "Giao dịch thành công.", currentStock, 201);
	}

	private PosResult ProcessReturn(PosEvent posEvent, string signature)
	{
		string originalTransactionId = posEvent.OriginalTransactionId;
		if (string.IsNullOrWhiteSpace(originalTransactionId) || !_transactions.ContainsKey(originalTransactionId) || !_sold.ContainsKey(originalTransactionId))
		{
			return Make(PosOutcome.REJECTED, "ORIGINAL_NOT_FOUND", "Không tìm thấy đơn bán gốc hoặc đơn gốc chưa được ghi nhận.", null, 404);
		}
		if (_cancelled.Contains(originalTransactionId))
		{
			return Make(PosOutcome.REJECTED, "ALREADY_CANCELLED", "Đơn gốc đã bị hủy, không thể trả hàng.", null, 409);
		}
		List<PosEventItem> source = _sold[originalTransactionId];
		List<PosEventItem> list = (_returned.TryGetValue(originalTransactionId, out List<PosEventItem> value) ? value : new List<PosEventItem>());
		foreach (PosEventItem item in posEvent.Items)
		{
			PosEventItem posEventItem = source.FirstOrDefault((PosEventItem entry) => entry.Sku == item.Sku);
			int num = list.Where((PosEventItem entry) => entry.Sku == item.Sku).Sum((PosEventItem entry) => entry.Quantity);
			if (posEventItem == null || item.Quantity > posEventItem.Quantity - num)
			{
				return Make(PosOutcome.REJECTED, "RETURN_EXCEEDS_SOLD", "Số lượng trả vượt quá số lượng còn có thể trả của SKU " + item.Sku + ".", null, 409);
			}
		}
		foreach (PosEventItem item2 in posEvent.Items)
		{
			_stock[item2.Sku] += item2.Quantity;
			list.Add(new PosEventItem
			{
				Sku = item2.Sku,
				Quantity = item2.Quantity,
				UnitPrice = item2.UnitPrice
			});
		}
		_returned[originalTransactionId] = list;
		Dictionary<string, int> currentStock = CurrentStockFor(posEvent.Items.Select((PosEventItem i) => i.Sku));
		_transactions[posEvent.TransactionId] = new StoredTransaction(signature, currentStock);
		return Make(PosOutcome.OK, "RETURN_ACCEPTED", "Trả hàng thành công.", currentStock, 201);
	}

	private PosResult ProcessCancellation(PosEvent posEvent, string signature)
	{
		string originalTransactionId = posEvent.OriginalTransactionId;
		if (string.IsNullOrWhiteSpace(originalTransactionId) || !_transactions.ContainsKey(originalTransactionId) || !_sold.ContainsKey(originalTransactionId))
		{
			return Make(PosOutcome.REJECTED, "ORIGINAL_NOT_FOUND", "Không tìm thấy đơn bán gốc hoặc đơn gốc chưa được ghi nhận.", null, 404);
		}
		if (_cancelled.Contains(originalTransactionId))
		{
			return Make(PosOutcome.REJECTED, "ALREADY_CANCELLED", "Đơn gốc đã được hủy trước đó.", null, 409);
		}
		List<PosEventItem> list = _sold[originalTransactionId];
		List<PosEventItem> source = (_returned.TryGetValue(originalTransactionId, out List<PosEventItem> value) ? value : new List<PosEventItem>());
		foreach (PosEventItem item in list)
		{
			int num = source.Where((PosEventItem entry) => entry.Sku == item.Sku).Sum((PosEventItem entry) => entry.Quantity);
			_stock[item.Sku] += item.Quantity - num;
		}
		_cancelled.Add(originalTransactionId);
		Dictionary<string, int> currentStock = CurrentStockFor(list.Select((PosEventItem i) => i.Sku));
		_transactions[posEvent.TransactionId] = new StoredTransaction(signature, currentStock);
		return Make(PosOutcome.OK, "CANCELLATION_ACCEPTED", "Hủy đơn thành công.", currentStock, 201);
	}

	private PosResult? ValidateItems(IReadOnlyList<PosEventItem> items)
	{
		if (items.Count == 0)
		{
			return Make(PosOutcome.REJECTED, "INVALID_ITEMS", "Giao dịch phải có ít nhất một dòng hàng.", null, 422);
		}
		foreach (PosEventItem item in items)
		{
			if (string.IsNullOrWhiteSpace(item.Sku) || !_stock.ContainsKey(item.Sku))
			{
				return Make(PosOutcome.REJECTED, "SKU_NOT_FOUND", "SKU " + (string.IsNullOrWhiteSpace(item.Sku) ? "(trống)" : item.Sku) + " không tồn tại trong danh mục.", null, 422);
			}
			if (item.Quantity <= 0 || item.UnitPrice < 0m)
			{
				return Make(PosOutcome.REJECTED, "INVALID_QUANTITY", "Số lượng hoặc giá của một dòng hàng không hợp lệ.", null, 422);
			}
		}
		return null;
	}

	private Dictionary<string, int> CurrentStockFor(IEnumerable<string> skus)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (string sku in skus)
		{
			dictionary[sku] = (_stock.TryGetValue(sku, out var value) ? value : 0);
		}
		return dictionary;
	}

	private static string CanonicalSignature(PosEvent posEvent)
	{
		return JsonSerializer.Serialize(new
		{
			event_type = posEvent.EventType,
			transaction_id = posEvent.TransactionId,
			original_transaction_id = posEvent.OriginalTransactionId,
			store_id = posEvent.StoreId,
			pos_id = posEvent.PosId,
			items = posEvent.Items.Select((PosEventItem item) => new
			{
				sku = item.Sku,
				quantity = item.Quantity,
				unit_price = item.UnitPrice
			}).ToList()
		});
	}

	private static PosResult Make(PosOutcome outcome, string? code, string message, Dictionary<string, int>? currentStock, int httpStatus)
	{
		return new PosResult
		{
			Outcome = outcome,
			Code = code,
			Message = message,
			CurrentStock = currentStock,
			LatencyMs = 0.0,
			HttpStatus = httpStatus
		};
	}
}
