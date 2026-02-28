using System.ComponentModel.DataAnnotations;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs;

public class UserDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateUserRequestDto
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
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public int RoleId { get; set; }
}

public class UpdateUserRequestDto
{
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string? FullName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? Phone { get; set; }

    public UserState? Status { get; set; }
    public int? RoleId { get; set; }
}

public class UpdateProfileRequestDto
{
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    public string? FullName { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }
}

public class UpdateUserStatusRequestDto
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public UserState Status { get; set; }
}

public class UserResponseDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public UserState Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public MarketStaffInfoDto? MarketStaffInfo { get; set; }
}

public class MarketStaffInfoDto
{
    public Guid MarketStaffId { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }

    public SupermarketBasicInfoDto? Supermarket { get; set; }
}

public class SupermarketBasicInfoDto
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
}
