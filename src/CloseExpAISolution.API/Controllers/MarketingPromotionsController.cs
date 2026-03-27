using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/marketing/promotions")]
[Authorize(Roles = "MarketingStaff,Admin")]
public class MarketingPromotionsController : ControllerBase
{
    private readonly IServiceProviders _services;

    public MarketingPromotionsController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AdminPromotionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromotions(CancellationToken cancellationToken = default)
    {
        var data = await _services.PromotionService.GetPromotionsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<AdminPromotionDto>>.SuccessResponse(data));
    }

    [HttpGet("{promotionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPromotionById(Guid promotionId, CancellationToken cancellationToken = default)
    {
        var data = await _services.PromotionService.GetPromotionByIdAsync(promotionId, cancellationToken);
        if (data == null)
            return NotFound(ApiResponse<AdminPromotionDto>.ErrorResponse("Không tìm thấy khuyến mãi"));

        return Ok(ApiResponse<AdminPromotionDto>.SuccessResponse(data));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _services.PromotionService.CreatePromotionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetPromotionById), new { promotionId = data.PromotionId }, ApiResponse<AdminPromotionDto>.SuccessResponse(data));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AdminPromotionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{promotionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePromotion(Guid promotionId, [FromBody] UpdatePromotionRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _services.PromotionService.UpdatePromotionAsync(promotionId, request, cancellationToken);
            if (data == null)
                return NotFound(ApiResponse<AdminPromotionDto>.ErrorResponse("Không tìm thấy khuyến mãi"));

            return Ok(ApiResponse<AdminPromotionDto>.SuccessResponse(data));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AdminPromotionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPatch("{promotionId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePromotionStatus(Guid promotionId, [FromBody] UpdatePromotionStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _services.PromotionService.UpdatePromotionStatusAsync(promotionId, request.Status, cancellationToken);
            if (data == null)
                return NotFound(ApiResponse<AdminPromotionDto>.ErrorResponse("Không tìm thấy khuyến mãi"));

            return Ok(ApiResponse<AdminPromotionDto>.SuccessResponse(data));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AdminPromotionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("usages")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PromotionUsageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsages([FromQuery] PromotionUsageFilterRequestDto request, CancellationToken cancellationToken = default)
    {
        var data = await _services.PromotionUsageService.GetUsagesAsync(request, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<PromotionUsageDto>>.SuccessResponse(data));
    }

    [HttpGet("analytics/overview")]
    [ProducesResponseType(typeof(ApiResponse<PromotionAnalyticsOverviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalyticsOverview([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var data = await _services.PromotionAnalyticsService.GetOverviewAsync(fromUtc, toUtc, cancellationToken);
        return Ok(ApiResponse<PromotionAnalyticsOverviewDto>.SuccessResponse(data));
    }

    [HttpGet("analytics/top")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AdminPromotionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopPromotions([FromQuery] string metric = "usage", [FromQuery] int limit = 10, [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null, CancellationToken cancellationToken = default)
    {
        var data = await _services.PromotionAnalyticsService.GetTopPromotionsAsync(metric, limit, fromUtc, toUtc, cancellationToken);
        return Ok(ApiResponse<IEnumerable<AdminPromotionDto>>.SuccessResponse(data));
    }

    [HttpGet("{promotionId:guid}/analytics/trend")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PromotionTrendPointDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromotionTrend(Guid promotionId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var data = await _services.PromotionAnalyticsService.GetPromotionTrendAsync(promotionId, fromUtc, toUtc, cancellationToken);
        return Ok(ApiResponse<IEnumerable<PromotionTrendPointDto>>.SuccessResponse(data));
    }

    [HttpGet("my-usages")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PromotionUsageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyUsages([FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));

        var request = new PromotionUsageFilterRequestDto
        {
            UserId = userId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var data = await _services.PromotionUsageService.GetUsagesAsync(request, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<PromotionUsageDto>>.SuccessResponse(data));
    }
}
