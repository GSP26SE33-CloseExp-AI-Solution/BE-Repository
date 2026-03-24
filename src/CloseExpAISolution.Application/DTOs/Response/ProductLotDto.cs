using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Response;

public class StockLotDetailDto
{
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal Weight { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public decimal OriginalUnitPrice { get; set; }
    public decimal SuggestedUnitPrice { get; set; }
    public decimal? FinalUnitPrice { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public Guid SupermarketId { get; set; }
    public string SupermarketName { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public int TotalImages { get; set; }
    public List<ProductImageDto> ProductImages { get; set; } = new();
    public ExpiryStatus ExpiryStatus { get; set; }
    public int DaysRemaining { get; set; }
    public int? HoursRemaining { get; set; }
    public string ExpiryStatusText { get; set; } = string.Empty;
    public string? Ingredients { get; set; }
    public Dictionary<string, string>? NutritionFacts { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StockLotFilterDto
{
    public Guid? SupermarketId { get; set; }
    public ExpiryStatus? ExpiryStatus { get; set; }
    public ProductWeightType? WeightType { get; set; }
    public bool? IsFreshFood { get; set; }
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AvailableStocklotDto
{
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public Guid SupermarketId { get; set; }
    public string SupermarketName { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Weight { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public decimal SuggestedUnitPrice { get; set; }
    public decimal? FinalUnitPrice { get; set; }
    public decimal SellingUnitPrice { get; set; }
    public int DaysRemaining { get; set; }
}