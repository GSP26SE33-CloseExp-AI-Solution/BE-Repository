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

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerAddressResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CustomerAddressResponseDto>>> Create(
        [FromBody] UpsertCustomerAddressRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        var created = await _services.CustomerAddressService.CreateAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetMyAddressById), new { id = created.CustomerAddressId },
            ApiResponse<CustomerAddressResponseDto>.SuccessResponse(created, "Tạo địa chỉ thành công"));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerAddressResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CustomerAddressResponseDto>>> Update(
        Guid id,
        [FromBody] UpsertCustomerAddressRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        var updated = await _services.CustomerAddressService.UpdateAsync(id, userId, request, cancellationToken);
        if (updated == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy địa chỉ"));
        return Ok(ApiResponse<CustomerAddressResponseDto>.SuccessResponse(updated, "Cập nhật địa chỉ thành công"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        var ok = await _services.CustomerAddressService.DeleteAsync(id, userId, cancellationToken);
        if (!ok)
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy địa chỉ"));
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Xóa địa chỉ thành công"));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdString, out userId);
    }
}
