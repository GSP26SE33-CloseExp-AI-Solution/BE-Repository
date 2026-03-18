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
    public NewSupermarketRequest? NewSupermarket { get; set; }
    public string? Position { get; set; }
}
public class NewSupermarketRequest
{
    [Required(ErrorMessage = "Tên siêu thị không được để trống")]
    [StringLength(200, ErrorMessage = "Tên siêu thị không được quá 200 ký tự")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ không được để trống")]
    [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Latitude phải trong khoảng -90 đến 90")]
    public decimal Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude phải trong khoảng -180 đến 180")]
    public decimal Longitude { get; set; }

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string ContactPhone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? ContactEmail { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RegistrationType
{
    Vendor = (int)RoleUser.Vendor,
    SupermarketStaff = (int)RoleUser.SupermarketStaff
}
