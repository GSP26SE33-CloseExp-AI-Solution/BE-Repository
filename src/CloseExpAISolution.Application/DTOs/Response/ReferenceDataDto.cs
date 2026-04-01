namespace CloseExpAISolution.Application.DTOs.Response;

public class DeliveryTimeSlotDto
{
    public Guid TimeSlotId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string DisplayTimeRange { get; set; } = string.Empty;
    public int RelatedOrderCount { get; set; }
}

public class CollectionPointDto
{
    public Guid CollectionPointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int RelatedOrderCount { get; set; }
}

public class CustomerAddressDto
{
    public Guid AddressId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
