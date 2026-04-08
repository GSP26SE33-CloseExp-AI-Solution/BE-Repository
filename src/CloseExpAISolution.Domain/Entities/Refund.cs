using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class Refund
{
    public Guid RefundId { get; set; }
    public Guid OrderId { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? RefundedOrderItemIdsJson { get; set; }
    public RefundState Status { get; set; } = RefundState.Pending;
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Order? Order { get; set; }
    public Transaction? Transaction { get; set; }
}
