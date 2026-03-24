using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Response;

public class ProductDetailDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string? Origin { get; set; }
    public string Weight { get; set; } = "Đang cập nhật";
    public string Ingredients { get; set; } = "Chưa có mô tả chi tiết";
    public string UsageInstructions { get; set; } = "Chưa có mô tả chi tiết";
    public string StorageInstructions { get; set; } = "Chưa có mô tả chi tiết";
    public string ManufactureDate { get; set; } = "Xem trên bao bì";
    public string ExpiryDate { get; set; } = "Xem trên bao bì";
    public string Manufacturer { get; set; } = "Chưa có mô tả chi tiết";
    public string SafetyWarning { get; set; } = "Chưa có mô tả chi tiết";
    public string Distributor { get; set; } = "Chưa có mô tả chi tiết";
    public Dictionary<string, string>? NutritionFacts { get; set; }
    public string Category { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal SuggestedPrice { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public ProductState Status { get; set; }
    public string SupermarketName { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public int TotalImages { get; set; }
    public List<ProductImageDto> ProductImages { get; set; } = new();
    public int? DaysToExpiry { get; set; }
    public ExpiryStatus? ExpiryStatus { get; set; }
    public string? ExpiryStatusText { get; set; }
}

public class ProductSalesListItemDto
{
    public Guid ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal SuggestedPrice { get; set; }
    public string? MainImageUrl { get; set; }
    public int? DaysToExpiry { get; set; }
    public ExpiryStatus? ExpiryStatus { get; set; }
}