namespace DemoPos.Models;

public sealed class CartItem
{
	public string Sku { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public decimal UnitPrice { get; set; }

	public int Quantity { get; set; }

	public int SoldQuantity { get; set; }

	public int ReturnedQuantity { get; set; }

	public int? MaxReturnQuantity { get; set; }

	public decimal LineTotal => (decimal)Quantity * UnitPrice;
}
