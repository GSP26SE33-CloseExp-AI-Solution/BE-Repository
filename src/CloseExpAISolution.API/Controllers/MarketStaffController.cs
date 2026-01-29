using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Entities;
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
    public async Task<ActionResult<ApiResponse<PaginatedResult<MarketStaff>>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var items = (await _services.MarketStaffService.GetAllAsync()).ToList();
        var total = items.Count;
        var pageItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var result = new PaginatedResult<MarketStaff>
        {
            Items = pageItems,
            TotalResult = total,
            Rage = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<MarketStaff>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MarketStaff>>> GetById(Guid id)
    {
        var item = await _services.MarketStaffService.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (item == null) return NotFound(ApiResponse<MarketStaff>.ErrorResponse("MarketStaff not found"));
        return Ok(ApiResponse<MarketStaff>.SuccessResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MarketStaff>>> Create([FromBody] MarketStaff input, CancellationToken cancellationToken)
    {
        var created = await _services.MarketStaffService.AddAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.MarketStaffId }, ApiResponse<MarketStaff>.SuccessResponse(created, "Created"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] MarketStaff input, CancellationToken cancellationToken)
    {
        var existing = await _services.MarketStaffService.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (existing == null) return NotFound(ApiResponse<object>.ErrorResponse("MarketStaff not found"));

        input.MarketStaffId = id;
        await _services.MarketStaffService.UpdateAsync(input, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _services.MarketStaffService.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (existing == null) return NotFound(ApiResponse<object>.ErrorResponse("MarketStaff not found"));

        await _services.MarketStaffService.DeleteAsync(existing, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Deleted"));
    }
}
