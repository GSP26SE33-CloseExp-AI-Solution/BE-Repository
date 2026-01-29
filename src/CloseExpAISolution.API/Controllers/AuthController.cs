using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Mvc;

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
        var result = await _services.AuthService.LoginAsync(request);

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
}
