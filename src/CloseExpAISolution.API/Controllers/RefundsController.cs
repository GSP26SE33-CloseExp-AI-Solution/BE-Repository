using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Enums;
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RefundResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RefundResponseDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var refund = await _services.RefundService.GetByIdAsync(id, cancellationToken);
        if (refund == null)
            return NotFound(ApiResponse<RefundResponseDto>.ErrorResponse("Refund not found"));
        return Ok(ApiResponse<RefundResponseDto>.SuccessResponse(refund));
    }

    [HttpPost]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetPending(Guid id, CancellationToken cancellationToken = default) =>
        UpdateRefundStatus(id, RefundState.Pending, cancellationToken);

    [HttpPut("{id:guid}/approved")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetApproved(Guid id, CancellationToken cancellationToken = default) =>
        UpdateRefundStatus(id, RefundState.Approved, cancellationToken);

    [HttpPut("{id:guid}/rejected")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetRejected(Guid id, CancellationToken cancellationToken = default) =>
        UpdateRefundStatus(id, RefundState.Rejected, cancellationToken);

    [HttpPut("{id:guid}/completed")]
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
}
