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
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
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
