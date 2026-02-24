using System.ComponentModel.DataAnnotations;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Request;

public class ProductDto
{
    public Guid ProductId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductType Type { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Nutrition { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ResponsibleOrg { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public bool isActive { get; set; }
    public bool isFeatured { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? CreatedByUser { get; set; }
    public Supermarket? Supermarket { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public ICollection<ProductLot> ProductLots { get; set; } = new List<ProductLot>();
    public ICollection<AIVerificationLog> AIVerificationLogs { get; set; } = new List<AIVerificationLog>();
    public Pricing? Pricing { get; set; }
}

public class CreateProductRequestDto
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductType Type { get; set; } = ProductType.Standard;
    public string Sku { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Nutrition { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ResponsibleOrg { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public bool isActive { get; set; } = true;
    public bool isFeatured { get; set; } = false;
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class UpdateProductRequestDto
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductType Type { get; set; }
    public string Sku { get; set; } = string.Empty;
    public ProductState Status { get; set; }
    public string Ingredients { get; set; } = string.Empty;
    public string Nutrition { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ResponsibleOrg { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public bool isActive { get; set; }
    public bool isFeatured { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class ProductResponseDto
{
    public Guid ProductId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductType Type { get; set; }
    public string Sku { get; set; } = string.Empty;
    public ProductState Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Nutrition { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ResponsibleOrg { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public bool isActive { get; set; }
    public bool isFeatured { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public PricingResponseDto? Pricing { get; set; }
}

public class PricingResponseDto
{
    public Guid PricingId { get; set; }
    public decimal BasePrice { get; set; }
    public string BaseUnit { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
    public decimal? SalePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
}
