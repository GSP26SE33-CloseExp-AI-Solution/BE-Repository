using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Request;

public class RegisterRequest
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.])[A-Za-z\d@$!%*?&.]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Registration type is required")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RegistrationType RegistrationType { get; set; }

    public Guid? SupermarketId { get; set; }

    public string? Position { get; set; }
}

/// <summary>
/// Registration types available for public registration.
/// Use string values: "Vendor" or "SupplierStaff"
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RegistrationType
{
    Vendor = (int)RoleUser.Vendor,
    
    SupplierStaff = (int)RoleUser.SupplierStaff
}
