namespace CloseExpAISolution.Domain.Entities;

public class DeliveryTimeSlot
{
    public Guid TimeSlotId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
