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
    public async Task<ActionResult<IEnumerable<MarketStaff>>> GetAll()
    {
        var items = await _services.MarketStaffService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MarketStaff>> GetById(Guid id)
    {
        var item = await _services.MarketStaffService.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<MarketStaff>> Create([FromBody] MarketStaff input, CancellationToken cancellationToken)
    {
        var created = await _services.MarketStaffService.AddAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.MarketStaffId }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] MarketStaff input, CancellationToken cancellationToken)
    {
        var existing = await _services.MarketStaffService.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (existing == null) return NotFound();

        input.MarketStaffId = id;
        await _services.MarketStaffService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _services.MarketStaffService.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (existing == null) return NotFound();

        await _services.MarketStaffService.DeleteAsync(existing, cancellationToken);
        return NoContent();
    }
}

