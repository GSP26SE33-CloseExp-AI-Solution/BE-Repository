using System.ComponentModel.DataAnnotations;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs;

/// <summary>
/// Main User DTO for transferring user data between layers
/// </summary>
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

/// <summary>
/// Request DTO for creating a new user (Admin only)
/// </summary>
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

/// <summary>
/// Request DTO for updating user information (Admin only)
/// </summary>
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

/// <summary>
/// Request DTO for user to update their own profile (without status/role)
/// </summary>
public class UpdateProfileRequestDto
{
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    public string? FullName { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }
}

/// <summary>
/// Request DTO for updating user status only (Admin only)
/// Used for verifying/banning user accounts
/// </summary>
public class UpdateUserStatusRequestDto
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public UserState Status { get; set; }
}

/// <summary>
/// Response DTO for user API output
/// </summary>
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
}
