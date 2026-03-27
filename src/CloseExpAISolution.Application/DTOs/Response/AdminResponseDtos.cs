namespace CloseExpAISolution.Application.DTOs.Response;

public class AdminDashboardOverviewDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveSupermarkets { get; set; }
    public int SlaBreachedOrders { get; set; }
}

public class AdminRevenueTrendPointDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class AdminSlaAlertDto
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int MinutesLate { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}

public class AdminTimeSlotDto
{
    public Guid TimeSlotId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class AdminCollectionPointDto
{
    public Guid CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

public class AdminSystemConfigDto
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class AdminUnitDto
{
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdminPromotionDto
{
    public Guid PromotionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int MaxUsage { get; set; }
    public int UsedCount { get; set; }
    public int PerUserLimit { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PromotionValidationResultDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? PromotionId { get; set; }
    public string? PromotionCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal FinalAmount { get; set; }
}

public class PromotionUsageDto
{
    public Guid UsageId { get; set; }
    public Guid PromotionId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime UsedAt { get; set; }
}

public class PromotionAnalyticsOverviewDto
{
    public int TotalPromotionUsages { get; set; }
    public int UniqueUsers { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal GrossRevenueAffected { get; set; }
    public decimal NetRevenueAffected { get; set; }
    public decimal AvgDiscountPerUsage { get; set; }
}

public class PromotionTrendPointDto
{
    public DateTime Date { get; set; }
    public int UsageCount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetRevenueAffected { get; set; }
}

public class AdminAiPriceHistoryDto
{
    public Guid AiPriceId { get; set; }
    public Guid LotId { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal? MarketAvgPrice { get; set; }
    public float AiConfidence { get; set; }
    public bool AcceptedSuggestion { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
