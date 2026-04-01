namespace CloseExpAISolution.Application.AIService.Models;

#region OCR Models

public class OcrRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageB64 { get; set; }
    public List<string> Languages { get; set; } = new() { "vi", "en" };
    public bool ExtractDates { get; set; } = true;
    public bool ExtractBarcode { get; set; } = true;
    public bool ReturnRegions { get; set; } = false;
    public float MinConfidence { get; set; } = 0.5f;
}

public class OcrResponse
{
    public DateInfo? ExpiryDate { get; set; }
    public DateInfo? ManufacturedDate { get; set; }
    public ProductInfo? ProductInfo { get; set; }
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public string? RawText { get; set; }
    public List<TextRegion>? TextRegions { get; set; }
    public float Confidence { get; set; }
    public float? ProcessingTimeMs { get; set; }
    public List<string>? Warnings { get; set; }
    public FreshnessAssessment? Freshness { get; set; }
}

public class DateInfo
{
    public DateTime? Value { get; set; }
    public string? RawText { get; set; }
    public float Confidence { get; set; }
    public string? FormatDetected { get; set; }
}

public class ProductInfo
{
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public BarcodeInfo? BarcodeInfo { get; set; }
    public string? Weight { get; set; }
    public WeightInfo? WeightInfo { get; set; }
    public List<string>? Ingredients { get; set; }
    public Dictionary<string, object>? NutritionFacts { get; set; }
    public string? StorageInstructions { get; set; }
    public string? UsageInstructions { get; set; }
    public ManufacturerInfo? Manufacturer { get; set; }
    public string? Origin { get; set; }
    public List<string>? Certifications { get; set; }
    public List<string>? QualityStandards { get; set; }
    public List<string>? Warnings { get; set; }
    public ProductCodesInfo? ProductCodes { get; set; }
    public int? ShelfLifeDays { get; set; }
    public CategoryInfo? DetectedCategory { get; set; }
}

public class BarcodeInfo
{
    public string Barcode { get; set; } = string.Empty;
    public bool IsVietnamese { get; set; }
    public string? Company { get; set; }
    public string? Category { get; set; }
    public string? Prefix { get; set; }
    public string? Note { get; set; }
    public string? Country { get; set; }
}

public class WeightInfo
{
    public float Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Raw { get; set; }
}

public class ManufacturerInfo
{
    public string? Name { get; set; }
    public string? Distributor { get; set; }
    public string? Address { get; set; }
    public List<string>? Contact { get; set; }
}

public class ProductCodesInfo
{
    public string? Sku { get; set; }
    public string? Batch { get; set; }
    public string? Msktvsty { get; set; }  // Mã số kinh tế vệ sinh thú y
}

public class CategoryInfo
{
    public string Name { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public List<string>? KeywordsVi { get; set; }
}

public class TextRegion
{
    public string Text { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public BoundingBox BoundingBox { get; set; } = new();
    public string? Language { get; set; }
}

public class BoundingBox
{
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
}

#endregion

#region Pricing Models

public class PricingRequest
{
    public string ProductType { get; set; } = "other";
    public int DaysToExpire { get; set; }
    public decimal BasePrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Region { get; set; }
    public string? Brand { get; set; }
    public float? DemandIndex { get; set; }
    public decimal? CompetitorPrice { get; set; }
    public int? InventoryQuantity { get; set; }
    public string Strategy { get; set; } = "balanced";
    public decimal? MinMarketPrice { get; set; }
    public decimal? AvgMarketPrice { get; set; }
    public string? MarketPriceSource { get; set; }
    public string? ProductName { get; set; }
    public string? Barcode { get; set; }
    public string? FreshnessLevel { get; set; }
    public float? FreshnessScore { get; set; }
}

public class FreshnessAssessment
{
    public string? Level { get; set; }
    public float? Score { get; set; }
    public Dictionary<string, object>? Indicators { get; set; }
}

public class PricingResponse
{
    public decimal SuggestedPrice { get; set; }
    public float DiscountPercent { get; set; }
    public float Confidence { get; set; }
    public decimal MinSuggestedPrice { get; set; }
    public decimal MaxSuggestedPrice { get; set; }
    public float ExpectedSellRate { get; set; }
    public string EstimatedTimeToSell { get; set; } = string.Empty;
    public float Competitiveness { get; set; }
    public List<string> Reasons { get; set; } = new();
    public MarketPriceInfo? MarketPriceInfo { get; set; }

    public Dictionary<string, object> Rationale { get; set; } = new();
    public PriceBreakdown? Breakdown { get; set; }
    public string UrgencyLevel { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public float? CalculationTimeMs { get; set; }
    public string? ModelVersion { get; set; }
}

public class MarketPriceInfo
{
    public decimal? MinMarketPrice { get; set; }
    public decimal? AvgMarketPrice { get; set; }
    public string? Source { get; set; }
    public float? PriceVsMarketPercent { get; set; }
    public string? AdjustmentApplied { get; set; }
}

public class PriceBreakdown
{
    public decimal BasePrice { get; set; }
    public float DecayFactor { get; set; }
    public float DemandAdjustment { get; set; }
    public float StrategyAdjustment { get; set; }
    public float CompetitorAdjustment { get; set; }
    public float FinalDiscountPercent { get; set; }
}

#endregion

#region Vision Models

public class VisionRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageB64 { get; set; }
    public string? Model { get; set; }
    public float MinConfidence { get; set; } = 0.25f;
    public int MaxDetections { get; set; } = 100;
    public bool ReturnCrops { get; set; } = false;
    public bool ReturnAnnotatedImage { get; set; } = true;
    public bool AssessQuality { get; set; } = true;
    public bool AssessFreshness { get; set; } = false;
}

public class VisionResponse
{
    public List<Detection> Detections { get; set; } = new();
    public int DetectionCount { get; set; }
    public QualityAssessment? ImageQuality { get; set; }
    public Dictionary<string, int> ClassSummary { get; set; } = new();
    public Dictionary<string, int> ProductTypeSummary { get; set; } = new();
    public string? AnnotatedImageB64 { get; set; }
    public string? AnnotatedImageContentType { get; set; }
    public string Model { get; set; } = string.Empty;
    public float InferenceTimeMs { get; set; }
    public Dictionary<string, int>? ImageDimensions { get; set; }
}

public class Detection
{
    public int Index { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public BoundingBox BoundingBox { get; set; } = new();
    public string ProductType { get; set; } = "unknown";
    public QualityAssessment? Quality { get; set; }
    public string? CropImageB64 { get; set; }
    public string? CropImageContentType { get; set; }
}

public class QualityAssessment
{
    public string Label { get; set; } = string.Empty;
    public float Score { get; set; }
    public Dictionary<string, float> Metrics { get; set; } = new();
    public List<string> Reasons { get; set; } = new();
}

#endregion

#region Health Models

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}

public class ReadyResponse
{
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, bool> Checks { get; set; } = new();
    public string Timestamp { get; set; } = string.Empty;
}

public class ServiceInfoResponse
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public Dictionary<string, string> Endpoints { get; set; } = new();
    public string? Docs { get; set; }
}

#endregion

#region Fresh Produce Models

public class FreshProduceRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageB64 { get; set; }
}

public class FreshProduceResponse
{
    public List<FreshProduceInfo> DetectedItems { get; set; } = new();
    public Dictionary<string, object>? ImageQuality { get; set; }
    public float ProcessingTimeMs { get; set; }
    public List<string>? Warnings { get; set; }
}

public class FreshProduceInfo
{
    public string Category { get; set; } = string.Empty;
    public string? NameVi { get; set; }
    public string? NameEn { get; set; }
    public int? TypicalShelfLifeDays { get; set; }
    public string? StorageRecommendation { get; set; }
    public List<string>? FreshnessIndicators { get; set; }
    public float Confidence { get; set; }
}

#endregion

#region Unified Smart Scan Models

public class SmartScanRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageB64 { get; set; }
    public string ProductTypeHint { get; set; } = "auto";
    public bool LookupBarcode { get; set; } = true;
}

public class SmartScanResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ScanType { get; set; } = string.Empty;
    public OcrResponse? OcrResult { get; set; }
    public FreshProduceResponse? FreshProduceResult { get; set; }
    public VisionResponse? VisionResult { get; set; }
    public bool IsVietnameseProduct { get; set; }
    public BarcodeInfo? VietnameseBarcodeInfo { get; set; }
    public string? SuggestedCategory { get; set; }
    public int? SuggestedShelfLifeDays { get; set; }
    public string? StorageRecommendation { get; set; }
    public string? UsageInstructions { get; set; }
    public ManufacturerInfo? ManufacturerInfo { get; set; }
    public List<string>? QualityStandards { get; set; }
    public List<string>? Warnings { get; set; }
    public float ProcessingTimeMs { get; set; }
    public float Confidence { get; set; }
}

#endregion

#region Error Models

public class AIServiceError
{
    public bool Success { get; set; }
    public AIErrorDetail? Error { get; set; }
}

public class AIErrorDetail
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Details { get; set; }
}

#endregion

#region Market Price Models

public class MarketPriceCrawlRequest
{
    public string Barcode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public List<string>? Sources { get; set; }
}

public class MarketPriceCrawlResponse
{
    public bool Success { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public List<CrawledPrice> Prices { get; set; } = new();
    public MarketPriceStats? Stats { get; set; }
    public float ProcessingTimeMs { get; set; }
    public string? Error { get; set; }
}

public class CrawledPrice
{
    public string Source { get; set; } = string.Empty;
    public string? StoreName { get; set; }
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? Url { get; set; }
    public string? Unit { get; set; }
    public string? Weight { get; set; }
    public bool IsInStock { get; set; } = true;
    public float Confidence { get; set; }
}

public class MarketPriceStats
{
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AvgPrice { get; set; }
    public int SourceCount { get; set; }
    public List<string> Sources { get; set; } = new();
}

#endregion
