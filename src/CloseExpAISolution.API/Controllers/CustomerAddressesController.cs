using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerAddressesController : ControllerBase
{
    private readonly IServiceProviders _services;

    public CustomerAddressesController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CustomerAddressResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CustomerAddressResponseDto>>>> GetMyAddresses(CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        var items = await _services.CustomerAddressService.GetByUserIdAsync(userId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<CustomerAddressResponseDto>>.SuccessResponse(items));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerAddressResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CustomerAddressResponseDto>>> GetMyAddressById(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        var item = await _services.CustomerAddressService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy địa chỉ"));
        if (item.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.ErrorResponse("Bạn không có quyền truy cập địa chỉ này."));

        return Ok(ApiResponse<CustomerAddressResponseDto>.SuccessResponse(item));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdString, out userId);
    }
}
