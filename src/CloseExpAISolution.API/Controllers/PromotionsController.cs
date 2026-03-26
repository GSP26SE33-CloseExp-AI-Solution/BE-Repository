using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController : ControllerBase
{
    private readonly IServiceProviders _services;

    public PromotionsController(IServiceProviders services)
    {
        _services = services;
    }

    [Authorize(Roles = "Vendor")]
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<PromotionValidationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidatePromotion([FromBody] ValidatePromotionRequestDto request, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

        var data = await _services.PromotionService.ValidatePromotionAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<PromotionValidationResultDto>.SuccessResponse(data));
    }

    [Authorize(Roles = "Vendor")]
    [HttpPost("orders/{orderId:guid}/apply")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyPromotionToOrder(Guid orderId, [FromBody] ApplyPromotionToOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

        try
        {
            var data = await _services.OrderService.ApplyPromotionAsync(orderId, userId, request, cancellationToken);
            return Ok(ApiResponse<OrderResponseDto>.SuccessResponse(data, "Áp dụng khuyến mãi thành công"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [Authorize(Roles = "Vendor")]
    [HttpGet("my-usages")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PromotionUsageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyUsages([FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

        var filter = new PromotionUsageFilterRequestDto
        {
            UserId = userId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var data = await _services.PromotionUsageService.GetUsagesAsync(filter, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<PromotionUsageDto>>.SuccessResponse(data));
    }
}
