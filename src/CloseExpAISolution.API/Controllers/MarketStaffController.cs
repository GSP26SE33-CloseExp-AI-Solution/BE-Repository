using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketStaffController : ControllerBase
{
    private readonly IServiceProviders _services;

    public MarketStaffController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<MarketStaffResponseDto>>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var items = (await _services.MarketStaffService.GetAllWithDtoAsync()).ToList();
        var total = items.Count;
        var pageItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var result = new PaginatedResult<MarketStaffResponseDto>
        {
            Items = pageItems,
            TotalResult = total,
            Rage = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<MarketStaffResponseDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MarketStaffResponseDto>>> GetById(Guid id)
    {
        var item = await _services.MarketStaffService.GetByIdWithDtoAsync(id);
        if (item == null) return NotFound(ApiResponse<MarketStaffResponseDto>.ErrorResponse("MarketStaff not found"));
        return Ok(ApiResponse<MarketStaffResponseDto>.SuccessResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MarketStaffResponseDto>>> Create([FromBody] CreateMarketStaffRequestDto request, CancellationToken cancellationToken)
    {
        var created = await _services.MarketStaffService.CreateMarketStaffAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.MarketStaffId }, ApiResponse<MarketStaffResponseDto>.SuccessResponse(created, "Created"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateMarketStaffRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _services.MarketStaffService.UpdateMarketStaffAsync(id, request, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null!, "Updated"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("MarketStaff not found"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _services.MarketStaffService.DeleteMarketStaffAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null!, "Deleted"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("MarketStaff not found"));
        }
    }
}
