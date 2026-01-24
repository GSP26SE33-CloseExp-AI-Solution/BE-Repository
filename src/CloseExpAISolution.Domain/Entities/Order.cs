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
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public Guid? DoorPickupId { get; set; }
    public Guid? PromotionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public TimeSlot? TimeSlot { get; set; }
    public PickupPoint? PickupPoint { get; set; }
    public DoorPickup? DoorPickup { get; set; }
    public Promotion? Promotion { get; set; }


    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

