using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DemoPos.Data;
using DemoPos.Models;

namespace DemoPos.Services;

public sealed class PosApiClient : IPosBackend, IDisposable
{
	public const string PosEventsEndpoint = "/pos/events";

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private readonly HttpClient _httpClient;

	private readonly object _gate = new object();

	private readonly List<Product> _products;

	private string _baseUrl;

	public string BaseUrl
	{
		get
		{
			return _baseUrl;
		}
		set
		{
			_baseUrl = NormalizeBaseUrl(value);
		}
	}

	public PosApiClient(AppConfig config, string? baseUrl = null)
	{
		_baseUrl = NormalizeBaseUrl(baseUrl ?? config.BaseUrl);
		_httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(10.0)
		};
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_products = ProductCatalog.CreateProducts().ToList();
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
				CurrentStock = p.CurrentStock
			}).ToList();
		}
	}

	public void Reset()
	{
		lock (_gate)
		{
			foreach (Product product in _products)
			{
				product.CurrentStock = product.InitialStock;
			}
		}
	}

	public async Task<PosResult> SendAsync(PosEvent posEvent, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentNullException.ThrowIfNull(posEvent, "posEvent");
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			string payload = JsonSerializer.Serialize(posEvent, JsonOptions);
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, BuildEventsUrl())
			{
				Content = new StringContent(payload, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", posEvent.Token);
			using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			stopwatch.Stop();
			PosResult result = NormalizePayload(body, (int)response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
			ApplyStock(result);
			return result;
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			stopwatch.Stop();
			return Error("TIMEOUT", "Dịch vụ tạm thời không khả dụng. Kiểm tra kết nối Middleware rồi gửi lại cùng transaction_id.", 0, stopwatch.Elapsed.TotalMilliseconds);
		}
		catch (OperationCanceledException)
		{
			stopwatch.Stop();
			return Error("CANCELLED", "Giao dịch đã bị hủy.", 0, stopwatch.Elapsed.TotalMilliseconds);
		}
		catch (HttpRequestException)
		{
			stopwatch.Stop();
			return Error("NETWORK_ERROR", "Dịch vụ tạm thời không khả dụng. Kiểm tra kết nối Middleware rồi gửi lại cùng transaction_id.", 0, stopwatch.Elapsed.TotalMilliseconds);
		}
		catch (Exception ex4)
		{
			Exception ex5 = ex4;
			stopwatch.Stop();
			return Error("NETWORK_ERROR", "Dịch vụ tạm thời không khả dụng. " + ex5.Message, 0, stopwatch.Elapsed.TotalMilliseconds);
		}
	}

	private string BuildEventsUrl()
	{
		return _baseUrl.TrimEnd('/') + "/pos/events";
	}

	private static string NormalizeBaseUrl(string? value)
	{
		return (value ?? string.Empty).Trim().TrimEnd('/');
	}

	private void ApplyStock(PosResult result)
	{
		if (result.CurrentStock == null || result.CurrentStock.Count == 0)
		{
			return;
		}
		lock (_gate)
		{
			foreach (Product product in _products)
			{
				if (result.CurrentStock.TryGetValue(product.Sku, out var value))
				{
					product.CurrentStock = value;
				}
			}
		}
	}

	public static PosResult NormalizePayload(string? body, int httpStatus, double elapsedMs)
	{
		Dictionary<string, int> currentStock = null;
		string outcome = null;
		string text = null;
		string text2 = null;
		if (!string.IsNullOrWhiteSpace(body))
		{
			try
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(body);
				JsonElement rootElement = jsonDocument.RootElement;
				if (rootElement.ValueKind == JsonValueKind.Object)
				{
					outcome = ReadString(rootElement, "outcome");
					text = ReadString(rootElement, "code") ?? ReadString(rootElement, "error_code");
					text2 = ReadString(rootElement, "message") ?? ReadString(rootElement, "detail");
					currentStock = ReadStock(rootElement, "currentStock") ?? ReadStock(rootElement, "current_stock");
				}
			}
			catch (JsonException)
			{
			}
		}
		PosOutcome outcome2 = ResolveOutcome(outcome, httpStatus);
		string code = text ?? ResolveCode(httpStatus);
		string message = text2 ?? ResolveMessage(outcome2, code, httpStatus);
		return new PosResult
		{
			Outcome = outcome2,
			Code = code,
			Message = message,
			CurrentStock = currentStock,
			LatencyMs = Math.Round(elapsedMs, 2),
			HttpStatus = httpStatus
		};
	}

	private static PosOutcome ResolveOutcome(string? outcome, int httpStatus)
	{
		if (!string.IsNullOrWhiteSpace(outcome) && Enum.TryParse<PosOutcome>(outcome, ignoreCase: true, out var result))
		{
			return result;
		}
		if (httpStatus >= 200 && httpStatus < 300)
		{
			return PosOutcome.OK;
		}
		if (1 == 0)
		{
		}
		PosOutcome result2 = httpStatus switch
		{
			401 => PosOutcome.REJECTED, 
			403 => PosOutcome.REJECTED, 
			409 => PosOutcome.REJECTED, 
			422 => PosOutcome.REJECTED, 
			404 => PosOutcome.REJECTED, 
			_ => PosOutcome.ERROR, 
		};
		if (1 == 0)
		{
		}
		return result2;
	}

	private static string? ResolveCode(int httpStatus)
	{
		if (1 == 0)
		{
		}
		string result = httpStatus switch
		{
			401 => "UNAUTHORIZED", 
			403 => "TERMINAL_NOT_REGISTERED", 
			409 => "CONFLICT", 
			422 => "INVALID_EVENT", 
			503 => "SERVICE_UNAVAILABLE", 
			_ => null, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string ResolveMessage(PosOutcome outcome, string? code, int httpStatus)
	{
		if (1 == 0)
		{
		}
		string result;
		switch (outcome)
		{
		case PosOutcome.OK:
			result = "Giao dịch thành công.";
			break;
		case PosOutcome.DUPLICATE:
			result = "Giao dịch đã được ghi nhận trước đó (idempotent), tồn kho không đổi.";
			break;
		case PosOutcome.REJECTED:
		{
			if (1 == 0)
			{
			}
			string text = code switch
			{
				"UNAUTHORIZED" => "Token POS không hợp lệ hoặc không thuộc terminal đang chọn.", 
				"TERMINAL_NOT_REGISTERED" => "Store/POS chưa được đăng ký trong cấu hình terminal.", 
				"CONFLICT" => "Transaction ID đã tồn tại nhưng nội dung giao dịch khác.", 
				_ => "Giao dịch bị từ chối.", 
			};
			if (1 == 0)
			{
			}
			result = text;
			break;
		}
		default:
			result = ((httpStatus == 503) ? "Dịch vụ tạm thời không khả dụng. Giữ nguyên giỏ hàng và thử gửi lại." : "Dịch vụ tạm thời không khả dụng. Kiểm tra kết nối Middleware rồi gửi lại cùng transaction_id.");
			break;
		}
		if (1 == 0)
		{
		}
		return result;
	}

	private static PosResult Error(string code, string message, int httpStatus, double elapsedMs)
	{
		return new PosResult
		{
			Outcome = PosOutcome.ERROR,
			Code = code,
			Message = message,
			CurrentStock = null,
			LatencyMs = Math.Round(elapsedMs, 2),
			HttpStatus = httpStatus
		};
	}

	private static string? ReadString(JsonElement root, string name)
	{
		JsonElement value;
		return (root.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.String) ? value.GetString() : null;
	}

	private static Dictionary<string, int>? ReadStock(JsonElement root, string name)
	{
		if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Object)
		{
			return null;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (JsonProperty item in value.EnumerateObject())
		{
			if (item.Value.ValueKind == JsonValueKind.Number && item.Value.TryGetInt32(out var value2))
			{
				dictionary[item.Name] = value2;
			}
		}
		return (dictionary.Count == 0) ? null : dictionary;
	}

	public void Dispose()
	{
		_httpClient.Dispose();
	}
}
