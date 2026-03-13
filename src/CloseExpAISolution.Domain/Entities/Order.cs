namespace CloseExpAISolution.Domain.Entities;

public class Order
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid TimeSlotId { get; set; }
    public Guid? PickupPointId { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public Guid AddressId { get; set; }
    public Guid? PromotionId { get; set; }
    public Guid? DeliveryGroupId { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryNote { get; set; }
    public decimal DeliveryFee { get; set; }
    public DateTime? CancelDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public DeliveryTimeSlot? TimeSlot { get; set; }
    public CollectionPoint? PickupPoint { get; set; }
    public CustomerAddress? CustomerAddress { get; set; }
    public Promotion? Promotion { get; set; }
    public DeliveryGroup? DeliveryGroup { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<DeliveryLog> DeliveryLogs { get; set; } = new List<DeliveryLog>();
}
