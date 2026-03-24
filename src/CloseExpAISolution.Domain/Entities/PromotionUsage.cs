namespace CloseExpAISolution.Domain.Entities;

/// <summary>
/// Tracks individual user usage of promotions to prevent duplicate use.
/// </summary>
public class PromotionUsage
{
    public Guid UsageId { get; set; }
    public Guid PromotionId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime UsedAt { get; set; }

    public Promotion? Promotion { get; set; }
    public User? User { get; set; }
    public Order? Order { get; set; }
}
