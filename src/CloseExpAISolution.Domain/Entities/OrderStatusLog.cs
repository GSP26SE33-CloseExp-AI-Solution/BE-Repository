using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class OrderStatusLog
{
    public Guid LogId { get; set; }
    public Guid OrderId { get; set; }
    public OrderState FromStatus { get; set; }
    public OrderState ToStatus { get; set; }
    public string? ChangedBy { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }

    public Order? Order { get; set; }
}
