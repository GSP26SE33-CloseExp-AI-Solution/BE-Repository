using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class RefundEmailOutbox
{
    public Guid EmailOutboxId { get; set; }
    public Guid RefundId { get; set; }
    public RefundNotificationKind Kind { get; set; }
    public RefundEmailOutboxStatus Status { get; set; } = RefundEmailOutboxStatus.Pending;
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public Refund? Refund { get; set; }
}
