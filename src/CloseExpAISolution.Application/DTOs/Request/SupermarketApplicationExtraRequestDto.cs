using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class SelectStaffContextRequestDto
{
    [Required(ErrorMessage = "Mã nhân viên là bắt buộc")]
    [StringLength(32, MinimumLength = 4)]
    public string EmployeeCode { get; set; } = string.Empty;
}

public class RejectSupermarketApplicationRequestDto
{
    [StringLength(2000)]
    public string? AdminReviewNote { get; set; }
}

/// <summary>Manager creates another staff persona (same UserId, new employee code) for shared login.</summary>
public class CreateStaffPersonaRequestDto
{
    [Required(ErrorMessage = "Chức danh/vị trí là bắt buộc")]
    [StringLength(100)]
    public string Position { get; set; } = string.Empty;
}
