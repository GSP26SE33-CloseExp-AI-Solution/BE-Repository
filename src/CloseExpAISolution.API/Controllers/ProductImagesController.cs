using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductImagesController : ControllerBase
{
    private readonly IServiceProviders _services;

    public ProductImagesController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductImage>>> GetAll()
    {
        var items = await _services.ProductImageService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductImage>> GetById(Guid id)
    {
        var item = await _services.ProductImageService.FirstOrDefaultAsync(x => x.ProductImageId == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ProductImage>> Create([FromBody] ProductImage input, CancellationToken cancellationToken)
    {
        var created = await _services.ProductImageService.AddAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductImageId }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductImage input, CancellationToken cancellationToken)
    {
        var existing = await _services.ProductImageService.FirstOrDefaultAsync(x => x.ProductImageId == id);
        if (existing == null) return NotFound();

        input.ProductImageId = id;
        await _services.ProductImageService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _services.ProductImageService.FirstOrDefaultAsync(x => x.ProductImageId == id);
        if (existing == null) return NotFound();

        await _services.ProductImageService.DeleteAsync(existing, cancellationToken);
        return NoContent();
    }
}

