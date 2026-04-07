using System.Security.Claims;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "PackagingStaff")]
public class PackagingController : ControllerBase
{
    private readonly IPackagingService _packagingService;
    private readonly ILogger<PackagingController> _logger;

    public PackagingController(IPackagingService packagingService, ILogger<PackagingController> logger)
    {
        _packagingService = packagingService;
        _logger = logger;
    }

    private string InternalErrorMessage()
    {
        var traceId = HttpContext.TraceIdentifier;
        return $"Đã có lỗi nội bộ. Vui lòng thử lại sau. TraceId: {traceId}";
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Không thể xác định người dùng hiện tại.");

        return userId;
    }

    [HttpGet("orders/pending")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<PackagingOrderSummaryDto>>>> GetPendingOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            var staffId = GetCurrentUserId();
            var (items, totalCount) = await _packagingService.GetPendingOrdersAsync(
                staffId,
                pageNumber,
                pageSize,
                cancellationToken);

            var result = new PaginatedResult<PackagingOrderSummaryDto>
            {
                Items = items,
                TotalResult = totalCount,
                Page = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PaginatedResult<PackagingOrderSummaryDto>>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PaginatedResult<PackagingOrderSummaryDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending packaging orders");
            return StatusCode(500, ApiResponse<PaginatedResult<PackagingOrderSummaryDto>>.ErrorResponse(InternalErrorMessage()));
        }
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<ActionResult<ApiResponse<PackagingOrderDetailDto>>> GetOrderDetail(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await _packagingService.GetOrderDetailAsync(orderId, cancellationToken);
            if (order == null)
                return NotFound(ApiResponse<PackagingOrderDetailDto>.ErrorResponse("Không tìm thấy đơn hàng."));

            return Ok(ApiResponse<PackagingOrderDetailDto>.SuccessResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get packaging order detail for {OrderId}", orderId);
            return StatusCode(500, ApiResponse<PackagingOrderDetailDto>.ErrorResponse(InternalErrorMessage()));
        }
    }

    [HttpPost("orders/{orderId:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<PackagingOrderDetailDto>>> ConfirmOrder(
        Guid orderId,
        [FromBody] ConfirmPackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var order = await _packagingService.ConfirmOrderAsync(orderId, staffId, request, cancellationToken);

            return Ok(ApiResponse<PackagingOrderDetailDto>.SuccessResponse(order, "Xác nhận đơn đóng gói thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm packaging order {OrderId}", orderId);
            return StatusCode(500, ApiResponse<PackagingOrderDetailDto>.ErrorResponse(InternalErrorMessage()));
        }
    }

    [HttpPost("orders/{orderId:guid}/collect")]
    public async Task<ActionResult<ApiResponse<PackagingOrderDetailDto>>> MarkCollected(
        Guid orderId,
        [FromBody] CollectPackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var order = await _packagingService.MarkCollectedAsync(orderId, staffId, request, cancellationToken);

            return Ok(ApiResponse<PackagingOrderDetailDto>.SuccessResponse(order, "Đã cập nhật bước thu gom sản phẩm."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect packaging order {OrderId}", orderId);
            return StatusCode(500, ApiResponse<PackagingOrderDetailDto>.ErrorResponse(InternalErrorMessage()));
        }
    }

    [HttpPost("orders/{orderId:guid}/package")]
    public async Task<ActionResult<ApiResponse<PackagingOrderDetailDto>>> CompletePackaging(
        Guid orderId,
        [FromBody] CompletePackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var order = await _packagingService.CompletePackagingAsync(orderId, staffId, request, cancellationToken);

            return Ok(ApiResponse<PackagingOrderDetailDto>.SuccessResponse(
                order,
                "Đã hoàn tất đóng gói cho các dòng được chọn; trạng thái đơn cập nhật khi tất cả dòng đã xử lý xong."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete packaging order {OrderId}", orderId);
            return StatusCode(500, ApiResponse<PackagingOrderDetailDto>.ErrorResponse(InternalErrorMessage()));
        }
    }

    [HttpPost("orders/{orderId:guid}/fail")]
    public async Task<ActionResult<ApiResponse<PackagingOrderDetailDto>>> FailPackaging(
        Guid orderId,
        [FromBody] FailPackagingOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var order = await _packagingService.FailPackagingAsync(orderId, staffId, request, cancellationToken);

            return Ok(ApiResponse<PackagingOrderDetailDto>.SuccessResponse(
                order,
                "Đã ghi nhận đóng gói thất bại cho các dòng được chọn; trạng thái đơn và hoàn tiền được cập nhật theo tổng hợp."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PackagingOrderDetailDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fail packaging order {OrderId}", orderId);
            return StatusCode(500, ApiResponse<PackagingOrderDetailDto>.ErrorResponse(InternalErrorMessage()));
        }
    }
}
