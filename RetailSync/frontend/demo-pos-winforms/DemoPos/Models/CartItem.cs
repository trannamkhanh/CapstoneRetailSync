namespace DemoPos.Models;

/// <summary>Dòng hàng trong giỏ bán hoặc trong bảng dòng hàng trả.</summary>
public sealed class CartItem
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    /// <summary>Số lượng đã bán (dùng cho tab Trả hàng).</summary>
    public int SoldQuantity { get; set; }

    /// <summary>Số lượng đã trả trước đó (dùng cho tab Trả hàng).</summary>
    public int ReturnedQuantity { get; set; }

    /// <summary>Số lượng còn có thể trả = đã bán − đã trả (null = không áp dụng).</summary>
    public int? MaxReturnQuantity { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
