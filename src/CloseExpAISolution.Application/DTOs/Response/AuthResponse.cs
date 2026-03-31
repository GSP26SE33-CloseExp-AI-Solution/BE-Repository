using CloseExpAISolution.Application.DTOs;

namespace CloseExpAISolution.Application.DTOs.Response;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponseDto? User { get; set; }

    /// <summary>True when staff must call select-staff-context (multiple active personas).</summary>
    public bool RequiresStaffContext { get; set; }
}