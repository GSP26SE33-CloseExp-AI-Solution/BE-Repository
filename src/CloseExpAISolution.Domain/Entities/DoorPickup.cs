namespace CloseExpAISolution.Domain.Entities;

public class DoorPickup
{
    public Guid DoorPickupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

