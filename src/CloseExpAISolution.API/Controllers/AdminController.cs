using System.Security.Claims;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IServiceProviders _services;

    public AdminController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet("dashboard/overview")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardOverviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardOverview([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken)
    {
        var data = await _services.AdminService.GetDashboardOverviewAsync(fromUtc, toUtc, cancellationToken);
        return Ok(ApiResponse<AdminDashboardOverviewDto>.SuccessResponse(data));
    }

    [HttpGet("dashboard/revenue-trend")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AdminRevenueTrendPointDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] int days = 7, CancellationToken cancellationToken = default)
    {
        var data = await _services.AdminService.GetRevenueTrendAsync(days, cancellationToken);
        return Ok(ApiResponse<IEnumerable<AdminRevenueTrendPointDto>>.SuccessResponse(data));
    }

    [HttpGet("dashboard/sla-alerts")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AdminSlaAlertDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSlaAlerts([FromQuery] int thresholdMinutes = 120, [FromQuery] int top = 50, CancellationToken cancellationToken = default)
    {
        var data = await _services.AdminService.GetSlaAlertsAsync(thresholdMinutes, top, cancellationToken);
        return Ok(ApiResponse<IEnumerable<AdminSlaAlertDto>>.SuccessResponse(data));
    }

    [HttpGet("system-config/time-slots")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AdminTimeSlotDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeSlots(CancellationToken cancellationToken)
    {
        var data = await _services.AdminService.GetTimeSlotsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<AdminTimeSlotDto>>.SuccessResponse(data));
    }

    [HttpPost("system-config/time-slots")]
    [ProducesResponseType(typeof(ApiResponse<AdminTimeSlotDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AdminTimeSlotDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTimeSlot([FromBody] UpsertTimeSlotRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _services.AdminService.CreateTimeSlotAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetTimeSlots), ApiResponse<AdminTimeSlotDto>.SuccessResponse(data, "Tạo khung giờ thành công"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminTimeSlotDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("system-config/time-slots/{timeSlotId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminTimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminTimeSlotDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AdminTimeSlotDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTimeSlot(Guid timeSlotId, [FromBody] UpsertTimeSlotRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _services.AdminService.UpdateTimeSlotAsync(timeSlotId, request, cancellationToken);
            if (data == null)
                return NotFound(ApiResponse<AdminTimeSlotDto>.ErrorResponse("Không tìm thấy khung giờ"));

            return Ok(ApiResponse<AdminTimeSlotDto>.SuccessResponse(data, "Cập nhật khung giờ thành công"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminTimeSlotDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("system-config/time-slots/{timeSlotId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTimeSlot(Guid timeSlotId, CancellationToken cancellationToken)
    {
        var ok = await _services.AdminService.DeleteTimeSlotAsync(timeSlotId, cancellationToken);
        if (!ok)
            return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy khung giờ"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa khung giờ thành công"));
    }

    [HttpGet("system-config/collection-points")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AdminCollectionPointDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCollectionPoints(CancellationToken cancellationToken)
    {
        var data = await _services.AdminService.GetCollectionPointsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<AdminCollectionPointDto>>.SuccessResponse(data));
    }

    [HttpPost("system-config/collection-points")]
    [ProducesResponseType(typeof(ApiResponse<AdminCollectionPointDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCollectionPoint([FromBody] UpsertCollectionPointRequestDto request, CancellationToken cancellationToken)
    {
        var data = await _services.AdminService.CreateCollectionPointAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCollectionPoints), ApiResponse<AdminCollectionPointDto>.SuccessResponse(data, "Tạo điểm tập kết thành công"));
    }

    [HttpPut("system-config/collection-points/{collectionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminCollectionPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminCollectionPointDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCollectionPoint(Guid collectionId, [FromBody] UpsertCollectionPointRequestDto request, CancellationToken cancellationToken)
    {
        var data = await _services.AdminService.UpdateCollectionPointAsync(collectionId, request, cancellationToken);
        if (data == null)
            return NotFound(ApiResponse<AdminCollectionPointDto>.ErrorResponse("Không tìm thấy điểm tập kết"));

        return Ok(ApiResponse<AdminCollectionPointDto>.SuccessResponse(data, "Cập nhật điểm tập kết thành công"));
    }

    [HttpDelete("system-config/collection-points/{collectionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCollectionPoint(Guid collectionId, CancellationToken cancellationToken)
    {
        var ok = await _services.AdminService.DeleteCollectionPointAsync(collectionId, cancellationToken);
        if (!ok)
            return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy điểm tập kết"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa điểm tập kết thành công"));
    }

    [HttpGet("system-config/parameters")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AdminSystemConfigDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemConfigs(CancellationToken cancellationToken)
    {
        var data = await _services.AdminService.GetSystemConfigsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<AdminSystemConfigDto>>.SuccessResponse(data));
    }

    [HttpPut("system-config/parameters/{configKey}")]
    [ProducesResponseType(typeof(ApiResponse<AdminSystemConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminSystemConfigDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertSystemConfig([FromRoute] string configKey, [FromBody] UpsertSystemConfigRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configKey))
            return BadRequest(ApiResponse<AdminSystemConfigDto>.ErrorResponse("Config key không hợp lệ"));

        var data = await _services.AdminService.UpsertSystemConfigAsync(configKey, request, cancellationToken);
        return Ok(ApiResponse<AdminSystemConfigDto>.SuccessResponse(data, "Cập nhật cấu hình thành công"));
    }

    [HttpGet("catalog/units")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AdminUnitDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnits(CancellationToken cancellationToken)
    {
        var data = await _services.AdminService.GetUnitsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<AdminUnitDto>>.SuccessResponse(data));
    }

    [HttpPost("catalog/units")]
    [ProducesResponseType(typeof(ApiResponse<AdminUnitDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUnit([FromBody] UpsertUnitRequestDto request, CancellationToken cancellationToken)
    {
        var data = await _services.AdminService.CreateUnitAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUnits), ApiResponse<AdminUnitDto>.SuccessResponse(data, "Tạo đơn vị tính thành công"));
    }

    [HttpPut("catalog/units/{unitId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminUnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminUnitDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUnit(Guid unitId, [FromBody] UpsertUnitRequestDto request, CancellationToken cancellationToken)
    {
        var data = await _services.AdminService.UpdateUnitAsync(unitId, request, cancellationToken);
        if (data == null)
            return NotFound(ApiResponse<AdminUnitDto>.ErrorResponse("Không tìm thấy đơn vị tính"));

        return Ok(ApiResponse<AdminUnitDto>.SuccessResponse(data, "Cập nhật đơn vị tính thành công"));
    }

    [HttpDelete("catalog/units/{unitId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUnit(Guid unitId, CancellationToken cancellationToken)
    {
        var ok = await _services.AdminService.DeleteUnitAsync(unitId, cancellationToken);
        if (!ok)
            return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy đơn vị tính"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa đơn vị tính thành công"));
    }

    [HttpGet("catalog/promotions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AdminPromotionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromotions(CancellationToken cancellationToken)
    {
        var data = await _services.PromotionService.GetPromotionsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<AdminPromotionDto>>.SuccessResponse(data));
    }

    [HttpPost("catalog/promotions")]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _services.PromotionService.CreatePromotionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetPromotions), ApiResponse<AdminPromotionDto>.SuccessResponse(data, "Tạo khuyến mãi thành công"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminPromotionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("catalog/promotions/{promotionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePromotion(Guid promotionId, [FromBody] UpdatePromotionRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _services.PromotionService.UpdatePromotionAsync(promotionId, request, cancellationToken);
            if (data == null)
                return NotFound(ApiResponse<AdminPromotionDto>.ErrorResponse("Không tìm thấy khuyến mãi"));

            return Ok(ApiResponse<AdminPromotionDto>.SuccessResponse(data, "Cập nhật khuyến mãi thành công"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminPromotionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPatch("catalog/promotions/{promotionId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AdminPromotionDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePromotionStatus(Guid promotionId, [FromBody] UpdatePromotionStatusRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _services.PromotionService.UpdatePromotionStatusAsync(promotionId, request.Status, cancellationToken);
            if (data == null)
                return NotFound(ApiResponse<AdminPromotionDto>.ErrorResponse("Không tìm thấy khuyến mãi"));

            return Ok(ApiResponse<AdminPromotionDto>.SuccessResponse(data, "Cập nhật trạng thái khuyến mãi thành công"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<AdminPromotionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("monitoring/ai-pricing-history")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AdminAiPriceHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAiPricingHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var data = await _services.AdminService.GetAiPriceHistoriesAsync(pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<AdminAiPriceHistoryDto>>.SuccessResponse(data));
    }

    [HttpGet("supermarkets/applications/pending")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminPendingSupermarketApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingSupermarketApplications(CancellationToken cancellationToken)
    {
        var result = await _services.SupermarketRegistrationService.GetPendingApplicationsForAdminAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("supermarkets/applications/{supermarketId:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveSupermarketApplication(Guid supermarketId, CancellationToken cancellationToken)
    {
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdStr, out var adminUserId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định quản trị viên"));

        var result = await _services.SupermarketRegistrationService.ApproveApplicationAsync(supermarketId, adminUserId, cancellationToken);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("supermarkets/applications/{supermarketId:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectSupermarketApplication(
        Guid supermarketId,
        [FromBody] RejectSupermarketApplicationRequestDto? request,
        CancellationToken cancellationToken)
    {
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdStr, out var adminUserId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định quản trị viên"));

        var result = await _services.SupermarketRegistrationService.RejectApplicationAsync(
            supermarketId, adminUserId, request?.AdminReviewNote, cancellationToken);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
