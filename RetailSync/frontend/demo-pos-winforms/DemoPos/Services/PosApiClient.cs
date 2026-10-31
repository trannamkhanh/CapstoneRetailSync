using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DemoPos.Models;

namespace DemoPos.Services;

/// <summary>
/// Gọi REST Middleware FastAPI. Payload / outcome / code giữ đúng hợp đồng của bản web demo-pos.
/// </summary>
public sealed class PosApiClient : IPosBackend, IDisposable
{
    /// <summary>Đường dẫn API — gom vào một hằng số để sửa 1 chỗ.</summary>
    public const string PosEventsEndpoint = "/pos/events";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly object _gate = new();
    private readonly List<Product> _products;
    private string _baseUrl;

    public PosApiClient(AppConfig config, string? baseUrl = null)
    {
        _baseUrl = NormalizeBaseUrl(baseUrl ?? config.BaseUrl);
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _products = Data.ProductCatalog.CreateProducts().ToList();
    }

    /// <summary>BaseUrl đang dùng (có thể sửa từ UI khi chuyển sang Middleware thật).</summary>
    public string BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = NormalizeBaseUrl(value);
    }

    public IReadOnlyList<Product> GetProducts()
    {
        lock (_gate)
        {
            return _products.Select(p => new Product
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

    /// <summary>Middleware thật không có trạng thái cục bộ — Reset chỉ khôi phục cache tồn kho tại UI.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            foreach (var product in _products)
            {
                product.CurrentStock = product.InitialStock;
            }
        }
    }

    public async Task<PosResult> SendAsync(PosEvent posEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(posEvent);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var payload = JsonSerializer.Serialize(posEvent, JsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildEventsUrl())
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", posEvent.Token);

            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            var result = NormalizePayload(body, (int)response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
            ApplyStock(result);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return Error("TIMEOUT", "Dịch vụ tạm thời không khả dụng. Kiểm tra kết nối Middleware rồi gửi lại cùng transaction_id.",
                0, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return Error("CANCELLED", "Giao dịch đã bị hủy.", 0, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (HttpRequestException)
        {
            stopwatch.Stop();
            return Error("NETWORK_ERROR", "Dịch vụ tạm thời không khả dụng. Kiểm tra kết nối Middleware rồi gửi lại cùng transaction_id.",
                0, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Error("NETWORK_ERROR", $"Dịch vụ tạm thời không khả dụng. {ex.Message}", 0, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private string BuildEventsUrl() => _baseUrl.TrimEnd('/') + PosEventsEndpoint;

    private static string NormalizeBaseUrl(string? value) =>
        (value ?? string.Empty).Trim().TrimEnd('/');

    private void ApplyStock(PosResult result)
    {
        if (result.CurrentStock is null || result.CurrentStock.Count == 0)
        {
            return;
        }

        lock (_gate)
        {
            foreach (var product in _products)
            {
                if (result.CurrentStock.TryGetValue(product.Sku, out var stock))
                {
                    product.CurrentStock = stock;
                }
            }
        }
    }

    /// <summary>
    /// Chuẩn hóa phản hồi Middleware về PosResult.
    /// Quy ước: 2xx OK/DUPLICATE; 401 UNAUTHORIZED; 403 TERMINAL_NOT_REGISTERED; 409 CONFLICT;
    /// 422 dữ liệu không hợp lệ; 503/timeout/mất kết nối => ERROR (cho phép gửi lại).
    /// </summary>
    public static PosResult NormalizePayload(string? body, int httpStatus, double elapsedMs)
    {
        Dictionary<string, int>? currentStock = null;
        string? outcome = null;
        string? code = null;
        string? message = null;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    outcome = ReadString(root, "outcome");
                    code = ReadString(root, "code") ?? ReadString(root, "error_code");
                    message = ReadString(root, "message") ?? ReadString(root, "detail");
                    currentStock = ReadStock(root, "currentStock") ?? ReadStock(root, "current_stock");
                }
            }
            catch (JsonException)
            {
                // Phản hồi không phải JSON: giữ nguyên code/message suy ra từ HTTP status.
            }
        }

        var finalOutcome = ResolveOutcome(outcome, httpStatus);
        var finalCode = code ?? ResolveCode(httpStatus);
        var finalMessage = message ?? ResolveMessage(finalOutcome, finalCode, httpStatus);

        return new PosResult
        {
            Outcome = finalOutcome,
            Code = finalCode,
            Message = finalMessage,
            CurrentStock = currentStock,
            LatencyMs = Math.Round(elapsedMs, 2),
            HttpStatus = httpStatus
        };
    }

    private static PosOutcome ResolveOutcome(string? outcome, int httpStatus)
    {
        if (!string.IsNullOrWhiteSpace(outcome) &&
            Enum.TryParse<PosOutcome>(outcome, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        if (httpStatus is >= 200 and < 300)
        {
            return PosOutcome.OK;
        }

        return httpStatus switch
        {
            (int)HttpStatusCode.Unauthorized => PosOutcome.REJECTED,
            (int)HttpStatusCode.Forbidden => PosOutcome.REJECTED,
            (int)HttpStatusCode.Conflict => PosOutcome.REJECTED,
            422 => PosOutcome.REJECTED,
            404 => PosOutcome.REJECTED,
            _ => PosOutcome.ERROR
        };
    }

    private static string? ResolveCode(int httpStatus) => httpStatus switch
    {
        (int)HttpStatusCode.Unauthorized => "UNAUTHORIZED",
        (int)HttpStatusCode.Forbidden => "TERMINAL_NOT_REGISTERED",
        (int)HttpStatusCode.Conflict => "CONFLICT",
        422 => "INVALID_EVENT",
        (int)HttpStatusCode.ServiceUnavailable => "SERVICE_UNAVAILABLE",
        _ => null
    };

    private static string ResolveMessage(PosOutcome outcome, string? code, int httpStatus) => outcome switch
    {
        PosOutcome.OK => "Giao dịch thành công.",
        PosOutcome.DUPLICATE => "Giao dịch đã được ghi nhận trước đó (idempotent), tồn kho không đổi.",
        PosOutcome.REJECTED => code switch
        {
            "UNAUTHORIZED" => "Token POS không hợp lệ hoặc không thuộc terminal đang chọn.",
            "TERMINAL_NOT_REGISTERED" => "Store/POS chưa được đăng ký trong cấu hình terminal.",
            "CONFLICT" => "Transaction ID đã tồn tại nhưng nội dung giao dịch khác.",
            _ => "Giao dịch bị từ chối."
        },
        _ => httpStatus == 503
            ? "Dịch vụ tạm thời không khả dụng. Giữ nguyên giỏ hàng và thử gửi lại."
            : "Dịch vụ tạm thời không khả dụng. Kiểm tra kết nối Middleware rồi gửi lại cùng transaction_id."
    };

    private static PosResult Error(string code, string message, int httpStatus, double elapsedMs) => new()
    {
        Outcome = PosOutcome.ERROR,
        Code = code,
        Message = message,
        CurrentStock = null,
        LatencyMs = Math.Round(elapsedMs, 2),
        HttpStatus = httpStatus
    };

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static Dictionary<string, int>? ReadStock(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in value.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var quantity))
            {
                result[property.Name] = quantity;
            }
        }

        return result.Count == 0 ? null : result;
    }

    public void Dispose() => _httpClient.Dispose();
}
