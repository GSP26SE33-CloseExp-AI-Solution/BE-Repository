using System.ComponentModel.DataAnnotations;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Request;

/// <summary>
/// Public registration request (for Vendor or MarketStaff only)
/// </summary>
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
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Registration type: Only Vendor or MarketStaff allowed for public registration
    /// </summary>
    [Required(ErrorMessage = "Registration type is required")]
    public RegistrationType RegistrationType { get; set; }
}

/// <summary>
/// Allowed registration types for public registration
/// </summary>
public enum RegistrationType
{
    /// <summary>
    /// Register as Vendor (Small restaurant/retail seller)
    /// </summary>
    Vendor = (int)RoleUser.Vendor,

    /// <summary>
    /// Register as MarketStaff (Supermarket staff)
    /// </summary>
    MarketStaff = (int)RoleUser.MarketStaff
}
