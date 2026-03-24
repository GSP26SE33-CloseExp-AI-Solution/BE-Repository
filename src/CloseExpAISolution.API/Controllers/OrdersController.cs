using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Enums;
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
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PickupPointDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PickupPointDto>>>> GetCollectionPoints(CancellationToken cancellationToken = default)
    {
        var collectionPoints = await _services.OrderService.GetCollectionPointsAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<PickupPointDto>>.SuccessResponse(collectionPoints));
    }

    /// <summary>
    /// Get all orders with pagination
    /// </summary>
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

    /// <summary>
    /// Get order by ID
    /// </summary>
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

    /// <summary>
    /// Get order by ID with full details (user, time slot, pickup point, order items with product/lot info)
    /// </summary>
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

    /// <summary>
    /// Create a new order
    /// </summary>
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

    /// <summary>Set order status to Pending (one-click PUT, no body).</summary>
    [HttpPut("{id:guid}/pending")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetPending(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.Pending, cancellationToken);

    /// <summary>Set order status to PaidProcessing (one-click PUT, no body).</summary>
    [HttpPut("{id}/paid-processing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetPaidProcessing(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.PaidProcessing, cancellationToken);

    /// <summary>Set order status to ReadyToShip (one-click PUT, no body).</summary>
    [HttpPut("{id}/ready-to-ship")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetReadyToShip(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.ReadyToShip, cancellationToken);

    /// <summary>Set order status to DeliveredWaitConfirm (one-click PUT, no body).</summary>
    [HttpPut("{id}/delivered-wait-confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetDeliveredWaitConfirm(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.DeliveredWaitConfirm, cancellationToken);

    /// <summary>Set order status to Completed (one-click PUT, no body).</summary>
    [HttpPut("{id:guid}/completed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetCompleted(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.Completed, cancellationToken);

    /// <summary>Set order status to Canceled (one-click PUT, no body).</summary>
    [HttpPut("{id:guid}/canceled")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetCanceled(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.Canceled, cancellationToken);

    /// <summary>Set order status to Refunded (one-click PUT, no body).</summary>
    [HttpPut("{id:guid}/refunded")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<object>>> SetRefunded(Guid id, CancellationToken cancellationToken = default) => UpdateOrderStatus(id, OrderState.Refunded, cancellationToken);

    /// <summary>Set order status to Failed (one-click PUT, no body).</summary>
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

    /// <summary>
    /// Update an existing order
    /// </summary>
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

    /// <summary>
    /// Delete an order
    /// </summary>
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
}
