namespace CloseExpAISolution.Application.DTOs.Response;

public class DeliveryTimeSlotDto
{
    public Guid TimeSlotId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string DisplayTimeRange { get; set; } = string.Empty;
    public int RelatedOrderCount { get; set; }
}

public class PickupPointDto
{
    public Guid PickupPointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int RelatedOrderCount { get; set; }

    /// <summary>Khoảng cách ước lượng từ điểm tham chiếu (km), có khi gọi nearby.</summary>
    public double? DistanceKm { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class CustomerAddressDto
{
    public Guid AddressId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
