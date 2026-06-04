namespace CloseExpAISolution.Application.DTOs.Response;

public class CustomerPromotionOptionDto
{
    public Guid PromotionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int PerUserLimit { get; set; }
    public int UserUsageCount { get; set; }
    public bool CanApply { get; set; }
    public bool IsDisabled { get; set; }
    public string? DisabledReason { get; set; }
    public decimal PreviewDiscountAmount { get; set; }
    public decimal PreviewFinalAmount { get; set; }
}
