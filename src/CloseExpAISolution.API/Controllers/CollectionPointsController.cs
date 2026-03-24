using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollectionPointsController : ControllerBase
{
    private readonly IServiceProviders _services;

    public CollectionPointsController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CollectionPointResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<CollectionPointResponseDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var items = (await _services.CollectionPointService.GetAllAsync(cancellationToken)).ToList();
        var total = items.Count;
        var pageItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var result = new PaginatedResult<CollectionPointResponseDto>
        {
            Items = pageItems,
            TotalResult = total,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<CollectionPointResponseDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CollectionPointResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CollectionPointResponseDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _services.CollectionPointService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy điểm tập kết"));
        return Ok(ApiResponse<CollectionPointResponseDto>.SuccessResponse(item));
    }
}
