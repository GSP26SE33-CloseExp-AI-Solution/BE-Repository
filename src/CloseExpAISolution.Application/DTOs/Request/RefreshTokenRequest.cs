using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

/// <summary>
/// Request DTO for refreshing access token
/// </summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token không được để trống")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for logout (revoke refresh token)
/// </summary>
public class LogoutRequest
{
    [Required(ErrorMessage = "Refresh token không được để trống")]
    public string RefreshToken { get; set; } = string.Empty;
}
