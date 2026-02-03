using CloseExpAISolution.Application.DTOs.Response;
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
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductImage>>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var items = (await _services.ProductImageService.GetAllAsync()).ToList();
        var total = items.Count;
        var pageItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var result = new PaginatedResult<ProductImage>
        {
            Items = pageItems,
            TotalResult = total,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<ProductImage>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductImage>>> GetById(Guid id)
    {
        var item = await _services.ProductImageService.FirstOrDefaultAsync(x => x.ProductImageId == id);
        if (item == null) return NotFound(ApiResponse<ProductImage>.ErrorResponse("Không tìm thấy hình ảnh sản phẩm"));
        return Ok(ApiResponse<ProductImage>.SuccessResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductImage>>> Create([FromBody] ProductImage input, CancellationToken cancellationToken)
    {
        var created = await _services.ProductImageService.AddAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductImageId }, ApiResponse<ProductImage>.SuccessResponse(created, "Tạo thành công"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] ProductImage input, CancellationToken cancellationToken)
    {
        var existing = await _services.ProductImageService.FirstOrDefaultAsync(x => x.ProductImageId == id);
        if (existing == null) return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy hình ảnh sản phẩm"));

        input.ProductImageId = id;
        await _services.ProductImageService.UpdateAsync(input, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _services.ProductImageService.FirstOrDefaultAsync(x => x.ProductImageId == id);
        if (existing == null) return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy hình ảnh sản phẩm"));

        await _services.ProductImageService.DeleteAsync(existing, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Xóa thành công"));
    }
}
