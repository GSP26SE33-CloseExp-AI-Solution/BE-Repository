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
            return WorkflowError(StatusCodes.Status400BadRequest, ex.Message, "workflow_identify_failed");
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
        [FromForm] bool manualFallback = false,
        CancellationToken cancellationToken = default)
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

        CancellationTokenSource? timeoutCts = null;
        try
        {
            var tokenForAnalyze = cancellationToken;
            if (!manualFallback)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(WorkflowAiTimeoutSeconds));
                tokenForAnalyze = timeoutCts.Token;
            }

            await using var stream = file.OpenReadStream();
            var result = await _workflowService.AnalyzeProductImageAsync(
                supermarketIdResult.SupermarketId!.Value,
                stream,
                file.FileName,
                file.ContentType,
                skipAi: manualFallback,
                tokenForAnalyze);

            var message = manualFallback
                ? "Image uploaded. AI OCR skipped (manual fallback); enter product fields manually."
                : $"AI OCR completed. Timeout threshold: {WorkflowAiTimeoutSeconds}s. You can still input manually if needed.";

            return Ok(ApiResponse<OcrAnalysisResponseDto>.SuccessResponse(result, message));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !manualFallback)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout,
                ApiResponse<object>.ErrorResponse($"AI OCR timeout after {WorkflowAiTimeoutSeconds}s. Please retry or use manual fallback."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image for supermarket staff");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        finally
        {
            timeoutCts?.Dispose();
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

            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<CreateNewProductResponseDto>.SuccessResponse(
                    result,
                    "Product created and verified by staff. Continue to lot creation & pricing."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product from staff workflow");
            return ex is InvalidOperationException
                ? WorkflowError(StatusCodes.Status409Conflict, ex.Message, "workflow_conflict")
                : WorkflowError(StatusCodes.Status400BadRequest, ex.Message, "workflow_create_product_failed");
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

        CancellationTokenSource? timeoutCts = null;
        try
        {
            var staffName = GetCurrentStaffDisplayName();
            var tokenForWorkflow = cancellationToken;
            if (!request.IsManualFallback)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(WorkflowAiTimeoutSeconds));
                tokenForWorkflow = timeoutCts.Token;
            }

            var result = await _workflowService.CreateLotAndPublishForStaffAsync(
                request,
                supermarketIdResult.SupermarketId!.Value,
                staffName,
                tokenForWorkflow);

            var message = request.IsManualFallback
                ? "StockLot created, priced and published (manual pricing; AI skipped)."
                : $"StockLot created, priced and published. Timeout threshold: {WorkflowAiTimeoutSeconds}s.";

            return Ok(ApiResponse<StaffCreateLotAndPublishResponseDto>.SuccessResponse(result, message));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !request.IsManualFallback)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout,
                ApiResponse<object>.ErrorResponse($"AI pricing timeout after {WorkflowAiTimeoutSeconds}s. Please retry or switch to manual fallback."));
        }
        finally
        {
            timeoutCts?.Dispose();
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

    private ObjectResult WorkflowError(int statusCode, string message, string errorCode)
    {
        return StatusCode(statusCode, ApiResponse<object>.ErrorResponse(message, [errorCode]));
    }
}
