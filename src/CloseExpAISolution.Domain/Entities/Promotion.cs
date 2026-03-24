using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class Promotion
{
    public Guid PromotionId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public int MaxUsage { get; set; }
    public int UsedCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PromotionState Status { get; set; } = PromotionState.Draft;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<PromotionUsage> PromotionUsages { get; set; } = new List<PromotionUsage>();
    public Category? Category { get; set; }
}
