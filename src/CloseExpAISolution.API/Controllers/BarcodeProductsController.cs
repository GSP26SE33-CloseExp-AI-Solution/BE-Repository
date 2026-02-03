using CloseExpAISolution.Application.AIService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

/// <summary>
/// Controller for managing barcode product data.
/// Supports Cache & Crowd-source mechanism.
/// </summary>
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

    /// <summary>
    /// Lookup product information by barcode.
    /// First checks local database, then external APIs if not found.
    /// </summary>
    /// <param name="barcode">Product barcode (EAN-13, UPC-A, etc.)</param>
    /// <returns>Product information if found</returns>
    [HttpGet("lookup/{barcode}")]
    [ProducesResponseType(typeof(BarcodeProductInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BarcodeProductInfo>> Lookup(string barcode)
    {
        var result = await _barcodeLookupService.LookupAsync(barcode);
        
        if (result == null)
        {
            return NotFound(new { 
                message = "Không tìm thấy sản phẩm với mã vạch này",
                barcode = barcode,
                suggestion = "Bạn có thể thêm sản phẩm thủ công bằng endpoint POST /api/BarcodeProducts"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Lookup multiple barcodes in batch.
    /// </summary>
    /// <param name="request">List of barcodes to lookup</param>
    /// <returns>Dictionary of barcode to product info</returns>
    [HttpPost("lookup/batch")]
    [ProducesResponseType(typeof(Dictionary<string, BarcodeProductInfo?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, BarcodeProductInfo?>>> LookupBatch([FromBody] BatchLookupRequest request)
    {
        if (request.Barcodes == null || !request.Barcodes.Any())
        {
            return BadRequest(new { message = "Danh sách barcode không được rỗng" });
        }

        var results = await _barcodeLookupService.LookupBatchAsync(request.Barcodes);
        return Ok(results);
    }

    /// <summary>
    /// Add new product manually (crowd-source).
    /// Used when external APIs don't have the product.
    /// </summary>
    /// <param name="request">Product information to add</param>
    /// <returns>Saved product information</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(BarcodeProductInfo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BarcodeProductInfo>> AddProduct([FromBody] AddBarcodeProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return BadRequest(new { message = "Barcode là bắt buộc" });
        }

        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            return BadRequest(new { message = "Tên sản phẩm là bắt buộc" });
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

        return CreatedAtAction(nameof(Lookup), new { barcode = result.Barcode }, result);
    }

    /// <summary>
    /// Add product from AI OCR result.
    /// Called when AI extracts product info from packaging image.
    /// </summary>
    /// <param name="request">Product information extracted by AI</param>
    /// <returns>Saved product information</returns>
    [HttpPost("from-ocr")]
    [Authorize]
    [ProducesResponseType(typeof(BarcodeProductInfo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BarcodeProductInfo>> AddFromOcr([FromBody] AddBarcodeProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return BadRequest(new { message = "Barcode là bắt buộc" });
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

        return CreatedAtAction(nameof(Lookup), new { barcode = result.Barcode }, result);
    }

    /// <summary>
    /// Update existing product information.
    /// </summary>
    /// <param name="barcode">Barcode to update</param>
    /// <param name="request">Updated product information</param>
    /// <returns>Updated product information</returns>
    [HttpPut("{barcode}")]
    [Authorize]
    [ProducesResponseType(typeof(BarcodeProductInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BarcodeProductInfo>> UpdateProduct(
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
            return NotFound(new { message = "Không tìm thấy sản phẩm với barcode này" });
        }

        _logger.LogInformation(
            "User {UserId} updated barcode product: {Barcode}",
            userId, barcode);

        return Ok(result);
    }

    /// <summary>
    /// Verify/approve a product entry (mark as reviewed).
    /// </summary>
    /// <param name="barcode">Barcode to verify</param>
    /// <returns>Success status</returns>
    [HttpPost("{barcode}/verify")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> VerifyProduct(string barcode)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value ?? "system";

        var success = await _barcodeLookupService.VerifyProductAsync(barcode, userId);

        if (!success)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm với barcode này" });
        }

        _logger.LogInformation("User {UserId} verified barcode product: {Barcode}", userId, barcode);

        return Ok(new { message = "Đã xác minh sản phẩm", barcode = barcode });
    }

    /// <summary>
    /// Get list of products pending review.
    /// </summary>
    /// <returns>List of products awaiting verification</returns>
    [HttpGet("pending-review")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(IEnumerable<BarcodeProductInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BarcodeProductInfo>>> GetPendingReview()
    {
        var results = await _barcodeLookupService.GetPendingReviewAsync();
        return Ok(results);
    }

    /// <summary>
    /// Search products by name or brand.
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="limit">Maximum results (default: 20)</param>
    /// <returns>List of matching products</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<BarcodeProductInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BarcodeProductInfo>>> Search(
        [FromQuery] string q, 
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { message = "Từ khóa tìm kiếm là bắt buộc" });
        }

        var results = await _barcodeLookupService.SearchAsync(q, limit);
        return Ok(results);
    }

    /// <summary>
    /// Check if a barcode is Vietnamese (prefix 893).
    /// </summary>
    /// <param name="barcode">Barcode to check</param>
    /// <returns>Vietnamese status</returns>
    [HttpGet("is-vietnamese/{barcode}")]
    [ProducesResponseType(typeof(VietnameseBarcodeResponse), StatusCodes.Status200OK)]
    public ActionResult<VietnameseBarcodeResponse> IsVietnamese(string barcode)
    {
        var isVietnamese = _barcodeLookupService.IsVietnameseBarcode(barcode);
        return Ok(new VietnameseBarcodeResponse
        {
            Barcode = barcode,
            IsVietnamese = isVietnamese,
            Message = isVietnamese 
                ? "Đây là mã vạch sản phẩm Việt Nam (prefix 893)" 
                : "Đây không phải mã vạch sản phẩm Việt Nam"
        });
    }
}

#region Request/Response Models

public class BatchLookupRequest
{
    public List<string> Barcodes { get; set; } = new();
}

public class AddBarcodeProductRequest
{
    public string Barcode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Manufacturer { get; set; }
    public string? Weight { get; set; }
    public string? Ingredients { get; set; }
    public Dictionary<string, string>? NutritionFacts { get; set; }
    public string? Country { get; set; }
    public string? Source { get; set; }
    public float? Confidence { get; set; }
}

public class UpdateBarcodeProductRequest
{
    public string? ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Manufacturer { get; set; }
    public string? Weight { get; set; }
    public string? Ingredients { get; set; }
    public Dictionary<string, string>? NutritionFacts { get; set; }
    public string? Country { get; set; }
}

public class VietnameseBarcodeResponse
{
    public string Barcode { get; set; } = string.Empty;
    public bool IsVietnamese { get; set; }
    public string Message { get; set; } = string.Empty;
}

#endregion
