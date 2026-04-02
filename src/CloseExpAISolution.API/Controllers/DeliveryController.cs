using System.Security.Claims;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly IServiceProviders _services;

    public DeliveryController(
        IServiceProviders services)
    {
        _services = services;
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            return false;

        return Guid.TryParse(userIdClaim, out userId);
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

            var (items, totalCount) = await _services.DeliveryAdminService.GetPendingDeliveryGroupsAsync(
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
    [HttpGet("groups/drafts")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>>> GetDraftGroups(
        [FromQuery] DraftDeliveryGroupQueryDto query)
    {
        try
        {
            var (items, totalCount) = await _services.DeliveryAdminService.GetDraftDeliveryGroupsAsync(query);
            var (pageNumber, pageSize) = NormalizePaging(query.PageNumber, query.PageSize);
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
                "Lỗi khi lấy danh sách draft delivery groups."));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("groups/drafts/generate")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeliveryGroupSummaryDto>>>> GenerateDraftGroups(
        [FromBody] GenerateDeliveryGroupDraftRequestDto request)
    {
        try
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return Unauthorized(ApiResponse<IReadOnlyList<DeliveryGroupSummaryDto>>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var result = await _services.DeliveryAdminService.GenerateDraftGroupsAsync(request, adminId);
            return Ok(ApiResponse<IReadOnlyList<DeliveryGroupSummaryDto>>.SuccessResponse(result, "Tạo draft delivery groups thành công."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<IReadOnlyList<DeliveryGroupSummaryDto>>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<IReadOnlyList<DeliveryGroupSummaryDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<IReadOnlyList<DeliveryGroupSummaryDto>>.ErrorResponse(
                "Lỗi khi tạo draft delivery groups."));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("groups/{deliveryGroupId:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<DeliveryGroupResponseDto>>> ConfirmDraftGroup(Guid deliveryGroupId)
    {
        try
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return Unauthorized(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var group = await _services.DeliveryAdminService.ConfirmDraftGroupAsync(deliveryGroupId, adminId);
            return Ok(ApiResponse<DeliveryGroupResponseDto>.SuccessResponse(group, "Xác nhận draft group thành công."));
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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                "Lỗi khi xác nhận draft group."));
        }
    }

    /// <summary>
    /// Chỉnh đơn giữa các nhóm Draft hoặc gỡ đơn khỏi nhóm Draft (body deliveryGroupId null).
    /// Luồng gợi ý: generate draft → (GET drafts) → chỉnh tay đơn nếu cần → confirm → assign staff.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("orders/{orderId:guid}/draft-group")]
    public async Task<ActionResult<ApiResponse<MoveOrderToDraftGroupResultDto>>> MoveOrderToDraftGroup(
        Guid orderId,
        [FromBody] MoveOrderToDraftGroupRequestDto request)
    {
        try
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return Unauthorized(ApiResponse<MoveOrderToDraftGroupResultDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var result = await _services.DeliveryAdminService.MoveOrderToDraftGroupAsync(
                orderId,
                request,
                adminId);
            return Ok(ApiResponse<MoveOrderToDraftGroupResultDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<MoveOrderToDraftGroupResultDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<MoveOrderToDraftGroupResultDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MoveOrderToDraftGroupResultDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<MoveOrderToDraftGroupResultDto>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<MoveOrderToDraftGroupResultDto>.ErrorResponse(
                "Lỗi khi cập nhật nhóm Draft cho đơn hàng."));
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

            if (!TryGetCurrentUserId(out var adminId))
            {
                return Unauthorized(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var group = await _services.DeliveryAdminService.AssignGroupToStaffAsync(
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
            var groups = await _services.DeliveryService.GetAvailableDeliveryGroupsAsync(deliveryDate);
            return Ok(ApiResponse<IEnumerable<DeliveryGroupSummaryDto>>.SuccessResponse(groups));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<IEnumerable<DeliveryGroupSummaryDto>>.ErrorResponse(
                "Lỗi khi lấy danh sách nhóm giao hàng."));
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
            if (!TryGetCurrentUserId(out var staffId))
            {
                return Unauthorized(ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

            var (items, totalCount) = await _services.DeliveryService.GetMyDeliveryGroupsAsync(
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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<PaginatedResult<DeliveryGroupSummaryDto>>.ErrorResponse(
                "Lỗi khi lấy danh sách nhóm giao hàng."));
        }
    }

    [Authorize(Roles = "DeliveryStaff")]
    [HttpGet("groups/{deliveryGroupId:guid}")]
    public async Task<ActionResult<ApiResponse<DeliveryGroupResponseDto>>> GetGroupDetail(Guid deliveryGroupId)
    {
        try
        {
            var group = await _services.DeliveryService.GetDeliveryGroupDetailAsync(deliveryGroupId);
            if (group == null)
                return NotFound(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse("Không tìm thấy nhóm giao hàng."));

            return Ok(ApiResponse<DeliveryGroupResponseDto>.SuccessResponse(group));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                "Lỗi khi lấy chi tiết nhóm giao hàng."));
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
            if (!TryGetCurrentUserId(out var staffId))
            {
                return Unauthorized(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var group = await _services.DeliveryService.AcceptDeliveryGroupAsync(
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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
            "Lỗi khi nhận nhóm giao hàng."));
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
            if (!TryGetCurrentUserId(out var staffId))
            {
                return Unauthorized(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var group = await _services.DeliveryService.StartDeliveryAsync(
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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
            "Lỗi khi bắt đầu giao hàng."));
        }
    }

    [Authorize(Roles = "DeliveryStaff")]
    [HttpPost("groups/{deliveryGroupId:guid}/complete")]
    public async Task<ActionResult<ApiResponse<DeliveryGroupResponseDto>>> CompleteGroup(Guid deliveryGroupId)
    {
        try
        {
            if (!TryGetCurrentUserId(out var staffId))
            {
                return Unauthorized(ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var group = await _services.DeliveryService.CompleteDeliveryGroupAsync(
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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryGroupResponseDto>.ErrorResponse(
            "Lỗi khi hoàn thành nhóm giao hàng."));
        }
    }

    [Authorize(Roles = "DeliveryStaff")]
    [HttpGet("orders/{orderId:guid}")]
    public async Task<ActionResult<ApiResponse<DeliveryOrderResponseDto>>> GetOrderDetail(Guid orderId)
    {
        try
        {
            if (!TryGetCurrentUserId(out var staffId))
            {
                return Unauthorized(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var order = await _services.DeliveryService.GetOrderDetailForDeliveryAsync(orderId, staffId);
            if (order == null)
                return NotFound(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse("Không tìm thấy đơn hàng."));

            return Ok(ApiResponse<DeliveryOrderResponseDto>.SuccessResponse(order));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
                "Lỗi khi lấy chi tiết đơn hàng."));
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
            if (!TryGetCurrentUserId(out var staffId))
            {
                return Unauthorized(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var order = await _services.DeliveryService.ConfirmDeliveryAsync(
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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
            "Lỗi khi xác nhận giao hàng."));
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
            if (!TryGetCurrentUserId(out var staffId))
            {
                return Unauthorized(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var order = await _services.DeliveryService.ReportDeliveryFailureAsync(orderId, staffId, request);

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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
            "Lỗi khi báo cáo giao hàng thất bại."));
        }
    }

    [Authorize(Roles = "Vendor")]
    [HttpPost("orders/{orderId:guid}/customer-confirmation")]
    public async Task<ActionResult<ApiResponse<DeliveryOrderResponseDto>>> ConfirmOrderReceiptByCustomer(
        Guid orderId,
        [FromBody] ConfirmOrderReceiptRequestDto? request)
    {
        try
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Unauthorized(ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var order = await _services.DeliveryService.ConfirmOrderReceiptByCustomerAsync(
                orderId,
                customerId,
                request ?? new ConfirmOrderReceiptRequestDto());

            return Ok(ApiResponse<DeliveryOrderResponseDto>.SuccessResponse(
                order,
                "Xác nhận đã nhận hàng thành công."));
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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryOrderResponseDto>.ErrorResponse(
                "Lỗi khi xác nhận nhận hàng."));
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
            if (!TryGetCurrentUserId(out var staffId))
            {
                return Unauthorized(ApiResponse<PaginatedResult<DeliveryRecordResponseDto>>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

            var (items, totalCount) = await _services.DeliveryService.GetDeliveryHistoryAsync(
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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<PaginatedResult<DeliveryRecordResponseDto>>.ErrorResponse(
                "Lỗi khi lấy lịch sử giao hàng."));
        }
    }

    [Authorize(Roles = "DeliveryStaff")]
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DeliveryStatsResponseDto>>> GetMyStats()
    {
        try
        {
            if (!TryGetCurrentUserId(out var staffId))
            {
                return Unauthorized(ApiResponse<DeliveryStatsResponseDto>.ErrorResponse(
                    "Không thể xác định người dùng"));
            }

            var stats = await _services.DeliveryService.GetDeliveryStatsAsync(staffId);

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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DeliveryStatsResponseDto>.ErrorResponse(
                "Lỗi khi lấy thống kê giao hàng."));
        }
    }
}

