using CloseExpAISolution.API.Helpers;
using CloseExpAISolution.Application.AIService.Configuration;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Application.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly int WorkflowAiTimeoutSeconds;

    private readonly IServiceProviders _services;
    private readonly IProductWorkflowService _workflowService;
    private readonly IExcelImportService _excelImportService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IServiceProviders services,
        IProductWorkflowService workflowService,
        IExcelImportService excelImportService,
        IOptions<AIServiceSettings> aiServiceOptions,
        ILogger<ProductsController> logger)
    {
        _services = services;
        _workflowService = workflowService;
        _excelImportService = excelImportService;
        _logger = logger;
        WorkflowAiTimeoutSeconds = aiServiceOptions.Value.TimeoutSeconds;

        if (WorkflowAiTimeoutSeconds <= 0)
            throw new InvalidOperationException("AIService:TimeoutSeconds phải là số nguyên dương.");
    }

    #region Basic CRUD

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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> GetById(Guid id)
    {
        var item = await _services.ProductService.GetByIdWithImagesAsync(id);
        if (item == null) return NotFound(ApiResponse<ProductResponseDto>.ErrorResponse("Không tìm thấy sản phẩm"));
        return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(item));
    }

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

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> Create(
        [FromBody] CreateProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var created = await _services.ProductService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, ApiResponse<ProductResponseDto>.SuccessResponse(created, "Tạo thành công"));
    }

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
            CategoryName = Category,
            Barcode = Barcode,
            Type = Type,
            Sku = Sku,
            ResponsibleOrg = ResponsibleOrg,
            isFeatured = isFeatured,
            Tags = Array.Empty<string>(),
            Detail = new ProductDetailRequestDto
            {
                Brand = Brand,
                Ingredients = Ingredients,
                NutritionFactsJson = Nutrition,
                UsageInstructions = Usage,
                Manufacturer = Manufacturer,
                SafetyWarnings = Warning
            }
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

        var result = await _services.ProductService.GetByIdWithImagesAsync(created.ProductId, includeHiddenDeletedProducts: true);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, ApiResponse<ProductResponseDto>.SuccessResponse(result!, "Product created with images"));
    }

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

    #region Supermarket Staff Workflow APIs

    [Authorize(Roles = "SupermarketStaff")]
    [HttpPost("workflow/identify")]
    [ProducesResponseType(typeof(ApiResponse<StaffProductIdentificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<StaffProductIdentificationResponseDto>>> IdentifyProductForStaff(
        [FromBody] StaffProductIdentificationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Barcode is required"));
        }

        var supermarketIdResult = await GetCurrentStaffSupermarketIdAsync();
        if (!supermarketIdResult.Success)
        {
            return supermarketIdResult.ErrorResult!;
        }

        try
        {
            var result = await _workflowService.IdentifyProductForStaffAsync(request.Barcode.Trim(), supermarketIdResult.SupermarketId!.Value, cancellationToken);
            return Ok(ApiResponse<StaffProductIdentificationResponseDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying product for barcode {Barcode}", request.Barcode);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [Authorize(Roles = "SupermarketStaff")]
    [HttpPost("workflow/analyze-image")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<OcrAnalysisResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status408RequestTimeout)]
    public async Task<ActionResult<ApiResponse<OcrAnalysisResponseDto>>> AnalyzeProductImageForStaff(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Image file is required"));
        }

        var supermarketIdResult = await GetCurrentStaffSupermarketIdAsync();
        if (!supermarketIdResult.Success)
        {
            return supermarketIdResult.ErrorResult!;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(WorkflowAiTimeoutSeconds));

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _workflowService.AnalyzeProductImageAsync(
                supermarketIdResult.SupermarketId!.Value,
                stream,
                file.FileName,
                file.ContentType,
                timeoutCts.Token);

            return Ok(ApiResponse<OcrAnalysisResponseDto>.SuccessResponse(
                result,
                $"AI OCR completed. Timeout threshold: {WorkflowAiTimeoutSeconds}s. You can still input manually if needed."));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout,
                ApiResponse<object>.ErrorResponse($"AI OCR timeout after {WorkflowAiTimeoutSeconds}s. Please retry or use manual fallback."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image for supermarket staff");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [Authorize(Roles = "SupermarketStaff")]
    [HttpPost("workflow/products")]
    [ProducesResponseType(typeof(ApiResponse<CreateNewProductResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CreateNewProductResponseDto>>> CreateProductForStaffWorkflow(
        [FromBody] StaffCreateProductFromWorkflowRequestDto request,
        CancellationToken cancellationToken)
    {
        var supermarketIdResult = await GetCurrentStaffSupermarketIdAsync();
        if (!supermarketIdResult.Success)
        {
            return supermarketIdResult.ErrorResult!;
        }

        try
        {
            var staffName = GetCurrentStaffDisplayName();
            var result = await _workflowService.CreateProductFromStaffWorkflowAsync(
                request,
                supermarketIdResult.SupermarketId!.Value,
                staffName,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.ProductId },
                ApiResponse<CreateNewProductResponseDto>.SuccessResponse(
                    result,
                    "Product created and verified by staff. Continue to lot creation & pricing."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product from staff workflow");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [Authorize(Roles = "SupermarketStaff")]
    [HttpPost("workflow/lots/create-and-publish")]
    [ProducesResponseType(typeof(ApiResponse<StaffCreateLotAndPublishResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status408RequestTimeout)]
    public async Task<ActionResult<ApiResponse<StaffCreateLotAndPublishResponseDto>>> CreateLotAndPublishForStaffWorkflow(
        [FromBody] StaffCreateLotAndPublishRequestDto request,
        CancellationToken cancellationToken)
    {
        var supermarketIdResult = await GetCurrentStaffSupermarketIdAsync();
        if (!supermarketIdResult.Success)
        {
            return supermarketIdResult.ErrorResult!;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(WorkflowAiTimeoutSeconds));

        try
        {
            var staffName = GetCurrentStaffDisplayName();
            var result = await _workflowService.CreateLotAndPublishForStaffAsync(
                request,
                supermarketIdResult.SupermarketId!.Value,
                staffName,
                timeoutCts.Token);

            return Ok(ApiResponse<StaffCreateLotAndPublishResponseDto>.SuccessResponse(
                result,
                $"StockLot created, priced and published. Timeout threshold: {WorkflowAiTimeoutSeconds}s."));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout,
                ApiResponse<object>.ErrorResponse($"AI pricing timeout after {WorkflowAiTimeoutSeconds}s. Please retry or switch to manual fallback."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lot and publishing for staff workflow");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    #endregion

    #region Lookup APIs

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

    [HttpGet("lots/supermarket/{supermarketId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<StockLotDetailDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<StockLotDetailDto>>>> GetStockLotsBySupermarket(
        Guid supermarketId,
        [FromQuery] ExpiryStatus? expiryStatus = null,
        [FromQuery] ProductWeightType? weightType = null,
        [FromQuery] bool? isFreshFood = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new StockLotFilterDto
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

        var (items, totalCount) = await _services.ProductService.GetStockLotsBySupermarketAsync(filter);

        var result = new PaginatedResult<StockLotDetailDto>
        {
            Items = items,
            TotalResult = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<StockLotDetailDto>>.SuccessResponse(
            result,
            $"Tìm thấy {totalCount} lô sản phẩm"));
    }

    [Authorize(Roles = "SupermarketStaff")]
    [HttpGet("my-supermarket/lots")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<StockLotDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<StockLotDetailDto>>>> GetMySupplierStockLots(
        [FromQuery] ExpiryStatus? expiryStatus = null,
        [FromQuery] ProductWeightType? weightType = null,
        [FromQuery] bool? isFreshFood = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var supermarketIdResult = await GetCurrentStaffSupermarketIdAsync();
        if (!supermarketIdResult.Success)
            return supermarketIdResult.ErrorResult!;
        var supermarketId = supermarketIdResult.SupermarketId!.Value;

        var filter = new StockLotFilterDto
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

        var (items, totalCount) = await _services.ProductService.GetStockLotsBySupermarketAsync(
            filter,
            includeHiddenDeletedProducts: true);

        var result = new PaginatedResult<StockLotDetailDto>
        {
            Items = items,
            TotalResult = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<StockLotDetailDto>>.SuccessResponse(
            result,
            $"Tìm thấy {totalCount} lô sản phẩm"));
    }

    [Authorize(Roles = "SupermarketStaff")]
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
        var supermarketIdResult = await GetCurrentStaffSupermarketIdAsync();
        if (!supermarketIdResult.Success)
            return supermarketIdResult.ErrorResult!;
        var supermarketId = supermarketIdResult.SupermarketId!.Value;

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var (items, totalCount) = await _services.ProductService.GetProductsBySupermarketAsync(
            supermarketId,
            searchTerm,
            category,
            pageNumber,
            pageSize,
            includeHiddenDeletedProducts: true);

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

    [HttpGet("by-status/{supermarketId:guid}")]
    public async Task<ActionResult<ApiResponse<List<ProductResponseDto>>>> GetByStatus(
        Guid supermarketId,
        [FromQuery] ProductState status,
        CancellationToken cancellationToken)
    {
        var products = await _workflowService.GetProductsByStatusAsync(supermarketId, status, cancellationToken);
        return Ok(ApiResponse<List<ProductResponseDto>>.SuccessResponse(products.ToList()));
    }

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
                "Product verified successfully. Use lots/{lotId}/pricing-suggestion endpoint to get price recommendation."));
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

    #endregion

    #region New Workflow - Barcode First

    [HttpGet("scan/{barcode}")]
    public async Task<ActionResult<ApiResponse<ScanBarcodeResponseDto>>> ScanBarcode(
        string barcode,
        [FromQuery] Guid supermarketId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.ScanBarcodeAsync(barcode, supermarketId, cancellationToken);
            return Ok(ApiResponse<ScanBarcodeResponseDto>.SuccessResponse(result));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning barcode {Barcode}", barcode);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("lots/from-existing")]
    public async Task<ActionResult<ApiResponse<StockLotResponseDto>>> CreateLotFromExisting(
        [FromBody] CreateStockLotFromExistingDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.CreateStockLotFromExistingAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetStockLot),
                new { lotId = result.LotId },
                ApiResponse<StockLotResponseDto>.SuccessResponse(result, "StockLot created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lot from existing product");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("analyze-image")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<OcrAnalysisResponseDto>>> AnalyzeImage(
        [FromForm] Guid supermarketId,
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
            var result = await _workflowService.AnalyzeProductImageAsync(
                supermarketId, stream, file.FileName, file.ContentType, cancellationToken);
            return Ok(ApiResponse<OcrAnalysisResponseDto>.SuccessResponse(
                result, "Image analyzed. Please verify the extracted info and create product."));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("create-new")]
    public async Task<ActionResult<ApiResponse<CreateNewProductResponseDto>>> CreateNewProduct(
        [FromBody] CreateNewProductRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.CreateNewProductAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.ProductId },
                ApiResponse<CreateNewProductResponseDto>.SuccessResponse(
                    result,
                    $"Product created successfully (Draft). Next: Verify product using POST /api/products/{result.ProductId}/verify"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new product");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("lots/{lotId:guid}")]
    public async Task<ActionResult<ApiResponse<StockLotResponseDto>>> GetStockLot(
        Guid lotId,
        CancellationToken cancellationToken)
    {
        var result = await _workflowService.GetStockLotAsync(lotId, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<object>.ErrorResponse("StockLot not found"));
        return Ok(ApiResponse<StockLotResponseDto>.SuccessResponse(result));
    }

    [HttpGet("  /{supermarketId:guid}")]
    public async Task<ActionResult<ApiResponse<List<StockLotResponseDto>>>> GetLotsByStatus(
        Guid supermarketId,
        [FromQuery] ProductState status,
        CancellationToken cancellationToken)
    {
        var lots = await _workflowService.GetStockLotsByStatusAsync(supermarketId, status, cancellationToken);
        return Ok(ApiResponse<List<StockLotResponseDto>>.SuccessResponse(lots.ToList()));
    }

    [HttpPost("lots/{lotId:guid}/pricing-suggestion")]
    public async Task<ActionResult<ApiResponse<PricingSuggestionResponseDto>>> GetLotPricingSuggestion(
        Guid lotId,
        [FromBody] GetPricingSuggestionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.GetLotPricingSuggestionAsync(lotId, request, cancellationToken);
            return Ok(ApiResponse<PricingSuggestionResponseDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pricing for lot {LotId}", lotId);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("lots/{lotId:guid}/confirm-price")]
    public async Task<ActionResult<ApiResponse<StockLotResponseDto>>> ConfirmLotPrice(
        Guid lotId,
        [FromBody] ConfirmPriceRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.ConfirmLotPriceAsync(lotId, request, cancellationToken);
            return Ok(ApiResponse<StockLotResponseDto>.SuccessResponse(result, "Price confirmed"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming price for lot {LotId}", lotId);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("lots/{lotId:guid}/publish")]
    public async Task<ActionResult<ApiResponse<StockLotResponseDto>>> PublishLot(
        Guid lotId,
        [FromBody] PublishProductRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workflowService.PublishStockLotAsync(lotId, request, cancellationToken);
            return Ok(ApiResponse<StockLotResponseDto>.SuccessResponse(result, "StockLot published"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing lot {LotId}", lotId);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    #endregion

    #region Excel Import

    [HttpPost("import/preview")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ExcelPreviewResponseDto>>> PreviewExcel(
        IFormFile file,
        [FromQuery] int headerRow = 0,
        [FromQuery] int previewRows = 5,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Excel file is required"));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _excelImportService.PreviewExcelAsync(stream, headerRow, previewRows, cancellationToken);
            return Ok(ApiResponse<ExcelPreviewResponseDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing Excel file");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("import")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ExcelImportResponseDto>>> ImportFromExcel(
        IFormFile file,
        [FromForm] Guid supermarketId,
        [FromForm] string importedBy,
        [FromForm] string columnMappingsJson,
        [FromQuery] int dataStartRow = 1,
        [FromQuery] bool skipErrorRows = true,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Excel file is required"));
        }

        try
        {
            var columnMappings = System.Text.Json.JsonSerializer.Deserialize<List<ExcelColumnMappingDto>>(columnMappingsJson);
            if (columnMappings == null || !columnMappings.Any())
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Column mappings are required"));
            }

            var request = new ExcelImportRequestDto
            {
                SupermarketId = supermarketId,
                ImportedBy = importedBy,
                ColumnMappings = columnMappings,
                DataStartRow = dataStartRow,
                SkipErrorRows = skipErrorRows
            };

            await using var stream = file.OpenReadStream();
            var result = await _excelImportService.ImportProductsAsync(stream, request, cancellationToken);
            return Ok(ApiResponse<ExcelImportResponseDto>.SuccessResponse(
                result, $"Import completed: {result.SuccessCount} success, {result.ErrorCount} errors"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing from Excel");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("import/fields")]
    public ActionResult<ApiResponse<string[]>> GetImportFields()
    {
        var fields = _excelImportService.GetAvailableFields().ToArray();
        return Ok(ApiResponse<string[]>.SuccessResponse(fields));
    }

    #endregion

    private async Task<(bool Success, Guid? SupermarketId, ActionResult? ErrorResult)> GetCurrentStaffSupermarketIdAsync()
    {
        var userId = StaffClaimsParser.ReadUserId(User);
        if (userId == null)
            return (false, null, Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng")));

        var (jwtStaff, jwtMarket) = StaffClaimsParser.Read(User);
        var resolution = await _services.MarketStaffService.ResolveStaffContextAsync(userId.Value, jwtStaff, jwtMarket);
        if (!resolution.Success)
            return (false, null, BadRequest(ApiResponse<object>.ErrorResponse(resolution.ErrorMessage!)));

        return (true, resolution.SupermarketId, null);
    }

    private string GetCurrentStaffDisplayName()
    {
        var fullName = User.FindFirst(ClaimTypes.Name)?.Value;
        return string.IsNullOrWhiteSpace(fullName) ? "Supermarket Staff" : fullName;
    }
}
