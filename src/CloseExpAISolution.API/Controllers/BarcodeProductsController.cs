using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.DTOs.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BarcodeProductsController : ControllerBase
{
    private readonly IBarcodeLookupService _barcodeLookupService;
    private readonly ILogger<BarcodeProductsController> _logger;

    public BarcodeProductsController(
        IBarcodeLookupService barcodeLookupService,
        ILogger<BarcodeProductsController> logger)
    {
        _barcodeLookupService = barcodeLookupService;
        _logger = logger;
    }

    [HttpGet("lookup/{barcode}")]
    [ProducesResponseType(typeof(ApiResponse<BarcodeProductInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BarcodeProductInfo>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BarcodeProductInfo>>> Lookup(string barcode)
    {
        var result = await _barcodeLookupService.LookupAsync(barcode);

        if (result == null)
        {
            return NotFound(ApiResponse<BarcodeProductInfo>.ErrorResponse(
                $"Không tìm thấy sản phẩm với mã vạch {barcode}"));
        }

        return Ok(ApiResponse<BarcodeProductInfo>.SuccessResponse(result));
    }

    [HttpPost("lookup/batch")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, BarcodeProductInfo?>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Dictionary<string, BarcodeProductInfo?>>>> LookupBatch([FromBody] BatchLookupRequest request)
    {
        if (request.Barcodes == null || !request.Barcodes.Any())
        {
            return BadRequest(ApiResponse<Dictionary<string, BarcodeProductInfo?>>.ErrorResponse("Danh sách barcode không được rỗng"));
        }

        var results = await _barcodeLookupService.LookupBatchAsync(request.Barcodes);
        return Ok(ApiResponse<Dictionary<string, BarcodeProductInfo?>>.SuccessResponse(results));
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BarcodeProductInfo>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BarcodeProductInfo>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BarcodeProductInfo>>> AddProduct([FromBody] AddBarcodeProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return BadRequest(ApiResponse<BarcodeProductInfo>.ErrorResponse("Barcode là bắt buộc"));
        }

        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            return BadRequest(ApiResponse<BarcodeProductInfo>.ErrorResponse("Tên sản phẩm là bắt buộc"));
        }

        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;

        var productInfo = new BarcodeProductInfo
        {
            Barcode = request.Barcode,
            ProductName = request.ProductName,
            Brand = request.Brand,
            Category = request.Category,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            Manufacturer = request.Manufacturer,
            Weight = request.Weight,
            Ingredients = request.Ingredients,
            NutritionFacts = request.NutritionFacts,
            Country = request.Country,
            Source = request.Source ?? "manual",
            Confidence = request.Confidence ?? 0.7f
        };

        var result = await _barcodeLookupService.SaveProductAsync(productInfo, userId);

        _logger.LogInformation(
            "User {UserId} added barcode product: {Barcode} - {ProductName}",
            userId, request.Barcode, request.ProductName);

        return CreatedAtAction(nameof(Lookup), new { barcode = result.Barcode }, ApiResponse<BarcodeProductInfo>.SuccessResponse(result, "Tạo sản phẩm barcode thành công"));
    }

    [HttpPost("from-ocr")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BarcodeProductInfo>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BarcodeProductInfo>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BarcodeProductInfo>>> AddFromOcr([FromBody] AddBarcodeProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return BadRequest(ApiResponse<BarcodeProductInfo>.ErrorResponse("Barcode là bắt buộc"));
        }

        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;

        var productInfo = new BarcodeProductInfo
        {
            Barcode = request.Barcode,
            ProductName = request.ProductName ?? "Sản phẩm (AI OCR)",
            Brand = request.Brand,
            Category = request.Category,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            Manufacturer = request.Manufacturer,
            Weight = request.Weight,
            Ingredients = request.Ingredients,
            NutritionFacts = request.NutritionFacts,
            Country = request.Country,
            Source = "ai-ocr",
            Confidence = request.Confidence ?? 0.6f
        };

        var result = await _barcodeLookupService.SaveProductAsync(productInfo, userId);

        _logger.LogInformation(
            "AI OCR added barcode product: {Barcode} - {ProductName} (User: {UserId})",
            request.Barcode, request.ProductName, userId);

        return CreatedAtAction(nameof(Lookup), new { barcode = result.Barcode }, ApiResponse<BarcodeProductInfo>.SuccessResponse(result, "Tạo sản phẩm từ OCR thành công"));
    }

    [HttpPut("{barcode}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BarcodeProductInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BarcodeProductInfo>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BarcodeProductInfo>>> UpdateProduct(
        string barcode,
        [FromBody] UpdateBarcodeProductRequest request)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;

        var productInfo = new BarcodeProductInfo
        {
            Barcode = barcode,
            ProductName = request.ProductName,
            Brand = request.Brand,
            Category = request.Category,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            Manufacturer = request.Manufacturer,
            Weight = request.Weight,
            Ingredients = request.Ingredients,
            NutritionFacts = request.NutritionFacts,
            Country = request.Country
        };

        var result = await _barcodeLookupService.UpdateProductAsync(barcode, productInfo, userId);

        if (result == null)
        {
            return NotFound(ApiResponse<BarcodeProductInfo>.ErrorResponse("Không tìm thấy sản phẩm với barcode này"));
        }

        _logger.LogInformation(
            "User {UserId} updated barcode product: {Barcode}",
            userId, barcode);

        return Ok(ApiResponse<BarcodeProductInfo>.SuccessResponse(result, "Cập nhật sản phẩm barcode thành công"));
    }

    [HttpPost("{barcode}/verify")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> VerifyProduct(string barcode)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value ?? "system";

        var success = await _barcodeLookupService.VerifyProductAsync(barcode, userId);

        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm với barcode này"));
        }

        _logger.LogInformation("User {UserId} verified barcode product: {Barcode}", userId, barcode);

        return Ok(ApiResponse<object>.SuccessResponse(new { barcode }, "Đã xác minh sản phẩm"));
    }

    [HttpGet("pending-review")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BarcodeProductInfo>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<BarcodeProductInfo>>>> GetPendingReview()
    {
        var results = await _barcodeLookupService.GetPendingReviewAsync();
        return Ok(ApiResponse<IEnumerable<BarcodeProductInfo>>.SuccessResponse(results));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BarcodeProductInfo>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<BarcodeProductInfo>>>> Search(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(ApiResponse<IEnumerable<BarcodeProductInfo>>.ErrorResponse("Từ khóa tìm kiếm là bắt buộc"));
        }

        var results = await _barcodeLookupService.SearchAsync(q, limit);
        return Ok(ApiResponse<IEnumerable<BarcodeProductInfo>>.SuccessResponse(results));
    }

    [HttpGet("is-vietnamese/{barcode}")]
    [ProducesResponseType(typeof(ApiResponse<VietnameseBarcodeResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<VietnameseBarcodeResponse>> IsVietnamese(string barcode)
    {
        var isVietnamese = _barcodeLookupService.IsVietnameseBarcode(barcode);
        return Ok(ApiResponse<VietnameseBarcodeResponse>.SuccessResponse(new VietnameseBarcodeResponse
        {
            Barcode = barcode,
            IsVietnamese = isVietnamese,
            Message = isVietnamese
                ? "Đây là mã vạch sản phẩm Việt Nam (prefix 893)"
                : "Đây không phải mã vạch sản phẩm Việt Nam"
        }));
    }
}
