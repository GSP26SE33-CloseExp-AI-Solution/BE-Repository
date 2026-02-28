using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    /// Lấy thông tin chi tiết đầy đủ của sản phẩm (như nhãn sản phẩm trong siêu thị)
    /// </summary>
    /// <remarks>
    /// Trả về thông tin chi tiết bao gồm:
    /// - Thông tin cơ bản: tên, mô tả, thương hiệu
    /// - Thông tin sản phẩm: xuất xứ, khối lượng, thành phần nguyên liệu
    /// - Hướng dẫn sử dụng, cách bảo quản
    /// - Thông tin nhà sản xuất, nhà phân phối
    /// - Thông tin dinh dưỡng (nutrition facts)
    /// - Giá bán, giảm giá, trạng thái hạn sử dụng
    /// </remarks>
    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProductDetail(Guid id)
    {
        var detail = await _services.ProductService.GetProductDetailAsync(id);
        if (detail == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm"));

        return Ok(ApiResponse<ProductDetailDto>.SuccessResponse(detail, "Lấy thông tin chi tiết sản phẩm thành công"));
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
        [FromForm] ProductType Type = ProductType.Standard,
        [FromForm] string Sku = "",
        [FromForm] string Ingredients = "",
        [FromForm] string Nutrition = "",
        [FromForm] string Usage = "",
        [FromForm] string Manufacturer = "",
        [FromForm] string ResponsibleOrg = "",
        [FromForm] string Warning = "",
        [FromForm] bool isActive = true,
        [FromForm] bool isFeatured = false,
        [FromForm] IFormFileCollection? files = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateProductRequestDto
        {
            SupermarketId = SupermarketId,
            Name = Name,
            Brand = Brand,
            Category = Category,
            Barcode = Barcode,
            IsFreshFood = IsFreshFood,
            Type = Type,
            Sku = Sku,
            Ingredients = Ingredients,
            Nutrition = Nutrition,
            Usage = Usage,
            Manufacturer = Manufacturer,
            ResponsibleOrg = ResponsibleOrg,
            Warning = Warning,
            isActive = isActive,
            isFeatured = isFeatured,
            Tags = Array.Empty<string>()
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

    #region Lookup APIs

    /// <summary>
    /// Lấy danh sách các phân loại hạn sử dụng
    /// </summary>
    /// <remarks>
    /// Trả về danh sách các trạng thái hạn sử dụng để sử dụng cho dropdown/filter
    /// 
    /// | Value | Name | Mô tả |
    /// |-------|------|-------|
    /// | 1 | Today | Trong ngày (dưới 24 giờ) - đếm giờ |
    /// | 2 | ExpiringSoon | Sắp hết hạn (1-2 ngày) |
    /// | 3 | ShortTerm | Còn ngắn hạn (3-7 ngày) |
    /// | 4 | LongTerm | Còn dài hạn (8+ ngày) |
    /// | 5 | Expired | Đã hết hạn |
    /// </remarks>
    [HttpGet("expiry-statuses")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IEnumerable<object>>> GetExpiryStatuses()
    {
        var statuses = Enum.GetValues<ExpiryStatus>()
            .Select(e => new
            {
                Value = (int)e,
                Name = e.ToString(),
                Description = e switch
                {
                    ExpiryStatus.Today => "Trong ngày (dưới 24 giờ)",
                    ExpiryStatus.ExpiringSoon => "Sắp hết hạn (1-2 ngày)",
                    ExpiryStatus.ShortTerm => "Còn ngắn hạn (3-7 ngày)",
                    ExpiryStatus.LongTerm => "Còn dài hạn (8+ ngày)",
                    ExpiryStatus.Expired => "Đã hết hạn",
                    _ => ""
                }
            })
            .ToList();

        return Ok(ApiResponse<IEnumerable<object>>.SuccessResponse(statuses, "Danh sách phân loại hạn sử dụng"));
    }

    /// <summary>
    /// Lấy danh sách các loại định lượng sản phẩm
    /// </summary>
    /// <remarks>
    /// Trả về danh sách các loại định lượng để sử dụng cho dropdown/filter
    /// 
    /// | Value | Name | Mô tả |
    /// |-------|------|-------|
    /// | 1 | Fixed | Định lượng cố định (VD: chai 500ml, gói 200g) |
    /// | 2 | Variable | Không cố định - bán theo cân (VD: rau củ quả, thịt cá) |
    /// </remarks>
    [HttpGet("weight-types")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IEnumerable<object>>> GetWeightTypes()
    {
        var types = Enum.GetValues<ProductWeightType>()
            .Select(e => new
            {
                Value = (int)e,
                Name = e.ToString(),
                Description = e switch
                {
                    ProductWeightType.Fixed => "Định lượng cố định (VD: chai 500ml, gói 200g)",
                    ProductWeightType.Variable => "Không cố định - bán theo cân (VD: rau củ quả, thịt cá)",
                    _ => ""
                }
            })
            .ToList();

        return Ok(ApiResponse<IEnumerable<object>>.SuccessResponse(types, "Danh sách loại định lượng"));
    }

    #endregion

    #region Product Lots by Supermarket

    /// <summary>
    /// Lấy danh sách ProductLot theo siêu thị với filter và phân loại hạn sử dụng
    /// </summary>
    /// <remarks>
    /// API này dùng để hiển thị danh sách sản phẩm theo lô cho nhân viên siêu thị.
    /// 
    /// Các nhóm trạng thái hạn sử dụng:
    /// - Today (1): Trong ngày (dưới 24 giờ) - đếm giờ
    /// - ExpiringSoon (2): Sắp hết hạn (1-2 ngày)
    /// - ShortTerm (3): Còn ngắn hạn (3-7 ngày)
    /// - LongTerm (4): Còn dài hạn (8+ ngày)
    /// - Expired (5): Đã hết hạn
    /// 
    /// Loại định lượng:
    /// - Fixed (1): Định lượng cố định (VD: chai 500ml, gói 200g)
    /// - Variable (2): Không cố định - bán theo cân (VD: rau củ quả, thịt cá)
    /// 
    /// Kết quả được sắp xếp theo thứ tự ưu tiên: Today → ExpiringSoon → ShortTerm → LongTerm → Expired (cuối cùng)
    /// </remarks>
    [HttpGet("lots/supermarket/{supermarketId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductLotDetailDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductLotDetailDto>>>> GetProductLotsBySupermarket(
        Guid supermarketId,
        [FromQuery] ExpiryStatus? expiryStatus = null,
        [FromQuery] ProductWeightType? weightType = null,
        [FromQuery] bool? isFreshFood = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new ProductLotFilterDto
        {
            SupermarketId = supermarketId,
            ExpiryStatus = expiryStatus,
            WeightType = weightType,
            IsFreshFood = isFreshFood,
            SearchTerm = searchTerm,
            Category = category,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var (items, totalCount) = await _services.ProductService.GetProductLotsBySupermarketAsync(filter);

        var result = new PaginatedResult<ProductLotDetailDto>
        {
            Items = items,
            TotalResult = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<ProductLotDetailDto>>.SuccessResponse(
            result,
            $"Tìm thấy {totalCount} lô sản phẩm"));
    }

    /// <summary>
    /// [Dành cho nhân viên siêu thị] Lấy danh sách ProductLot của siêu thị mà nhân viên đang làm việc
    /// </summary>
    /// <remarks>
    /// API này tự động lấy supermarketId từ token của nhân viên siêu thị (SupplierStaff).
    /// Chỉ nhân viên siêu thị (SupplierStaff) mới có thể sử dụng API này.
    /// 
    /// **Yêu cầu Authorization:** Bearer Token của SupplierStaff
    /// 
    /// Các nhóm trạng thái hạn sử dụng:
    /// - Today (1): Trong ngày (dưới 24 giờ) - đếm giờ
    /// - ExpiringSoon (2): Sắp hết hạn (1-2 ngày)
    /// - ShortTerm (3): Còn ngắn hạn (3-7 ngày)
    /// - LongTerm (4): Còn dài hạn (8+ ngày)
    /// - Expired (5): Đã hết hạn
    /// 
    /// Kết quả được sắp xếp theo thứ tự ưu tiên: Today → ExpiringSoon → ShortTerm → LongTerm → Expired (cuối cùng)
    /// </remarks>
    [Authorize(Roles = "SupplierStaff")]
    [HttpGet("my-supermarket/lots")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductLotDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductLotDetailDto>>>> GetMySupplierProductLots(
        [FromQuery] ExpiryStatus? expiryStatus = null,
        [FromQuery] ProductWeightType? weightType = null,
        [FromQuery] bool? isFreshFood = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        // Lấy UserId từ token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));
        }

        // Lấy SupermarketId từ MarketStaff
        var supermarketId = await _services.MarketStaffService.GetSupermarketIdByUserIdAsync(userId);
        if (supermarketId == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Bạn chưa được gán vào siêu thị nào"));
        }

        var filter = new ProductLotFilterDto
        {
            SupermarketId = supermarketId.Value,
            ExpiryStatus = expiryStatus,
            WeightType = weightType,
            IsFreshFood = isFreshFood,
            SearchTerm = searchTerm,
            Category = category,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var (items, totalCount) = await _services.ProductService.GetProductLotsBySupermarketAsync(filter);

        var result = new PaginatedResult<ProductLotDetailDto>
        {
            Items = items,
            TotalResult = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<ProductLotDetailDto>>.SuccessResponse(
            result,
            $"Tìm thấy {totalCount} lô sản phẩm"));
    }

    /// <summary>
    /// Lấy danh sách sản phẩm của siêu thị mà nhân viên đang làm việc
    /// </summary>
    /// <remarks>
    /// API dành cho SupplierStaff (nhân viên siêu thị) để lấy danh sách sản phẩm của siêu thị họ đang làm việc.
    /// </remarks>
    [Authorize(Roles = "SupplierStaff")]
    [HttpGet("my-supermarket")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductResponseDto>>>> GetMySupplierProducts(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        // Lấy UserId từ token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng"));
        }

        // Lấy SupermarketId từ MarketStaff
        var supermarketId = await _services.MarketStaffService.GetSupermarketIdByUserIdAsync(userId);
        if (supermarketId == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Bạn chưa được gán vào siêu thị nào"));
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var (items, totalCount) = await _services.ProductService.GetProductsBySupermarketAsync(
            supermarketId.Value, searchTerm, category, pageNumber, pageSize);

        var result = new PaginatedResult<ProductResponseDto>
        {
            Items = items,
            TotalResult = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<ProductResponseDto>>.SuccessResponse(
            result,
            $"Tìm thấy {totalCount} sản phẩm"));
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
        catch (ArgumentException ex) when (ex.ParamName == "supermarketId")
        {
            _logger.LogWarning("Supermarket not found: {SupermarketId}", supermarketId);
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading and extracting product");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Verify a draft product - confirm/correct OCR extracted info.
    /// - Allows staff to correct OCR-extracted info if needed
    /// - Changes status from DRAFT to VERIFIED
    /// - Does NOT calculate pricing (use pricing-suggestion endpoint next)
    /// </summary>
    [HttpPost("{id:guid}/verify")]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> Verify(
        Guid id,
        [FromBody] VerifyProductRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.VerifyProductAsync(id, request, cancellationToken);
            return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(
                result,
                "Product verified successfully. Use pricing-suggestion endpoint to get price recommendation."));
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
    /// - Sets the original price (required for pricing calculation)
    /// - Returns recommended price based on expiry date, category, market prices, etc.
    /// - Saves suggested price to product
    /// </summary>
    [HttpPost("{id:guid}/pricing-suggestion")]
    public async Task<ActionResult<ApiResponse<PricingSuggestionResponseDto>>> GetPricingSuggestion(
        Guid id,
        [FromBody] GetPricingSuggestionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.GetPricingSuggestionAsync(id, request, cancellationToken);
            return Ok(ApiResponse<PricingSuggestionResponseDto>.SuccessResponse(
                result,
                "Pricing suggestion calculated. Use confirm-price endpoint to accept or modify."));
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
