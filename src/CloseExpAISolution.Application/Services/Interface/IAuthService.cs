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

    // Email verification (OTP)
    Task<ApiResponse<bool>> VerifyOtpAsync(VerifyOtpRequest request);
    Task<ApiResponse<bool>> ResendOtpAsync(ResendOtpRequest request);

    // Forgot / Reset password
    Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request);

    // Google OAuth
    Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request, string? ipAddress = null, string? deviceInfo = null);
}
