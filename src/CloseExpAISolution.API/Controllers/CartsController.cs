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
public class CartsController : ControllerBase
{
    private readonly IServiceProviders _services;

    public CartsController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet("my-cart")]
    [ProducesResponseType(typeof(ApiResponse<CartResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CartResponseDto>>> GetMyCart(CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        var data = await _services.CartService.GetMyCartAsync(userId, cancellationToken);
        return Ok(ApiResponse<CartResponseDto>.SuccessResponse(data));
    }

    [HttpPost("my-cart/items")]
    [ProducesResponseType(typeof(ApiResponse<CartResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CartResponseDto>>> AddItem([FromBody] AddCartItemRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        try
        {
            var data = await _services.CartService.AddItemAsync(userId, request, cancellationToken);
            return Ok(ApiResponse<CartResponseDto>.SuccessResponse(data, "Thêm vào giỏ hàng thành công"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CartResponseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("my-cart/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CartResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CartResponseDto>>> UpdateItem(Guid itemId, [FromBody] UpdateCartItemRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        try
        {
            var data = await _services.CartService.UpdateItemAsync(userId, itemId, request, cancellationToken);
            return Ok(ApiResponse<CartResponseDto>.SuccessResponse(data, "Cập nhật giỏ hàng thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CartResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CartResponseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("my-cart/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CartResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CartResponseDto>>> RemoveItem(Guid itemId, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        try
        {
            var data = await _services.CartService.RemoveItemAsync(userId, itemId, cancellationToken);
            return Ok(ApiResponse<CartResponseDto>.SuccessResponse(data, "Xóa sản phẩm khỏi giỏ hàng thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CartResponseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("my-cart/items")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Clear(CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        await _services.CartService.ClearAsync(userId, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Đã xóa toàn bộ giỏ hàng"));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdString, out userId);
    }
}
