using System.Security.Claims;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IServiceProviders _services;

    public NotificationsController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _services.NotificationService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<NotificationResponseDto>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.NotificationService.GetByUserIdAsync(userId.Value);
        return Ok(result);
    }

    /// <summary>Chronological thread (đặt hàng + mọi cập nhật) cho một đơn.</summary>
    [HttpGet("me/order/{orderId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMineForOrder(Guid orderId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<NotificationResponseDto>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.NotificationService.GetMyOrderNotificationsAsync(userId.Value, orderId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("user/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var result = await _services.NotificationService.GetByUserIdAsync(userId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<NotificationResponseDto>.ErrorResponse("Không thể xác định người dùng"));

        var isAdmin = User.IsInRole("Admin");
        var result = await _services.NotificationService.GetByIdAsync(id, userId.Value, isAdmin);
        if (!result.Success)
        {
            if (result.Message == "Không tìm thấy thông báo")
                return NotFound(result);
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequestDto request)
    {
        var result = await _services.NotificationService.CreateAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data?.NotificationId }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNotificationRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<NotificationResponseDto>.ErrorResponse("Không thể xác định người dùng"));

        var isAdmin = User.IsInRole("Admin");
        var result = await _services.NotificationService.UpdateAsync(id, userId.Value, isAdmin, request);
        if (!result.Success)
        {
            if (result.Message == "Không tìm thấy thông báo")
                return NotFound(result);
            if (result.Message == "Bạn không có quyền sửa thông báo này" || result.Message == "Bạn chỉ có thể đánh dấu đã đọc")
                return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Không thể xác định người dùng"));

        var isAdmin = User.IsInRole("Admin");
        var result = await _services.NotificationService.DeleteAsync(id, userId.Value, isAdmin);
        if (!result.Success)
        {
            if (result.Message == "Không tìm thấy thông báo")
                return NotFound(result);
            if (result.Message == "Bạn không có quyền xóa thông báo này")
                return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }
}
