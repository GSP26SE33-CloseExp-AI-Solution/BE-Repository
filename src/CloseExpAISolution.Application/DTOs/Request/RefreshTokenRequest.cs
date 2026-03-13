using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token không được để trống")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    [Required(ErrorMessage = "Refresh token không được để trống")]
    public string RefreshToken { get; set; } = string.Empty;
}
