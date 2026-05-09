using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CloseExpAISolution.Application.DTOs.Request;
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
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

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
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

        var item = await _services.CustomerAddressService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy địa chỉ"));
        if (item.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.ErrorResponse("Bạn không có quyền truy cập địa chỉ này"));

        return Ok(ApiResponse<CustomerAddressResponseDto>.SuccessResponse(item));
    }

    [HttpGet("default")]
    [ProducesResponseType(typeof(ApiResponse<CustomerAddressResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CustomerAddressResponseDto>>> GetMyDefaultAddress(CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

        var item = await _services.CustomerAddressService.GetDefaultAddressAsync(userId, cancellationToken);
        if (item == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Chưa có địa chỉ mặc định"));

        return Ok(ApiResponse<CustomerAddressResponseDto>.SuccessResponse(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerAddressResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CustomerAddressResponseDto>>> CreateAddress([FromBody] CreateCustomerAddressDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.CustomerAddressService.CreateAsync(userId, request);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerAddressResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CustomerAddressResponseDto>>> UpdateAddress(Guid id, [FromBody] UpdateCustomerAddressDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.CustomerAddressService.UpdateAsync(userId, id, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAddress(Guid id)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.CustomerAddressService.DeleteAsync(userId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/set-default")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> SetDefaultAddress(Guid id)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.CustomerAddressService.SetDefaultAsync(userId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdString, out userId);
    }
}
