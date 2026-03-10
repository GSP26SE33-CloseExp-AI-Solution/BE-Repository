namespace CloseExpAISolution.Domain.Entities;

/// <summary>
/// Customer delivery address. Replaces DoorPickup.
/// </summary>
public class CustomerAddress
{
    public Guid CustomerAddressId { get; set; }
    public Guid UserId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public bool IsDefault { get; set; }


    public User? User { get; set; }
}
