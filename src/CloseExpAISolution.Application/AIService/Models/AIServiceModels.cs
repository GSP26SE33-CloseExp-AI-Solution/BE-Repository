namespace CloseExpAISolution.Application.AIService.Models;

#region OCR Models

/// <summary>
/// Request model for OCR extraction
/// </summary>
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

/// <summary>
/// Response model for OCR extraction
/// </summary>
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
    // Basic info
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public BarcodeInfo? BarcodeInfo { get; set; }
    
    // Weight/Volume
    public string? Weight { get; set; }
    public WeightInfo? WeightInfo { get; set; }
    
    // Ingredients and composition
    public List<string>? Ingredients { get; set; }
    public Dictionary<string, object>? NutritionFacts { get; set; }
    
    // Instructions
    public string? StorageInstructions { get; set; }
    public string? UsageInstructions { get; set; }
    
    // Manufacturer/Distributor
    public ManufacturerInfo? Manufacturer { get; set; }
    public string? Origin { get; set; }
    
    // Certifications and quality
    public List<string>? Certifications { get; set; }
    public List<string>? QualityStandards { get; set; }
    
    // Warnings and notes
    public List<string>? Warnings { get; set; }
    
    // Product codes
    public ProductCodesInfo? ProductCodes { get; set; }
    
    // Shelf life
    public int? ShelfLifeDays { get; set; }
    
    // Category detection
    public CategoryInfo? DetectedCategory { get; set; }
}

/// <summary>
/// Barcode origin information from AI service.
/// Note: Company and Category are populated by BarcodeLookupService using external APIs.
/// The AI service only provides barcode origin detection via GS1 prefix.
/// </summary>
public class BarcodeInfo
{
    public string Barcode { get; set; } = string.Empty;
    public bool IsVietnamese { get; set; }
    public string? Company { get; set; }
    public string? Category { get; set; }
    public string? Prefix { get; set; }
    public string? Note { get; set; }
    public string? Country { get; set; }  // Country of origin from GS1 prefix
}

/// <summary>
/// Product weight/volume information
/// </summary>
public class WeightInfo
{
    public float Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Raw { get; set; }
}

/// <summary>
/// Manufacturer and distributor information
/// </summary>
public class ManufacturerInfo
{
    public string? Name { get; set; }
    public string? Distributor { get; set; }
    public string? Address { get; set; }
    public List<string>? Contact { get; set; }
}

/// <summary>
/// Product identification codes (SKU, batch, etc.)
/// </summary>
public class ProductCodesInfo
{
    public string? Sku { get; set; }
    public string? Batch { get; set; }
    public string? Msktvsty { get; set; }  // Mã số kinh tế vệ sinh thú y
}

/// <summary>
/// Detected product category
/// </summary>
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

/// <summary>
/// Request model for price suggestion
/// </summary>
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
    
    // Market price data (from crawlers/crowdsource)
    public decimal? MinMarketPrice { get; set; }
    public decimal? AvgMarketPrice { get; set; }
    public string? MarketPriceSource { get; set; }
    public string? ProductName { get; set; }
    public string? Barcode { get; set; }
}

/// <summary>
/// Response model for price suggestion
/// </summary>
public class PricingResponse
{
    public decimal SuggestedPrice { get; set; }
    public float DiscountPercent { get; set; }
    public float Confidence { get; set; }
    public decimal MinSuggestedPrice { get; set; }
    public decimal MaxSuggestedPrice { get; set; }
    
    /// <summary>
    /// Khả năng bán được ở mức giá này (%)
    /// </summary>
    public float ExpectedSellRate { get; set; }
    
    /// <summary>
    /// Thời gian ước tính để bán hết
    /// </summary>
    public string EstimatedTimeToSell { get; set; } = string.Empty;
    
    /// <summary>
    /// Mức cạnh tranh thị trường (0 → 1)
    /// </summary>
    public float Competitiveness { get; set; }
    
    /// <summary>
    /// Các lý do AI đưa ra mức giá này
    /// </summary>
    public List<string> Reasons { get; set; } = new();
    
    /// <summary>
    /// Thông tin so sánh giá thị trường
    /// </summary>
    public MarketPriceInfo? MarketPriceInfo { get; set; }
    
    public Dictionary<string, object> Rationale { get; set; } = new();
    public PriceBreakdown? Breakdown { get; set; }
    public string UrgencyLevel { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public float? CalculationTimeMs { get; set; }
    public string? ModelVersion { get; set; }
}

/// <summary>
/// Market price comparison information
/// </summary>
public class MarketPriceInfo
{
    /// <summary>
    /// Giá thấp nhất trên thị trường
    /// </summary>
    public decimal? MinMarketPrice { get; set; }
    
    /// <summary>
    /// Giá trung bình trên thị trường
    /// </summary>
    public decimal? AvgMarketPrice { get; set; }
    
    /// <summary>
    /// Nguồn dữ liệu (crawl, google, crowdsource)
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// % chênh lệch so với giá thị trường (âm = rẻ hơn)
    /// </summary>
    public float? PriceVsMarketPercent { get; set; }
    
    /// <summary>
    /// Mô tả điều chỉnh đã áp dụng
    /// </summary>
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

/// <summary>
/// Request model for vision analysis
/// </summary>
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

/// <summary>
/// Response model for vision analysis
/// </summary>
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

/// <summary>
/// Request model for fresh produce identification
/// </summary>
public class FreshProduceRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageB64 { get; set; }
}

/// <summary>
/// Response model for fresh produce identification
/// </summary>
public class FreshProduceResponse
{
    public List<FreshProduceInfo> DetectedItems { get; set; } = new();
    public Dictionary<string, object>? ImageQuality { get; set; }
    public float ProcessingTimeMs { get; set; }
    public List<string>? Warnings { get; set; }
}

/// <summary>
/// Information about detected fresh produce
/// </summary>
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

/// <summary>
/// Unified request for smart product scanning
/// Automatically determines the appropriate AI endpoint based on image content
/// </summary>
public class SmartScanRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageB64 { get; set; }
    
    /// <summary>
    /// Hint about the product type to improve accuracy
    /// Options: "auto", "packaged", "fresh_produce", "barcode"
    /// Default is "auto" which will analyze the image to determine type
    /// </summary>
    public string ProductTypeHint { get; set; } = "auto";
    
    /// <summary>
    /// If true, will lookup barcode info from external databases
    /// </summary>
    public bool LookupBarcode { get; set; } = true;
}

/// <summary>
/// Unified response for smart product scanning
/// </summary>
public class SmartScanResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// The detected type of scan performed
    /// "packaged" - Product with barcode/packaging
    /// "fresh_produce" - Fresh vegetables, fruits, meat, seafood
    /// "mixed" - Contains both types
    /// </summary>
    public string ScanType { get; set; } = string.Empty;
    
    /// <summary>
    /// OCR extracted information (for packaged products)
    /// </summary>
    public OcrResponse? OcrResult { get; set; }
    
    /// <summary>
    /// Fresh produce detection results
    /// </summary>
    public FreshProduceResponse? FreshProduceResult { get; set; }
    
    /// <summary>
    /// Vision analysis results
    /// </summary>
    public VisionResponse? VisionResult { get; set; }
    
    /// <summary>
    /// Indicates if this is a Vietnamese product (based on barcode)
    /// </summary>
    public bool IsVietnameseProduct { get; set; }
    
    /// <summary>
    /// Vietnamese company info (if barcode starts with 893)
    /// </summary>
    public BarcodeInfo? VietnameseBarcodeInfo { get; set; }
    
    /// <summary>
    /// Suggested product category for the system
    /// </summary>
    public string? SuggestedCategory { get; set; }
    
    /// <summary>
    /// Suggested shelf life in days (if determinable)
    /// </summary>
    public int? SuggestedShelfLifeDays { get; set; }
    
    /// <summary>
    /// Storage recommendations
    /// </summary>
    public string? StorageRecommendation { get; set; }
    
    /// <summary>
    /// Usage instructions extracted from packaging
    /// </summary>
    public string? UsageInstructions { get; set; }
    
    /// <summary>
    /// Manufacturer/Distributor information
    /// </summary>
    public ManufacturerInfo? ManufacturerInfo { get; set; }
    
    /// <summary>
    /// Quality standards and certifications
    /// </summary>
    public List<string>? QualityStandards { get; set; }
    
    /// <summary>
    /// Product warnings and notes
    /// </summary>
    public List<string>? Warnings { get; set; }
    
    /// <summary>
    /// Total processing time in milliseconds
    /// </summary>
    public float ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// Confidence score for the overall scan result
    /// </summary>
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

/// <summary>
/// Request for market price crawling
/// </summary>
public class MarketPriceCrawlRequest
{
    public string Barcode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public List<string>? Sources { get; set; }
}

/// <summary>
/// Response from market price crawler
/// </summary>
public class MarketPriceCrawlResponse
{
    public bool Success { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public List<CrawledPrice> Prices { get; set; } = new();
    public MarketPriceStats? Stats { get; set; }
    public float ProcessingTimeMs { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Individual price from a source
/// </summary>
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

/// <summary>
/// Market price statistics
/// </summary>
public class MarketPriceStats
{
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AvgPrice { get; set; }
    public int SourceCount { get; set; }
    public List<string> Sources { get; set; } = new();
}

#endregion
