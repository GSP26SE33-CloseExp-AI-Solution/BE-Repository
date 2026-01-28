using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIVerificationLogsController : ControllerBase
{
    private readonly IServiceProviders _services;

    public AIVerificationLogsController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AIVerificationLog>>> GetAll()
    {
        var items = await _services.AIVerificationLogService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AIVerificationLog>> GetById(Guid id)
    {
        var item = await _services.AIVerificationLogService.FirstOrDefaultAsync(x => x.VerificationId == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<AIVerificationLog>> Create([FromBody] AIVerificationLog input, CancellationToken cancellationToken)
    {
        var created = await _services.AIVerificationLogService.AddAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.VerificationId }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AIVerificationLog input, CancellationToken cancellationToken)
    {
        var existing = await _services.AIVerificationLogService.FirstOrDefaultAsync(x => x.VerificationId == id);
        if (existing == null) return NotFound();

        input.VerificationId = id;
        await _services.AIVerificationLogService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _services.AIVerificationLogService.FirstOrDefaultAsync(x => x.VerificationId == id);
        if (existing == null) return NotFound();

        await _services.AIVerificationLogService.DeleteAsync(existing, cancellationToken);
        return NoContent();
    }
}

