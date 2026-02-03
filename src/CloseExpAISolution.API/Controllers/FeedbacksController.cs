using System.Security.Claims;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbacksController : ControllerBase
{
    private readonly IServiceProviders _services;

    public FeedbacksController(IServiceProviders services)
    {
        _services = services;
    }

    #region User Endpoints

    /// <summary>
    /// Get current user's feedbacks
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FeedbackResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFeedbacks()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<FeedbackResponseDto>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.FeedbackService.GetFeedbacksByUserIdAsync(userId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Create a new feedback (Authenticated user)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<FeedbackResponseDto>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.FeedbackService.CreateFeedbackAsync(userId.Value, request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetFeedbackById), new { id = result.Data?.FeedbackId }, result);
    }

    /// <summary>
    /// Update feedback (Owner only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFeedback(Guid id, [FromBody] UpdateFeedbackRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<FeedbackResponseDto>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.FeedbackService.UpdateFeedbackAsync(id, userId.Value, request);
        if (!result.Success)
        {
            if (result.Message == "Không tìm thấy đánh giá")
                return NotFound(result);
            if (result.Message == "Bạn không có quyền sửa đánh giá này")
                return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete feedback (Owner or Admin)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFeedback(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Không thể xác định người dùng"));

        var isAdmin = User.IsInRole("Admin");
        var result = await _services.FeedbackService.DeleteFeedbackAsync(id, userId.Value, isAdmin);

        if (!result.Success)
        {
            if (result.Message == "Không tìm thấy đánh giá")
                return NotFound(result);
            if (result.Message == "Bạn không có quyền xóa đánh giá này")
                return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    #endregion

    #region Public/Admin Endpoints

    /// <summary>
    /// Get feedback by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeedbackById(Guid id)
    {
        var result = await _services.FeedbackService.GetFeedbackByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get all feedbacks for an order
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FeedbackResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeedbacksByOrder(Guid orderId)
    {
        var result = await _services.FeedbackService.GetFeedbacksByOrderIdAsync(orderId);
        return Ok(result);
    }

    /// <summary>
    /// Get all feedbacks (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FeedbackResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFeedbacks()
    {
        var result = await _services.FeedbackService.GetAllFeedbacksAsync();
        return Ok(result);
    }

    #endregion

    #region Helpers

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }

    #endregion
}
