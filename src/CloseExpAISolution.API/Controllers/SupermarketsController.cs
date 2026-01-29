using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupermarketsController : ControllerBase
{
    private readonly IServiceProviders _services;

    public SupermarketsController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<Supermarket>>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var items = (await _services.SupermarketService.GetAllAsync()).ToList();
        var total = items.Count;
        var pageItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var result = new PaginatedResult<Supermarket>
        {
            Items = pageItems,
            TotalResult = total,
            Rage = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<Supermarket>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Supermarket>>> GetById(Guid id)
    {
        var item = await _services.SupermarketService.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (item == null) return NotFound(ApiResponse<Supermarket>.ErrorResponse("Supermarket not found"));
        return Ok(ApiResponse<Supermarket>.SuccessResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Supermarket>>> Create([FromBody] Supermarket input, CancellationToken cancellationToken)
    {
        var created = await _services.SupermarketService.AddAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.SupermarketId }, ApiResponse<Supermarket>.SuccessResponse(created, "Created"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] Supermarket input, CancellationToken cancellationToken)
    {
        var existing = await _services.SupermarketService.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (existing == null) return NotFound(ApiResponse<object>.ErrorResponse("Supermarket not found"));

        input.SupermarketId = id;
        await _services.SupermarketService.UpdateAsync(input, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _services.SupermarketService.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (existing == null) return NotFound(ApiResponse<object>.ErrorResponse("Supermarket not found"));

        await _services.SupermarketService.DeleteAsync(existing, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Deleted"));
    }
}
