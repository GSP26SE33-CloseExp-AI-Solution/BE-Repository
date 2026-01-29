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
    public async Task<ActionResult<IEnumerable<Supermarket>>> GetAll()
    {
        var items = await _services.SupermarketService.GetAllAsync();
        return Ok(items);
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

