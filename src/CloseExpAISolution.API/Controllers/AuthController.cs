using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IServiceProviders _services;

    public AuthController(IServiceProviders services)
    {
        _services = services;
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = GetIpAddress();
        var deviceInfo = GetDeviceInfo();

        var result = await _services.AuthService.LoginAsync(request, ipAddress, deviceInfo);

        if (!result.Success)
        {
            // Return 404 for user not found, 400 for invalid credentials
            if (result.Message != null && (result.Message.Contains("không tìm thấy") || result.Message.Contains("đã bị xóa")))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Register a new user account (Public registration for Vendor or MarketStaff only)
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///         "fullName": "John Doe",
    ///         "email": "john@example.com",
    ///         "phone": "+84901234567",
    ///         "password": "SecureP@ssw0rd",
    ///         "registrationType": 1  // 1 = Vendor, 2 = MarketStaff
    ///     }
    /// 
    /// Registration Types:
    /// - 1 (Vendor): Small restaurant/retail seller
    /// - 2 (MarketStaff): Supermarket staff
    /// 
    /// Other roles (Admin, Staff, SupplierStaff, DeliveryStaff) can only be created by Admin via /api/users endpoint
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _services.AuthService.RegisterAsync(request);

        if (!result.Success)
        {
            // Return 409 Conflict for duplicate email
            if (result.Message != null && result.Message.Contains("đã được đăng ký"))
            {
                return Conflict(result);
            }
            return BadRequest(result);
        }

        return Created(string.Empty, result);
    }

    /// <summary>
    /// Refresh access token using a valid refresh token
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/refresh-token
    ///     {
    ///         "refreshToken": "your_refresh_token_here"
    ///     }
    /// 
    /// Flow:
    /// 1. Client sends expired access token scenario
    /// 2. Call this endpoint with the refresh token received during login
    /// 3. Receive new access token and new refresh token (token rotation)
    /// 4. Old refresh token is invalidated
    /// 
    /// Security Notes:
    /// - Refresh tokens are rotated on each use
    /// - If a revoked token is reused, all user sessions are invalidated (security measure)
    /// </remarks>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = GetIpAddress();

        var result = await _services.AuthService.RefreshTokenAsync(request.RefreshToken, ipAddress);

        if (!result.Success)
        {
            // Return 401 for invalid/expired tokens
            if (result.Message != null && (result.Message.Contains("không hợp lệ") ||
                result.Message.Contains("hết hạn") ||
                result.Message.Contains("thu hồi")))
            {
                return Unauthorized(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Logout and invalidate the current refresh token
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/logout
    ///     {
    ///         "refreshToken": "your_refresh_token_here"
    ///     }
    /// 
    /// Note: This only invalidates the specific refresh token.
    /// To logout from all devices, use /api/auth/logout-all endpoint.
    /// </remarks>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var result = await _services.AuthService.LogoutAsync(request.RefreshToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Logout from all devices (revoke all refresh tokens)
    /// </summary>
    /// <remarks>
    /// Requires authentication. Revokes all active refresh tokens for the current user.
    /// Useful when user suspects their account is compromised.
    /// </remarks>
    [Authorize]
    [HttpPost("logout-all")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Không thể xác định người dùng"));
        }

        var result = await _services.AuthService.RevokeAllUserTokensAsync(userId);
        return Ok(result);
    }

    #region Private Helpers

    /// <summary>
    /// Gets the client IP address from the request
    /// </summary>
    private string? GetIpAddress()
    {
        // Check for forwarded IP (when behind proxy/load balancer)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Gets the device/browser info from User-Agent header
    /// </summary>
    private string? GetDeviceInfo()
    {
        return Request.Headers["User-Agent"].FirstOrDefault();
    }

    #endregion
}
