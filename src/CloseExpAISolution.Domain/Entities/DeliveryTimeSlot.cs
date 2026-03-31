namespace CloseExpAISolution.Domain.Entities;

public class DeliveryTimeSlot
{
    public Guid DeliveryTimeSlotId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
