using System.ComponentModel.DataAnnotations;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Response;

public class ProductImageDto
{
    public Guid ProductImageId { get; set; }
    public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
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
    public string WeightTypeName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public int? DaysToExpiry { get; set; }
    public float PricingConfidence { get; set; }
    public string? PricingReasons { get; set; }

    [Required(ErrorMessage = "CreatedBy is required")]
    public required string CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? PricedBy { get; set; }
    public DateTime? PricedAt { get; set; }
    public string? MainImageUrl { get; set; }
    public int TotalImages { get; set; }
    public ICollection<ProductImageDto> ProductImages { get; set; } = new List<ProductImageDto>();
    public List<string> Ingredients { get; set; } = new();
    public Dictionary<string, string>? NutritionFacts { get; set; }
    public BarcodeLookupInfoDto? BarcodeLookupInfo { get; set; }
}

public class BarcodeLookupInfoDto
{
    public string Barcode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Manufacturer { get; set; }
    public string? Weight { get; set; }
    public List<string> Ingredients { get; set; } = new();
    public Dictionary<string, string>? NutritionFacts { get; set; }
    public string? Country { get; set; }
    public string Source { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public bool IsVietnameseProduct { get; set; }
    public string? Gs1Prefix { get; set; }
    public int ScanCount { get; set; }
    public bool IsVerified { get; set; }
}

public class PricingSuggestionResponseDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public float Confidence { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysToExpiry { get; set; }
    public List<string> Reasons { get; set; } = new();
    public decimal? MinMarketPrice { get; set; }
    public decimal? AvgMarketPrice { get; set; }
    public decimal? MaxMarketPrice { get; set; }
    public List<MarketPriceSourceDto> MarketPriceSources { get; set; } = new();
}

public class MarketPriceSourceDto
{
    public string StoreName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class ScanBarcodeResponseDto
{
    public string Barcode { get; set; } = string.Empty;
    public bool ProductExists { get; set; }
    public ExistingProductInfoDto? ExistingProduct { get; set; }
    public BarcodeLookupInfoDto? BarcodeLookupInfo { get; set; }
    public string NextAction { get; set; } = string.Empty;
    public bool RequiresOcrUpload { get; set; }
}

public class ExistingProductInfoDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }

    public string? Manufacturer { get; set; }
    public List<string> Ingredients { get; set; } = new();
    public decimal? LastPrice { get; set; }
    public int TotalLotsSold { get; set; }
}

public class CreateNewProductResponseDto
{
    public Guid ProductId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public List<string> Ingredients { get; set; } = new();
    public string? MainImageUrl { get; set; }
    public ProductState Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string NextAction { get; set; } = "VERIFY_PRODUCT";
    public string NextActionDescription { get; set; } = "Xác nhận thông tin sản phẩm trước khi tạo StockLot";
}

public class OcrAnalysisResponseDto
{
    public string ImageUrl { get; set; } = string.Empty;
    public OcrExtractedInfoDto ExtractedInfo { get; set; } = new();
    public BarcodeLookupInfoDto? BarcodeLookupInfo { get; set; }
    public float Confidence { get; set; }
    public string? RawOcrData { get; set; }
}

public class OcrExtractedInfoDto
{
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public string? Category { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public string? Weight { get; set; }
    public List<string> Ingredients { get; set; } = new();
    public string? Manufacturer { get; set; }
    public string? Origin { get; set; }
    public Dictionary<string, string>? NutritionFacts { get; set; }
}

public class StockLotResponseDto
{
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductBarcode { get; set; } = string.Empty;
    public string? ProductBrand { get; set; }
    public string? ProductImageUrl { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }
    public int DaysToExpiry { get; set; }
    public decimal Quantity { get; set; }
    public decimal Weight { get; set; }
    public ProductState Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? SuggestedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public float? PricingConfidence { get; set; }
}

public class WorkflowTimeoutInfoDto
{
    public int TimeoutSeconds { get; set; }
    public bool IsAiStep { get; set; }
    public bool SupportsManualFallback { get; set; }
}

public class StaffProductIdentificationResponseDto
{
    public string Barcode { get; set; } = string.Empty;
    public bool ProductExists { get; set; }
    public string Phase { get; set; } = "IDENTIFICATION";
    public string NextAction { get; set; } = string.Empty;
    public ExistingProductInfoDto? ExistingProduct { get; set; }
    public BarcodeLookupInfoDto? BarcodeLookupInfo { get; set; }
    public WorkflowTimeoutInfoDto TimeoutInfo { get; set; } = new();
}

public class StaffCreateLotAndPublishResponseDto
{
    public Guid ProductId { get; set; }
    public Guid LotId { get; set; }
    public string Phase { get; set; } = "LOT_AND_PRICING";
    public PricingSuggestionResponseDto PricingSuggestion { get; set; } = new();
    public StockLotResponseDto StockLot { get; set; } = new();
    public bool IsManualFallback { get; set; }
    public WorkflowTimeoutInfoDto TimeoutInfo { get; set; } = new();
}

public class ExcelPreviewResponseDto
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, string>> PreviewData { get; set; } = new();
    public int TotalRows { get; set; }
    public List<ExcelColumnMappingDto> SuggestedMappings { get; set; } = new();
}

public class ExcelImportResponseDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ExcelImportErrorDto> Errors { get; set; } = new();
    public List<Guid> CreatedProductIds { get; set; } = new();
}

public class ExcelImportErrorDto
{
    public int RowNumber { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, string> RowData { get; set; } = new();
}
