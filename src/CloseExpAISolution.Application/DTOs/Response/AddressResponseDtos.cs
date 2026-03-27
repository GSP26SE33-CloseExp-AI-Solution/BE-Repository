namespace CloseExpAISolution.Application.DTOs.Response;

public class CollectionPointResponseDto
{
    public Guid CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
}

public class CustomerAddressResponseDto
{
    public Guid CustomerAddressId { get; set; }
    public Guid UserId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsDefault { get; set; }
}
