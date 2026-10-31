using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace DemoPos.Data.Entities;

/// <summary>Sản phẩm trong database.</summary>
[Table("Products")]
public class ProductEntity
{
    [Key]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Barcode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public int InitialStock { get; set; }

    public int CurrentStock { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Terminal POS trong database (khóa ghép StoreId + PosId).</summary>
[Table("Terminals")]
public class TerminalEntity
{
    [MaxLength(50)]
    public string StoreId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PosId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string StoreName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}