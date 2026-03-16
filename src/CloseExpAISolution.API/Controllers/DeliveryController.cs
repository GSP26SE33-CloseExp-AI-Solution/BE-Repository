using System.Security.Claims;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;
    private readonly IDeliveryAdminService _deliveryAdminService;

    public DeliveryController(
        IDeliveryService deliveryService,
        IDeliveryAdminService deliveryAdminService)
    {
        _deliveryService = deliveryService;
        _deliveryAdminService = deliveryAdminService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Không thể xác định người dùng hiện tại.");

        return userId;
    }

    private static (int PageNumber, int PageSize) NormalizePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        return (pageNumber, pageSize);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("groups")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>>> GetGroupsForAdmin(
        [FromQuery] PendingDeliveryGroupQueryDto query,
        [FromQuery] string status = "Pending")
    {
        try
        {
            if (!status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>.ErrorResponse(
                    "Hiện tại chỉ hỗ trợ truy vấn nhóm giao hàng ở trạng thái Pending."));
            }

            var (pageNumber, pageSize) = NormalizePaging(query.PageNumber, query.PageSize);

            var (items, totalCount) = await _deliveryAdminService.GetPendingDeliveryGroupsAsync(
                query.DeliveryDate,
                pageNumber,
                pageSize);

            var result = new PaginatedResult<DeliveryGroupSummaryDto>
            {
                Items = items,
                TotalResult = totalCount,
                Page = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>.SuccessResponse(result));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>.ErrorResponse(
                "Lỗi khi lấy danh sách nhóm giao hàng chờ điều phối."));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("groups/{deliveryGroupId:guid}/assignment")]
    [HttpPost("groups/{deliveryGroupId:guid}/assign")]
    public async Task<ActionResult<ApiResponse<DeliveryGroupResponseDto>>> AssignGroupToDeliveryStaff(
        Guid deliveryGroupId,
        [FromBody] AssignDeliveryGroupRequestDto request)
    {
        try
        {
            if (request.DeliveryStaffId == Guid.Empty)
            {
                return BadRequest(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                    "Mã nhân viên giao hàng không hợp lệ."));
            }

            var adminId = GetCurrentUserId();
            var group = await _deliveryAdminService.AssignGroupToStaffAsync(
                deliveryGroupId,
                request.DeliveryStaffId,
                adminId,
                request.Reason);

            return Ok(ApiResponse<DeliveryGroupResponseDto>.SuccessResponse(
                group,
                "Điều phối nhóm giao hàng thành công."));
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
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                "Lỗi khi điều phối nhóm giao hàng."));
        }
    }

    [Authorize(Roles = "DeliveryStaff")]
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

    [Authorize(Roles = "DeliveryStaff")]
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
            (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

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

    [Authorize(Roles = "DeliveryStaff")]
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

    [Authorize(Roles = "DeliveryStaff")]
    [HttpPost("groups/{deliveryGroupId:guid}/accept")]
    public async Task<ActionResult<ApiResponse<DeliveryGroupResponseDto>>> AcceptGroup(
        Guid deliveryGroupId,
        [FromBody] AcceptDeliveryGroupRequestDto? request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var group = await _deliveryService.AcceptDeliveryGroupAsync(
                deliveryGroupId, staffId, request ?? new AcceptDeliveryGroupRequestDto());

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

    [Authorize(Roles = "DeliveryStaff")]
    [HttpPost("groups/{deliveryGroupId:guid}/start")]
    public async Task<ActionResult<ApiResponse<DeliveryGroupResponseDto>>> StartDelivery(
        Guid deliveryGroupId,
        [FromBody] StartDeliveryRequestDto? request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var group = await _deliveryService.StartDeliveryAsync(
                deliveryGroupId, staffId, request ?? new StartDeliveryRequestDto());

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

    [Authorize(Roles = "DeliveryStaff")]
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

    [Authorize(Roles = "DeliveryStaff")]
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

    [Authorize(Roles = "DeliveryStaff")]
    [HttpPost("orders/{orderId:guid}/confirm-delivery")]
    public async Task<ActionResult<ApiResponse<DeliveryOrderResponseDto>>> ConfirmDelivery(
        Guid orderId,
        [FromBody] ConfirmDeliveryRequestDto? request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var order = await _deliveryService.ConfirmDeliveryAsync(
                orderId,
                staffId,
                request ?? new ConfirmDeliveryRequestDto());

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

    [Authorize(Roles = "DeliveryStaff")]
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

    [Authorize(Roles = "DeliveryStaff")]
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
            (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

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

    [Authorize(Roles = "DeliveryStaff")]
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

