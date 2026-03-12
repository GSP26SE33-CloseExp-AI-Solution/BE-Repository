using CloseExpAISolution.Application.AIService.Interfaces;

namespace CloseExpAISolution.Application.DTOs.Response;

public class AIHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Service { get; set; } = string.Empty;
}

public class ProductExtractionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufacturedDate { get; set; }
    public BarcodeProductInfo? BarcodeInfo { get; set; }
    public string? BestName => BarcodeInfo?.ProductName ?? Name;
    public string? BestBrand => BarcodeInfo?.Brand ?? Brand;
    public string? Category => BarcodeInfo?.Category;
    public string? Description => BarcodeInfo?.Description;
    public string? ProductImageUrl => BarcodeInfo?.ImageUrl;
    public string? Ingredients => BarcodeInfo?.Ingredients;
    public Dictionary<string, string>? NutritionFacts => BarcodeInfo?.NutritionFacts;
    public float OverallConfidence { get; set; }
    public float? ExpiryDateConfidence { get; set; }
    public float? ManufacturedDateConfidence { get; set; }
    public float ProcessingTimeMs { get; set; }
    public List<string> Warnings { get; set; } = new();
    public bool BarcodeFound => BarcodeInfo != null;
    public string? BarcodeSource => BarcodeInfo?.Source;
    public Guid? VerificationLogId { get; set; }
}

public class PricingSuggestionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public float DiscountPercent { get; set; }
    public float Confidence { get; set; }
    public float ExpectedSellRate { get; set; }
    public string EstimatedTimeToSell { get; set; } = string.Empty;
    public float Competitiveness { get; set; }
    public List<string> Reasons { get; set; } = new();
    public string UrgencyLevel { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public int DaysToExpire { get; set; }
    public decimal OriginalPrice { get; set; }
    public string Category { get; set; } = string.Empty;
    public float ProcessingTimeMs { get; set; }
}

public class ShelfAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalProducts { get; set; }
    public List<DetectedProduct> Products { get; set; } = new();
    public string ImageQuality { get; set; } = string.Empty;
    public float ImageQualityScore { get; set; }
    public Dictionary<string, int> CategorySummary { get; set; } = new();
    public string? AnnotatedImageBase64 { get; set; }
    public float ProcessingTimeMs { get; set; }
}

public class DetectedProduct
{
    public int Index { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public BoundingBoxResult BoundingBox { get; set; } = new();
}

public class BoundingBoxResult
{
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
}

public class ProductProcessingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ProductExtractionResult? Extraction { get; set; }
    public PricingSuggestionResult? Pricing { get; set; }
    public string? ExtractedName { get; set; }
    public DateTime? ExtractedExpiryDate { get; set; }
    public decimal? SuggestedPrice { get; set; }
    public float OverallConfidence { get; set; }
    public float TotalProcessingTimeMs { get; set; }
}

public class SmartScanResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ScanType { get; set; } = string.Empty;
    public bool IsVietnameseProduct { get; set; }
    public VietnameseBarcodeResult? VietnameseBarcodeInfo { get; set; }
    public string? ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufacturedDate { get; set; }
    public string? SuggestedCategory { get; set; }
    public int? SuggestedShelfLifeDays { get; set; }
    public string? StorageRecommendation { get; set; }
    public string? UsageInstructions { get; set; }
    public List<string>? Ingredients { get; set; }
    public Dictionary<string, object>? NutritionFacts { get; set; }
    public string? Weight { get; set; }
    public string? Origin { get; set; }
    public List<string>? Certifications { get; set; }
    public List<string>? QualityStandards { get; set; }
    public List<string>? Warnings { get; set; }
    public ManufacturerInfoResult? ManufacturerInfo { get; set; }
    public ProductCodesResult? ProductCodes { get; set; }
    public List<FreshProduceDetection>? FreshProduceItems { get; set; }
    public float Confidence { get; set; }
    public float ProcessingTimeMs { get; set; }
}

public class ManufacturerInfoResult
{
    public string? Name { get; set; }
    public string? Distributor { get; set; }
    public string? Address { get; set; }
    public List<string>? Contact { get; set; }
}

public class ProductCodesResult
{
    public string? Sku { get; set; }
    public string? Batch { get; set; }
    public string? Msktvsty { get; set; }
}

public class VietnameseBarcodeResult
{
    public string Barcode { get; set; } = string.Empty;
    public bool IsVietnamese { get; set; }
    public string? Company { get; set; }
    public string? Category { get; set; }
    public string? Prefix { get; set; }
    public string? Note { get; set; }
}

public class FreshProduceDetection
{
    public string Category { get; set; } = string.Empty;
    public string? NameVi { get; set; }
    public string? NameEn { get; set; }
    public int? TypicalShelfLifeDays { get; set; }
    public string? StorageRecommendation { get; set; }
    public List<string>? FreshnessIndicators { get; set; }
    public float Confidence { get; set; }
}

public class FreshProduceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<FreshProduceDetection> DetectedItems { get; set; } = new();
    public float ProcessingTimeMs { get; set; }
    public List<string>? Warnings { get; set; }
}
