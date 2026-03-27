using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class Promotion
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
    public int PerUserLimit { get; set; } = 1;
    public int UsedCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PromotionState Status { get; set; } = PromotionState.Draft;

    public Order? Order { get; set; }
    public ICollection<PromotionUsage> PromotionUsages { get; set; } = new List<PromotionUsage>();
    public Category? Category { get; set; }
}
