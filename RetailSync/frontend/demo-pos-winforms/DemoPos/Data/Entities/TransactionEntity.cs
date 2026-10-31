using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace DemoPos.Data.Entities;

/// <summary>Giao dịch POS đã ghi nhận (dùng cho idempotency + lịch sử).</summary>
[Table("Transactions")]
public class TransactionEntity
{
    [Key]
    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string StoreId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PosId { get; set; } = string.Empty;

    /// <summary>SALE / RETURN / CANCELLATION.</summary>
    [Required]
    [MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? OriginalTransactionId { get; set; }

    /// <summary>Chữ ký JSON của giao dịch (so sánh idempotency, không gồm timestamp).</summary>
    [Required]
    public string Signature { get; set; } = string.Empty;

    /// <summary>Tồn kho tại thời điểm giao dịch (JSON: SKU -> số lượng).</summary>
    [Required]
    public string CurrentStockJson { get; set; } = "{}";

    /// <summary>Danh sách dòng hàng (JSON).</summary>
    [Required]
    public string ItemsJson { get; set; } = "[]";

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public Dictionary<string, int> CurrentStock
    {
        get => string.IsNullOrWhiteSpace(CurrentStockJson)
            ? new Dictionary<string, int>()
            : JsonSerializer.Deserialize<Dictionary<string, int>>(CurrentStockJson) ?? new();
        set => CurrentStockJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<PosEventItemEntity> Items
    {
        get => string.IsNullOrWhiteSpace(ItemsJson)
            ? new List<PosEventItemEntity>()
            : JsonSerializer.Deserialize<List<PosEventItemEntity>>(ItemsJson) ?? new();
        set => ItemsJson = JsonSerializer.Serialize(value);
    }
}

/// <summary>Dòng hàng serialized trong cột ItemsJson.</summary>
public class PosEventItemEntity
{
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}

/// <summary>Dòng đã bán (nguồn dữ liệu cho trả hàng / hủy đơn).</summary>
[Table("SoldItems")]
public class SoldItemEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Dòng đã trả (theo dõi số lượng đã trả theo từng SKU của đơn gốc).</summary>
[Table("ReturnedItems")]
public class ReturnedItemEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string OriginalTransactionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    [MaxLength(100)]
    public string ReturnTransactionId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Đơn bán đã bị hủy.</summary>
[Table("CancelledTransactions")]
public class CancelledTransactionEntity
{
    [Key]
    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CancellationTransactionId { get; set; } = string.Empty;

    public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Audit log mỗi lần gửi sự kiện.</summary>
[Table("EventLogs")]
public class EventLogEntity
{
    [Key]
    public long Id { get; set; }

    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(50)]
    public string StoreId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PosId { get; set; } = string.Empty;

    /// <summary>OK / DUPLICATE / REJECTED / ERROR.</summary>
    [MaxLength(50)]
    public string Outcome { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    public string? Message { get; set; }

    public int HttpStatus { get; set; }

    public double LatencyMs { get; set; }

    public string RequestJson { get; set; } = string.Empty;

    public string ResponseJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Cài đặt dạng key/value.</summary>
[Table("AppSettings")]
public class AppSettingEntity
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}