using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class Order
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid TimeSlotId { get; set; }
    public Guid? CollectionId { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public OrderState Status { get; set; } = OrderState.Pending;
    public DateTime OrderDate { get; set; }
    public Guid? AddressId { get; set; }
    public Guid? PromotionId { get; set; }
    public Guid? DeliveryGroupId { get; set; }
    public string? DeliveryNote { get; set; }
    public decimal DeliveryFee { get; set; }
    public DateTime? CancelDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public DeliveryTimeSlot? DeliveryTimeSlot { get; set; }
    public CollectionPoint? CollectionPoint { get; set; }
    public CustomerAddress? CustomerAddress { get; set; }
    public Promotion? Promotion { get; set; }
    public DeliveryGroup? DeliveryGroup { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    public ICollection<DeliveryLog> DeliveryLogs { get; set; } = new List<DeliveryLog>();
    public ICollection<OrderStatusLog> StatusLogs { get; set; } = new List<OrderStatusLog>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
