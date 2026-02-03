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
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public string? Weight { get; set; }
    public List<string>? Ingredients { get; set; }
    public Dictionary<string, string>? NutritionFacts { get; set; }
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
    public Dictionary<string, object> Rationale { get; set; } = new();
    public PriceBreakdown? Breakdown { get; set; }
    public string UrgencyLevel { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public float? CalculationTimeMs { get; set; }
    public string? ModelVersion { get; set; }
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
