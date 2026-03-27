using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class UpsertCustomerAddressRequestDto
{
    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string RecipientName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string AddressLine { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
