using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null, string? deviceInfo = null);
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
    Task<ApiResponse<bool>> LogoutAsync(string refreshToken);
    Task<ApiResponse<bool>> RevokeAllUserTokensAsync(Guid userId);
}
