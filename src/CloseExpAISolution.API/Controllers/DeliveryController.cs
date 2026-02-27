using System.Security.Claims;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "DeliveryStaff")]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;

    public DeliveryController(IDeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Không thể xác định người dùng hiện tại.");

        return userId;
    }

    /// <summary>
    /// Lấy danh sách nhóm giao hàng khả dụng (chưa có shipper nhận)
    /// </summary>
    /// <param name="deliveryDate">Lọc theo ngày giao (yyyy-MM-dd)</param>
    [HttpGet("groups/available")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DeliveryGroupSummaryDto>>>> GetAvailableGroups(
        [FromQuery] DateTime? deliveryDate = null)
    {
        try
        {
            var groups = await _deliveryService.GetAvailableDeliveryGroupsAsync(deliveryDate);
            return Ok(ApiResponse<IEnumerable<DeliveryGroupSummaryDto>>.SuccessResponse(groups));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<DeliveryGroupSummaryDto>>.ErrorResponse(
                $"Lỗi khi lấy danh sách nhóm giao hàng: {ex.Message}"));
        }
    }

    /// <summary>
    /// Lấy danh sách nhóm giao hàng của tôi (đã nhận)
    /// </summary>
    /// <param name="status">Lọc theo trạng thái: Assigned, InTransit, Completed</param>
    /// <param name="deliveryDate">Lọc theo ngày giao (yyyy-MM-dd)</param>
    /// <param name="pageNumber">Số trang (mặc định: 1)</param>
    /// <param name="pageSize">Kích thước trang (mặc định: 20)</param>
    [HttpGet("groups/my")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>>> GetMyGroups(
        [FromQuery] string? status = null,
        [FromQuery] DateTime? deliveryDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var staffId = GetCurrentUserId();

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _deliveryService.GetMyDeliveryGroupsAsync(
                staffId, status, deliveryDate, pageNumber, pageSize);

            var result = new PaginatedResult<DeliveryGroupSummaryDto>
            {
                Items = items,
                TotalResult = totalCount,
                Page = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>.ErrorResponse(
                $"Lỗi khi lấy danh sách nhóm giao hàng: {ex.Message}"));
        }
    }

    /// <summary>
    /// Xem chi tiết nhóm giao hàng (bao gồm danh sách đơn hàng)
    /// </summary>
    [HttpGet("groups/{deliveryGroupId:guid}")]
    public async Task<ActionResult<ApiResponse<DeliveryGroupResponseDto>>> GetGroupDetail(Guid deliveryGroupId)
    {
        try
        {
            var group = await _deliveryService.GetDeliveryGroupDetailAsync(deliveryGroupId);
            if (group == null)
                return NotFound(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse("Không tìm thấy nhóm giao hàng."));

            return Ok(ApiResponse<DeliveryGroupResponseDto>.SuccessResponse(group));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                $"Lỗi khi lấy chi tiết nhóm giao hàng: {ex.Message}"));
        }
    }

    /// <summary>
    /// Nhận nhóm giao hàng (shipper nhận giao)
    /// </summary>
    [HttpPost("groups/{deliveryGroupId:guid}/accept")]
    public async Task<ActionResult<ApiResponse<DeliveryGroupResponseDto>>> AcceptGroup(
        Guid deliveryGroupId,
        [FromBody] AcceptDeliveryGroupRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var group = await _deliveryService.AcceptDeliveryGroupAsync(
                deliveryGroupId, staffId, request);

            return Ok(ApiResponse<DeliveryGroupResponseDto>.SuccessResponse(group, "Nhận nhóm giao hàng thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                $"Lỗi khi nhận nhóm giao hàng: {ex.Message}"));
        }
    }

    /// <summary>
    /// Bắt đầu giao hàng (chuyển nhóm sang trạng thái InTransit)
    /// </summary>
    [HttpPost("groups/{deliveryGroupId:guid}/start")]
    public async Task<ActionResult<ApiResponse<DeliveryGroupResponseDto>>> StartDelivery(
        Guid deliveryGroupId,
        [FromBody] StartDeliveryRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var group = await _deliveryService.StartDeliveryAsync(
                deliveryGroupId, staffId, request);

            return Ok(ApiResponse<DeliveryGroupResponseDto>.SuccessResponse(group, "Bắt đầu giao hàng thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                $"Lỗi khi bắt đầu giao hàng: {ex.Message}"));
        }
    }

    /// <summary>
    /// Hoàn thành nhóm giao hàng (tất cả đơn đã xử lý)
    /// </summary>
    [HttpPost("groups/{deliveryGroupId:guid}/complete")]
    public async Task<ActionResult<ApiResponse<DeliveryGroupResponseDto>>> CompleteGroup(Guid deliveryGroupId)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var group = await _deliveryService.CompleteDeliveryGroupAsync(
                deliveryGroupId, staffId);

            return Ok(ApiResponse<DeliveryGroupResponseDto>.SuccessResponse(group, "Hoàn thành nhóm giao hàng thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                $"Lỗi khi hoàn thành nhóm giao hàng: {ex.Message}"));
        }
    }

    /// <summary>
    /// Xem chi tiết đơn hàng cần giao
    /// </summary>
    [HttpGet("orders/{orderId:guid}")]
    public async Task<ActionResult<ApiResponse<DeliveryOrderResponseDto>>> GetOrderDetail(Guid orderId)
    {
        try
        {
            var order = await _deliveryService.GetOrderDetailForDeliveryAsync(orderId);
            if (order == null)
                return NotFound(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse("Không tìm thấy đơn hàng."));

            return Ok(ApiResponse<DeliveryOrderResponseDto>.SuccessResponse(order));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
                $"Lỗi khi lấy chi tiết đơn hàng: {ex.Message}"));
        }
    }

    /// <summary>
    /// Xác nhận đã giao hàng thành công (chuyển sang chờ khách xác nhận)
    /// </summary>
    [HttpPost("orders/{orderId:guid}/confirm-delivery")]
    public async Task<ActionResult<ApiResponse<DeliveryOrderResponseDto>>> ConfirmDelivery(
        Guid orderId,
        [FromBody] ConfirmDeliveryRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var order = await _deliveryService.ConfirmDeliveryAsync(orderId, staffId, request);

            return Ok(ApiResponse<DeliveryOrderResponseDto>.SuccessResponse(order, "Xác nhận giao hàng thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
                $"Lỗi khi xác nhận giao hàng: {ex.Message}"));
        }
    }

    /// <summary>
    /// Báo cáo giao hàng thất bại
    /// </summary>
    [HttpPost("orders/{orderId:guid}/report-failure")]
    public async Task<ActionResult<ApiResponse<DeliveryOrderResponseDto>>> ReportFailure(
        Guid orderId,
        [FromBody] ReportDeliveryFailureRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var order = await _deliveryService.ReportDeliveryFailureAsync(orderId, staffId, request);

            return Ok(ApiResponse<DeliveryOrderResponseDto>.SuccessResponse(order, "Báo cáo giao hàng thất bại thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
                $"Lỗi khi báo cáo giao hàng thất bại: {ex.Message}"));
        }
    }

    /// <summary>
    /// Lấy lịch sử giao hàng của tôi
    /// </summary>
    /// <param name="fromDate">Từ ngày (yyyy-MM-dd)</param>
    /// <param name="toDate">Đến ngày (yyyy-MM-dd)</param>
    /// <param name="status">Lọc theo trạng thái</param>
    /// <param name="pageNumber">Số trang (mặc định: 1)</param>
    /// <param name="pageSize">Kích thước trang (mặc định: 20)</param>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<DeliveryRecordResponseDto>>>> GetDeliveryHistory(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var staffId = GetCurrentUserId();

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _deliveryService.GetDeliveryHistoryAsync(
                staffId, fromDate, toDate, status, pageNumber, pageSize);

            var result = new PaginatedResult<DeliveryRecordResponseDto>
            {
                Items = items,
                TotalResult = totalCount,
                Page = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PaginatedResult<DeliveryRecordResponseDto>>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PaginatedResult<DeliveryRecordResponseDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PaginatedResult<DeliveryRecordResponseDto>>.ErrorResponse(
                $"Lỗi khi lấy lịch sử giao hàng: {ex.Message}"));
        }
    }

    /// <summary>
    /// Lấy thống kê giao hàng của tôi
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DeliveryStatsResponseDto>>> GetMyStats()
    {
        try
        {
            var staffId = GetCurrentUserId();
            var stats = await _deliveryService.GetDeliveryStatsAsync(staffId);

            return Ok(ApiResponse<DeliveryStatsResponseDto>.SuccessResponse(stats));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<DeliveryStatsResponseDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<DeliveryStatsResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DeliveryStatsResponseDto>.ErrorResponse(
                $"Lỗi khi lấy thống kê giao hàng: {ex.Message}"));
        }
    }
}
