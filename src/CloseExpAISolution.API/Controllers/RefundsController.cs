using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RefundsController : ControllerBase
{
    private readonly IServiceProviders _services;
    private readonly ILogger<RefundsController> _logger;

    public RefundsController(IServiceProviders services, ILogger<RefundsController> logger)
    {
        _services = services;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,MarketingStaff")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<RefundResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<RefundResponseDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (items, total) = await _services.RefundService.GetAllAsync(pageNumber, pageSize, cancellationToken);
        var result = new PaginatedResult<RefundResponseDto>
        {
            Items = items,
            TotalResult = total,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<RefundResponseDto>>.SuccessResponse(result));
    }

    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<RefundResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<RefundResponseDto>>>> GetMyRefunds(
        [FromQuery] Guid? orderId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (items, total) = await _services.RefundService.GetByUserAsync(userId, orderId, pageNumber, pageSize, cancellationToken);
        var result = new PaginatedResult<RefundResponseDto>
        {
            Items = items,
            TotalResult = total,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<RefundResponseDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<RefundResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RefundResponseDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        RefundResponseDto? refund;
        if (User.IsInRole("Admin") || User.IsInRole("MarketingStaff"))
        {
            refund = await _services.RefundService.GetByIdAsync(id, cancellationToken);
        }
        else
        {
            if (!TryGetCurrentUserId(out var userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));
            refund = await _services.RefundService.GetByIdForUserAsync(id, userId, cancellationToken);
        }

        if (refund == null)
            return NotFound(ApiResponse<RefundResponseDto>.ErrorResponse("Refund not found"));
        return Ok(ApiResponse<RefundResponseDto>.SuccessResponse(refund));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,MarketingStaff")]
    [ProducesResponseType(typeof(ApiResponse<RefundResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RefundResponseDto>>> Create(
        [FromBody] CreateRefundRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _services.RefundService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.RefundId },
                ApiResponse<RefundResponseDto>.SuccessResponse(created, "Refund request created"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<RefundResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Create refund failed");
            return BadRequest(ApiResponse<RefundResponseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id:guid}/pending")]
    [Authorize(Roles = "Admin,MarketingStaff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetPending(Guid id, CancellationToken cancellationToken = default) =>
        UpdateRefundStatus(id, RefundState.Pending, cancellationToken);

    [HttpPut("{id:guid}/approved")]
    [Authorize(Roles = "Admin,MarketingStaff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetApproved(Guid id, CancellationToken cancellationToken = default) =>
        UpdateRefundStatus(id, RefundState.Approved, cancellationToken);

    [HttpPut("{id:guid}/rejected")]
    [Authorize(Roles = "Admin,MarketingStaff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetRejected(Guid id, CancellationToken cancellationToken = default) =>
        UpdateRefundStatus(id, RefundState.Rejected, cancellationToken);

    [HttpPut("{id:guid}/completed")]
    [Authorize(Roles = "Admin,MarketingStaff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetCompleted(Guid id, CancellationToken cancellationToken = default) =>
        UpdateRefundStatus(id, RefundState.Completed, cancellationToken);

    private async Task<ActionResult<ApiResponse<object>>> UpdateRefundStatus(
        Guid id,
        RefundState status,
        CancellationToken cancellationToken)
    {
        var processedBy = ResolveProcessedBy();
        try
        {
            await _services.RefundService.UpdateStatusAsync(id, status, processedBy, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Refund not found"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update refund {RefundId} to {Status} failed", id, status);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    private string? ResolveProcessedBy() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.Identity?.Name;

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdString, out userId);
    }
}
