using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IServiceProviders _services;
    private readonly IProductWorkflowService _workflowService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IServiceProviders services,
        IProductWorkflowService workflowService,
        ILogger<ProductsController> logger)
    {
        _services = services;
        _workflowService = workflowService;
        _logger = logger;
    }

    #region Basic CRUD

    /// <summary>
    /// Get all products with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductResponseDto>>>> GetAll(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var items = (await _services.ProductService.GetAllWithImagesAsync()).ToList();
        var total = items.Count;
        var pageItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var result = new PaginatedResult<ProductResponseDto>
        {
            Items = pageItems,
            TotalResult = total,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<ProductResponseDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> GetById(Guid id)
    {
        var item = await _services.ProductService.GetByIdWithImagesAsync(id);
        if (item == null) return NotFound(ApiResponse<ProductResponseDto>.ErrorResponse("Không tìm thấy sản phẩm"));
        return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(item));
    }

    /// <summary>
    /// Create a product
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> Create(
        [FromBody] CreateProductRequestDto request, 
        CancellationToken cancellationToken)
    {
        var created = await _services.ProductService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, ApiResponse<ProductResponseDto>.SuccessResponse(created, "Tạo thành công"));
    }

    /// <summary>
    /// Create product with images. Used when MarketStaff creates a product and uploads images to R2.
    /// </summary>
    [HttpPost("with-images")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> CreateWithImages(
        [FromForm] Guid SupermarketId,
        [FromForm] string Name,
        [FromForm] string Brand,
        [FromForm] string Category,
        [FromForm] string Barcode,
        [FromForm] bool IsFreshFood,
        [FromForm] IFormFileCollection? files,
        CancellationToken cancellationToken)
    {
        var request = new CreateProductRequestDto
        {
            SupermarketId = SupermarketId,
            Name = Name,
            Brand = Brand,
            Category = Category,
            Barcode = Barcode,
            IsFreshFood = IsFreshFood
        };

        var created = await _services.ProductService.CreateProductAsync(request, cancellationToken);

        if (files != null && files.Count > 0)
        {
            foreach (var file in files.Where(f => f.Length > 0))
            {
                await using var stream = file.OpenReadStream();
                await _services.R2StorageService.UploadProductImageToR2Async(
                    stream,
                    file.FileName,
                    file.ContentType,
                    created.ProductId,
                    cancellationToken);
            }
        }

        var result = await _services.ProductService.GetByIdWithImagesAsync(created.ProductId);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, ApiResponse<ProductResponseDto>.SuccessResponse(result!, "Product created with images"));
    }

    /// <summary>
    /// Update a product
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, 
        [FromBody] UpdateProductRequestDto request, 
        CancellationToken cancellationToken)
    {
        try
        {
            await _services.ProductService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật thành công"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm"));
        }
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _services.ProductService.DeleteProductAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null!, "Xóa thành công"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm"));
        }
    }

    #endregion

    #region Query by Status

    /// <summary>
    /// Get products by status for a supermarket
    /// </summary>
    [HttpGet("by-status/{supermarketId:guid}")]
    public async Task<ActionResult<ApiResponse<List<ProductResponseDto>>>> GetByStatus(
        Guid supermarketId,
        [FromQuery] ProductState status,
        CancellationToken cancellationToken)
    {
        var products = await _workflowService.GetProductsByStatusAsync(supermarketId, status, cancellationToken);
        return Ok(ApiResponse<List<ProductResponseDto>>.SuccessResponse(products.ToList()));
    }

    /// <summary>
    /// Get workflow summary for a supermarket (count by status)
    /// </summary>
    [HttpGet("workflow-summary/{supermarketId:guid}")]
    public async Task<ActionResult<ApiResponse<WorkflowSummaryDto>>> GetWorkflowSummary(
        Guid supermarketId,
        CancellationToken cancellationToken)
    {
        var summary = await _workflowService.GetWorkflowSummaryAsync(supermarketId, cancellationToken);
        return Ok(ApiResponse<WorkflowSummaryDto>.SuccessResponse(summary));
    }

    #endregion

    #region Workflow Actions

    /// <summary>
    /// Upload product image and create draft product using AI OCR.
    /// - Uploads image to R2 storage
    /// - Calls AI OCR to extract product info (name, brand, expiry date, barcode)
    /// - Creates a DRAFT product with extracted info
    /// </summary>
    [HttpPost("upload-ocr")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> UploadAndOcr(
        [FromForm] Guid supermarketId,
        [FromForm] string createdBy,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Image file is required"));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _workflowService.UploadAndExtractAsync(
                supermarketId,
                createdBy,
                stream,
                file.FileName,
                file.ContentType,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.ProductId },
                ApiResponse<ProductResponseDto>.SuccessResponse(result, "Product draft created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading and extracting product");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Verify a draft product and set original price.
    /// - Allows staff to correct OCR-extracted info if needed
    /// - Sets the original price (required for pricing calculation)
    /// - Changes status from DRAFT to VERIFIED
    /// - Returns AI pricing suggestion
    /// </summary>
    [HttpPost("{id:guid}/verify")]
    public async Task<ActionResult<ApiResponse<PricingSuggestionResponseDto>>> Verify(
        Guid id,
        [FromBody] VerifyProductRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.VerifyProductAsync(id, request, cancellationToken);
            return Ok(ApiResponse<PricingSuggestionResponseDto>.SuccessResponse(
                result,
                "Product verified successfully. Please review the suggested price."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Product not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying product {ProductId}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get AI pricing suggestion for a verified product.
    /// Returns recommended price based on expiry date, category, market prices, etc.
    /// </summary>
    [HttpGet("{id:guid}/pricing-suggestion")]
    public async Task<ActionResult<ApiResponse<PricingSuggestionResponseDto>>> GetPricingSuggestion(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.GetPricingSuggestionAsync(id, cancellationToken);
            return Ok(ApiResponse<PricingSuggestionResponseDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Product not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pricing suggestion for {ProductId}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Confirm the final price for a verified product.
    /// - Accepts AI suggested price or allows staff to set custom price
    /// - Records staff feedback for AI improvement
    /// - Changes status from VERIFIED to PRICED
    /// </summary>
    [HttpPost("{id:guid}/confirm-price")]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> ConfirmPrice(
        Guid id,
        [FromBody] ConfirmPriceRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.ConfirmPriceAsync(id, request, cancellationToken);
            return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(
                result,
                "Price confirmed successfully. Product is ready to publish."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Product not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming price for {ProductId}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Publish a priced product to make it visible to customers.
    /// Changes status from PRICED to PUBLISHED.
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> Publish(
        Guid id,
        [FromBody] PublishProductRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.PublishProductAsync(id, request, cancellationToken);
            return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(
                result,
                "Product published successfully."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Product not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing product {ProductId}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Quick approve: Verify, confirm price, and publish in one step (for trusted staff).
    /// </summary>
    [HttpPost("{id:guid}/quick-approve")]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> QuickApprove(
        Guid id,
        [FromBody] QuickApproveRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.QuickApproveAsync(id, request, cancellationToken);
            return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(
                result,
                "Product approved and published successfully."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Product not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error quick approving product {ProductId}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    #endregion
}
