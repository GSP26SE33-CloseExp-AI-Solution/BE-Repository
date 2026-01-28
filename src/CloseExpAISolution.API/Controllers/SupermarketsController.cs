using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Application.DTOs.Response;
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
    public async Task<ActionResult<PaginatedResult<Supermarket>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var items = (await _services.SupermarketService.GetAllAsync()).ToList();
        var total = items.Count;
        var pageItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        return Ok(new PaginatedResult<Supermarket>
        {
            Items = pageItems,
            TotalResult = total,
            Rage = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Supermarket>> GetById(Guid id)
    {
        var item = await _services.SupermarketService.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Supermarket>> Create([FromBody] Supermarket input, CancellationToken cancellationToken)
    {
        var created = await _services.SupermarketService.AddAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.SupermarketId }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Supermarket input, CancellationToken cancellationToken)
    {
        var existing = await _services.SupermarketService.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (existing == null) return NotFound();

        input.SupermarketId = id;
        await _services.SupermarketService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _services.SupermarketService.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (existing == null) return NotFound();

        await _services.SupermarketService.DeleteAsync(existing, cancellationToken);
        return NoContent();
    }
}

