using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

/// <summary>
/// Xác nhận email bằng OTP 6 số
/// </summary>
public class VerifyOtpRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã OTP là bắt buộc")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải đúng 6 số")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải là 6 chữ số")]
    public string OtpCode { get; set; } = string.Empty;
}

/// <summary>
/// Gửi lại mã OTP xác nhận email
/// </summary>
public class ResendOtpRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Yêu cầu đặt lại mật khẩu - gửi OTP về email
/// </summary>
public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Đặt lại mật khẩu bằng OTP
/// </summary>
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã OTP là bắt buộc")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải đúng 6 số")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải là 6 chữ số")]
    public string OtpCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 100 ký tự")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Đăng nhập bằng Google OAuth IdToken
/// </summary>
public class GoogleLoginRequest
{
    [Required(ErrorMessage = "Google IdToken là bắt buộc")]
    public string IdToken { get; set; } = string.Empty;

    /// <summary>
    /// Loại đăng ký (chỉ cần khi user chưa tồn tại - tạo tài khoản mới)
    /// </summary>
    public RegistrationType? RegistrationType { get; set; }

    /// <summary>
    /// SupermarketId (bắt buộc nếu RegistrationType = SupplierStaff)
    /// </summary>
    public Guid? SupermarketId { get; set; }

    /// <summary>
    /// Chức vụ (optional, default "Nhân viên")
    /// </summary>
    public string? Position { get; set; }
}
