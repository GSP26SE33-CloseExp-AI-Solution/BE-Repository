using CloseExpAISolution.API.Helpers;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SupermarketStaff,Admin")]
public class SupermarketStaffController : ControllerBase
{
    private readonly IServiceProviders _services;
    private readonly ILogger<SupermarketStaffController> _logger;
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private const long MaxImageSize = 5 * 1024 * 1024; // 5MB/file

    public SupermarketStaffController(IServiceProviders services, ILogger<SupermarketStaffController> logger)
    {
        _services = services;
        _logger = logger;
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductResponseDto>>>> GetMySupermarketProducts(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var supermarketId = await ResolveCurrentSupermarketIdAsync();
        if (!supermarketId.HasValue)
            return Forbid();

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var (items, total) = await _services.ProductService.GetProductsBySupermarketAsync(
            supermarketId.Value,
            searchTerm,
            category,
            pageNumber,
            pageSize,
            includeHiddenDeletedProducts: true);

        var result = new PaginatedResult<ProductResponseDto>
        {
            Items = items,
            TotalResult = total,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<ProductResponseDto>>.SuccessResponse(result));
    }

    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> GetMySupermarketProductById(Guid id)
    {
        var supermarketId = await ResolveCurrentSupermarketIdAsync();
        if (!supermarketId.HasValue)
            return Forbid();

        var item = await _services.ProductService.GetByIdWithImagesAsync(id, includeHiddenDeletedProducts: true);
        if (item == null)
            return NotFound(ApiResponse<ProductResponseDto>.ErrorResponse("Không tìm thấy sản phẩm"));
        if (item.SupermarketId != supermarketId.Value)
            return Forbid();

        return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(item));
    }

    [HttpPost("products")]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> CreateMySupermarketProduct(
        [FromBody] CreateProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var supermarketId = await ResolveCurrentSupermarketIdAsync();
        if (!supermarketId.HasValue)
            return Forbid();

        request.SupermarketId = supermarketId.Value;
        var created = await _services.ProductService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMySupermarketProductById), new { id = created.ProductId },
            ApiResponse<ProductResponseDto>.SuccessResponse(created, "Tạo thành công"));
    }

    [HttpPut("products/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMySupermarketProduct(
        Guid id,
        [FromBody] UpdateProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var supermarketId = await ResolveCurrentSupermarketIdAsync();
        if (!supermarketId.HasValue)
            return Forbid();

        var existing = await _services.ProductService.GetByIdWithImagesAsync(id, includeHiddenDeletedProducts: true);
        if (existing == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm"));
        if (existing.SupermarketId != supermarketId.Value)
            return Forbid();

        request.SupermarketId = supermarketId.Value;
        await _services.ProductService.UpdateProductAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật thành công"));
    }

    [HttpDelete("products/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMySupermarketProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        var supermarketId = await ResolveCurrentSupermarketIdAsync();
        if (!supermarketId.HasValue)
            return Forbid();

        var existing = await _services.ProductService.GetByIdWithImagesAsync(id, includeHiddenDeletedProducts: true);
        if (existing == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm"));
        if (existing.SupermarketId != supermarketId.Value)
            return Forbid();

        await _services.ProductService.DeleteProductAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Xóa thành công"));
    }

    [HttpPut("products/{id:guid}/images")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20MB total request
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> UpdateMySupermarketProductImages(
        Guid id,
        [FromForm] IFormFileCollection files,
        [FromForm] bool replaceExisting = true,
        CancellationToken cancellationToken = default)
    {
        var supermarketId = await ResolveCurrentSupermarketIdAsync();
        if (!supermarketId.HasValue)
            return Forbid();

        var existingProduct = await _services.ProductService.GetByIdWithImagesAsync(id, includeHiddenDeletedProducts: true);
        if (existingProduct == null)
            return NotFound(ApiResponse<ProductResponseDto>.ErrorResponse("Không tìm thấy sản phẩm"));
        if (existingProduct.SupermarketId != supermarketId.Value)
            return Forbid();

        if (files == null || files.Count == 0)
            return BadRequest(ApiResponse<ProductResponseDto>.ErrorResponse("Vui lòng chọn ít nhất 1 ảnh."));

        foreach (var file in files)
        {
            if (file.Length <= 0)
                return BadRequest(ApiResponse<ProductResponseDto>.ErrorResponse($"File {file.FileName} rỗng."));
            if (file.Length > MaxImageSize)
                return BadRequest(ApiResponse<ProductResponseDto>.ErrorResponse($"Ảnh {file.FileName} vượt quá 5MB."));
            if (!AllowedImageTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                return BadRequest(ApiResponse<ProductResponseDto>.ErrorResponse(
                    $"Định dạng {file.ContentType} không hỗ trợ. Chỉ chấp nhận jpeg/png/gif/webp."));
        }

        if (replaceExisting)
        {
            var existingImages = (await _services.ProductImageService.FindAsync(x => x.ProductId == id)).ToList();
            if (existingImages.Count > 0)
                await _services.ProductImageService.DeleteRangeAsync(existingImages, cancellationToken);
        }

        foreach (var file in files.Where(f => f.Length > 0))
        {
            await using var stream = file.OpenReadStream();
            await _services.R2StorageService.UploadProductImageToR2Async(
                stream,
                file.FileName,
                file.ContentType,
                id,
                cancellationToken);
        }

        var updated = await _services.ProductService.GetByIdWithImagesAsync(id, includeHiddenDeletedProducts: true);
        return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(updated!, "Cập nhật ảnh sản phẩm thành công"));
    }

    [HttpPut("my-supermarket/location")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMySupermarketLocation(
        [FromBody] UpdateSupermarketLocationRequestDto request,
        CancellationToken cancellationToken)
    {
        var supermarketId = await ResolveCurrentSupermarketIdAsync();
        if (!supermarketId.HasValue)
            return Forbid();

        var current = await _services.SupermarketService.GetByIdWithDtoAsync(supermarketId.Value);
        if (current == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy siêu thị"));

        var updateRequest = new UpdateSupermarketRequestDto
        {
            Name = current.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ContactPhone = current.ContactPhone,
            Status = current.Status
        };

        await _services.SupermarketService.UpdateSupermarketAsync(supermarketId.Value, updateRequest, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật vị trí siêu thị thành công"));
    }

    private async Task<Guid?> ResolveCurrentSupermarketIdAsync()
    {
        var userId = StaffClaimsParser.ReadUserId(User);
        if (userId == null)
        {
            _logger.LogWarning("Cannot resolve current user id from claims");
            return null;
        }

        var (jwtStaff, jwtMarket) = StaffClaimsParser.Read(User);
        var resolution = await _services.MarketStaffService.ResolveStaffContextAsync(userId.Value, jwtStaff, jwtMarket);
        return resolution.Success ? resolution.SupermarketId : null;
    }
}
