namespace DemoPos.Models;

public sealed class Product
{
	public string Sku { get; set; } = string.Empty;

	public string Barcode { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public decimal UnitPrice { get; set; }

	public int InitialStock { get; set; }

	public int CurrentStock { get; set; }

	public bool IsOutOfStock => CurrentStock <= 0;

	public bool IsLowStock => CurrentStock > 0 && CurrentStock <= 5;
}
