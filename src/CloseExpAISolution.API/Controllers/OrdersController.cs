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
public class OrdersController : ControllerBase
{
    private readonly IServiceProviders _services;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IServiceProviders services, ILogger<OrdersController> logger)
    {
        _services = services;
        _logger = logger;
    }

    [HttpGet("time-slots")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DeliveryTimeSlotDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<DeliveryTimeSlotDto>>>> GetTimeSlots(CancellationToken cancellationToken = default)
    {
        var timeSlots = await _services.OrderService.GetDeliveryTimeSlotsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<DeliveryTimeSlotDto>>.SuccessResponse(timeSlots));
    }

    [HttpGet("collection-points")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CollectionPointDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CollectionPointDto>>>> GetCollectionPoints(CancellationToken cancellationToken = default)
    {
        var collectionPoints = await _services.OrderService.GetCollectionPointsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<CollectionPointDto>>.SuccessResponse(collectionPoints));
    }

    /// <summary>Trả điểm tập kết trong bán kính (km); bán kính bị giới hạn bởi cấu hình PickupSearch.</summary>
    [HttpPost("collection-points/nearby")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PickupPointDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PickupPointDto>>>> GetCollectionPointsNearby(
        [FromBody] NearbyCollectionPointsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var items = await _services.OrderService.GetCollectionPointsNearbyAsync(request, cancellationToken);
        return Ok(ApiResponse<IEnumerable<PickupPointDto>>.SuccessResponse(items));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderResponseDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _services.OrderService.GetAllAsync(pageNumber, pageSize, cancellationToken);
        var result = new PaginatedResult<OrderResponseDto>
        {
            Items = items,
            TotalResult = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<OrderResponseDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _services.OrderService.GetByIdAsync(id, cancellationToken);
        if (order == null)
            return NotFound(ApiResponse<OrderResponseDto>.ErrorResponse("Order not found"));
        return Ok(ApiResponse<OrderResponseDto>.SuccessResponse(order));
    }

    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> GetByIdWithDetails(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _services.OrderService.GetByIdWithDetailsAsync(id, cancellationToken);
        if (order == null)
            return NotFound(ApiResponse<OrderResponseDto>.ErrorResponse("Order not found"));
        return Ok(ApiResponse<OrderResponseDto>.SuccessResponse(order));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> Create(
        [FromBody] CreateOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _services.OrderService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.OrderId }, ApiResponse<OrderResponseDto>.SuccessResponse(created, "Order created"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Create order failed");
            return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("my-orders")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderResponseDto>>>> GetMyOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _services.OrderService.GetByUserIdAsync(userId, pageNumber, pageSize, cancellationToken);
        var result = new PaginatedResult<OrderResponseDto>
        {
            Items = items,
            TotalResult = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<OrderResponseDto>>.SuccessResponse(result));
    }

    [HttpPost("my-orders")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<OrderResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> CreateMyOrder(
        [FromBody] CreateOwnOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token."));

        try
        {
            var created = await _services.OrderService.CreateForCustomerAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.OrderId }, ApiResponse<OrderResponseDto>.SuccessResponse(created, "Order created"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Create my-order failed");
            return BadRequest(ApiResponse<OrderResponseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id:guid}/pending")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetPending(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.Pending, cancellationToken);

    [HttpPut("{id}/paid-processing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetPaidProcessing(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.PaidProcessing, cancellationToken);

    [HttpPut("{id}/ready-to-ship")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetReadyToShip(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.ReadyToShip, cancellationToken);

    [HttpPut("{id}/delivered-wait-confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetDeliveredWaitConfirm(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.DeliveredWaitConfirm, cancellationToken);

    [HttpPut("{id:guid}/completed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetCompleted(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.Completed, cancellationToken);

    [HttpPut("{id:guid}/canceled")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetCanceled(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.Canceled, cancellationToken);

    [HttpPut("{id:guid}/refunded")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetRefunded(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.Refunded, cancellationToken);

    [HttpPut("{id:guid}/failed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetFailed(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.Failed, cancellationToken);

    private async Task<ActionResult<ApiResponse<object>>> UpdateOrderStatus(Guid id, OrderState status, CancellationToken cancellationToken)
    {
        try
        {
            await _services.OrderService.UpdateStatusAsync(id, status, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Order not found"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update order status {OrderId} to {Status} failed", id, status);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _services.OrderService.UpdateAsync(id, request, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Order not found"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update order {OrderId} failed", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _services.OrderService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Order not found"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Delete order {OrderId} failed", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdString, out userId);
    }
}
