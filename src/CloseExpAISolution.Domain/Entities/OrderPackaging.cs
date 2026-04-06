using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class OrderPackaging
{
    public Guid PackagingId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? OrderItemId { get; set; }
    public Guid UserId { get; set; }
    public PackagingState Status { get; set; } = PackagingState.Pending;
    public DateTime? PackagedAt { get; set; }

    public User? User { get; set; }
    public Order? Order { get; set; }
    public OrderItem? OrderItem { get; set; }
}