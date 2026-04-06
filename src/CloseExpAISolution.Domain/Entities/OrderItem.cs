using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class OrderItem
{
    public Guid OrderItemId { get; set; }
    public Guid OrderId { get; set; }
    public Guid LotId { get; set; }
    public short Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public PackagingState PackagingStatus { get; set; } = PackagingState.Pending;
    public DeliveryState? DeliveryStatus { get; set; }
    public DateTime? PackagedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryFailedReason { get; set; }
    public Guid? DeliveryGroupId { get; set; }

    public Order? Order { get; set; }
    public StockLot? StockLot { get; set; }
    public DeliveryGroup? DeliveryGroup { get; set; }
}
