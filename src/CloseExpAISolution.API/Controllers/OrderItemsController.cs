using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderItemsController : ControllerBase
{
    private readonly IServiceProviders _services;
    private readonly ILogger<OrderItemsController> _logger;

    public OrderItemsController(IServiceProviders services, ILogger<OrderItemsController> logger)
    {
        _services = services;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderItemResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderItemResponseDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? orderId = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _services.OrderItemService.GetAllAsync(pageNumber, pageSize, orderId, cancellationToken);
        var result = new PaginatedResult<OrderItemResponseDto>
        {
            Items = items,
            TotalResult = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<OrderItemResponseDto>>.SuccessResponse(result));
    }

    [HttpGet("by-order/{orderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrderItemResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrderItemResponseDto>>>> GetByOrderId(Guid orderId, CancellationToken cancellationToken = default)
    {
        var items = await _services.OrderItemService.GetByOrderIdAsync(orderId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<OrderItemResponseDto>>.SuccessResponse(items));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderItemResponseDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _services.OrderItemService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound(ApiResponse<OrderItemResponseDto>.ErrorResponse("Order item not found"));
        return Ok(ApiResponse<OrderItemResponseDto>.SuccessResponse(item));
    }

    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(ApiResponse<OrderItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderItemResponseDto>>> GetByIdWithDetails(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _services.OrderItemService.GetByIdWithDetailsAsync(id, cancellationToken);
        if (item == null)
            return NotFound(ApiResponse<OrderItemResponseDto>.ErrorResponse("Order item not found"));
        return Ok(ApiResponse<OrderItemResponseDto>.SuccessResponse(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderItemResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<OrderItemResponseDto>>> Create(
        [FromBody] CreateOrderItemRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _services.OrderItemService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.OrderItemId }, ApiResponse<OrderItemResponseDto>.SuccessResponse(created, "Order item created"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Create order item failed");
            return BadRequest(ApiResponse<OrderItemResponseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateOrderItemRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _services.OrderItemService.UpdateAsync(id, request, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Order item not found"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update order item {OrderItemId} failed", id);
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
            await _services.OrderItemService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Order item not found"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Delete order item {OrderItemId} failed", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }
}
