using System;
using System.Collections.Generic;
using System.Linq;
using DemoPos.Models;

namespace DemoPos.Data;

public static class ProductCatalog
{
	private static readonly Product[] Seed = new Product[10]
	{
		new Product
		{
			Sku = "VIN-MLK-1L",
			Barcode = "890000000001",
			Name = "Sữa tươi Vinamilk Hộp 1L",
			UnitPrice = 18500m,
			InitialStock = 24
		},
		new Product
		{
			Sku = "HH-TOM-01",
			Barcode = "890000000002",
			Name = "Mì gói Hảo Hảo Tôm",
			UnitPrice = 5200m,
			InitialStock = 1
		},
		new Product
		{
			Sku = "AQU-5L",
			Barcode = "890000000003",
			Name = "Nước suối Aquafina 5L",
			UnitPrice = 27000m,
			InitialStock = 18
		},
		new Product
		{
			Sku = "SND-ICH-01",
			Barcode = "890000000004",
			Name = "Bánh mì sandwich đông lạnh",
			UnitPrice = 32000m,
			InitialStock = 12
		},
		new Product
		{
			Sku = "VIT-SY-03",
			Barcode = "890000000005",
			Name = "Sữa chua uống Vitasoy",
			UnitPrice = 7500m,
			InitialStock = 30
		},
		new Product
		{
			Sku = "EGG-TA-10",
			Barcode = "890000000006",
			Name = "Trứng gà ta (lọ 10 quả)",
			UnitPrice = 38000m,
			InitialStock = 15
		},
		new Product
		{
			Sku = "COC-250-01",
			Barcode = "890000000007",
			Name = "Nước Coca-Cola 250ml",
			UnitPrice = 6500m,
			InitialStock = 48
		},
		new Product
		{
			Sku = "TSN-BG-01",
			Barcode = "890000000008",
			Name = "Trà xanh Trí Việt 500ml",
			UnitPrice = 10500m,
			InitialStock = 26
		},
		new Product
		{
			Sku = "CFD-MX-01",
			Barcode = "890000000009",
			Name = "Cà phê lon Phin Đậm Đá",
			UnitPrice = 14500m,
			InitialStock = 20
		},
		new Product
		{
			Sku = "PST-LAP-01",
			Barcode = "890000000010",
			Name = "Bánh tráng nước Lạnh",
			UnitPrice = 4500m,
			InitialStock = 60
		}
	};

	public static IReadOnlyList<Product> CreateProducts()
	{
		return Seed.Select((Product p) => new Product
		{
			Sku = p.Sku,
			Barcode = p.Barcode,
			Name = p.Name,
			UnitPrice = p.UnitPrice,
			InitialStock = p.InitialStock,
			CurrentStock = p.InitialStock
		}).ToList();
	}

	public static Product? FindByCode(IEnumerable<Product> products, string code)
	{
		string needle = (code ?? string.Empty).Trim();
		if (needle.Length == 0)
		{
			return null;
		}
		return products.FirstOrDefault((Product p) => string.Equals(p.Sku, needle, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Barcode, needle, StringComparison.OrdinalIgnoreCase)) ?? products.FirstOrDefault((Product p) => p.Barcode.Contains(needle, StringComparison.OrdinalIgnoreCase) || p.Sku.Contains(needle, StringComparison.OrdinalIgnoreCase) || p.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
	}
}
