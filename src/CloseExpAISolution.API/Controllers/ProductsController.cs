using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IServiceProviders _services;

    public ProductsController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        var items = await _services.ProductService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetById(Guid id)
    {
        var item = await _services.ProductService.FirstOrDefaultAsync(x => x.ProductId == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product input, CancellationToken cancellationToken)
    {
        var created = await _services.ProductService.AddAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Product input, CancellationToken cancellationToken)
    {
        var existing = await _services.ProductService.FirstOrDefaultAsync(x => x.ProductId == id);
        if (existing == null) return NotFound();

        input.ProductId = id;
        await _services.ProductService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _services.ProductService.FirstOrDefaultAsync(x => x.ProductId == id);
        if (existing == null) return NotFound();

        await _services.ProductService.DeleteAsync(existing, cancellationToken);
        return NoContent();
    }
}

