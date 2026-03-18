namespace CloseExpAISolution.Domain.Entities;

public class DeliveryGroup
{
    public Guid DeliveryGroupId { get; set; }

    public string GroupCode { get; set; } = string.Empty;
    public Guid? DeliveryStaffId { get; set; }
    public Guid DeliveryTimeSlotId { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
    public string DeliveryArea { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public string? Notes { get; set; }
    public DateTime DeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? DeliveryStaff { get; set; }
    public DeliveryTimeSlot? DeliveryTimeSlot { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
