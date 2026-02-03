using CloseExpAISolution.Application.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

/// <summary>
/// Controller for AI-powered product operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AIController : ControllerBase
{
    private readonly IAIProductService _aiProductService;
    private readonly ILogger<AIController> _logger;

    public AIController(
        IAIProductService aiProductService,
        ILogger<AIController> logger)
    {
        _aiProductService = aiProductService;
        _logger = logger;
    }

    /// <summary>
    /// Check AI service health status
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(AIHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CheckHealth(CancellationToken cancellationToken)
    {
        var isAvailable = await _aiProductService.IsServiceAvailableAsync(cancellationToken);
        
        var response = new AIHealthResponse
        {
            Status = isAvailable ? "healthy" : "unhealthy",
            Timestamp = DateTime.UtcNow,
            Service = "AI Service"
        };

        return isAvailable 
            ? Ok(response) 
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    /// <summary>
    /// Extract product information from image using OCR
    /// If barcode is detected, will also lookup product info from Open Food Facts
    /// </summary>
    [HttpPost("extract")]
    [ProducesResponseType(typeof(ProductExtractionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ExtractProductInfo(
        [FromBody] ExtractProductRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ImageUrl) && string.IsNullOrEmpty(request.ImageBase64))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Either ImageUrl or ImageBase64 must be provided",
                Code = "INVALID_REQUEST"
            });
        }

        var imageSource = !string.IsNullOrEmpty(request.ImageUrl) 
            ? request.ImageUrl 
            : $"data:image/jpeg;base64,{request.ImageBase64}";

        var result = await _aiProductService.ExtractProductInfoAsync(
            request.ProductId ?? Guid.NewGuid(),
            imageSource,
            request.LookupBarcode,
            cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse
            {
                Error = result.ErrorMessage ?? "AI Service unavailable",
                Code = "AI_SERVICE_ERROR"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get AI-suggested pricing for a product
    /// </summary>
    [HttpPost("pricing")]
    [ProducesResponseType(typeof(PricingSuggestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetPricingSuggestion(
        [FromBody] PricingRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Category))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Category is required",
                Code = "INVALID_REQUEST"
            });
        }

        if (request.OriginalPrice <= 0)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "OriginalPrice must be greater than 0",
                Code = "INVALID_REQUEST"
            });
        }

        if (request.ExpiryDate <= DateTime.UtcNow)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "ExpiryDate must be in the future",
                Code = "INVALID_REQUEST"
            });
        }

        var result = await _aiProductService.GetPriceSuggestionAsync(
            request.Category,
            request.ExpiryDate,
            request.OriginalPrice,
            request.Brand,
            cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse
            {
                Error = result.ErrorMessage ?? "AI Service unavailable",
                Code = "AI_SERVICE_ERROR"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Analyze shelf image for product detection
    /// </summary>
    [HttpPost("analyze-shelf")]
    [ProducesResponseType(typeof(ShelfAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AnalyzeShelf(
        [FromBody] AnalyzeShelfRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ImageUrl) && string.IsNullOrEmpty(request.ImageBase64))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Either ImageUrl or ImageBase64 must be provided",
                Code = "INVALID_REQUEST"
            });
        }

        var imageSource = !string.IsNullOrEmpty(request.ImageUrl) 
            ? request.ImageUrl 
            : $"data:image/jpeg;base64,{request.ImageBase64}";

        var result = await _aiProductService.AnalyzeShelfImageAsync(
            imageSource,
            cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse
            {
                Error = result.ErrorMessage ?? "AI Service unavailable",
                Code = "AI_SERVICE_ERROR"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Process product with full AI pipeline (OCR + Pricing)
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType(typeof(ProductProcessingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ProcessProduct(
        [FromBody] ProcessProductRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ImageUrl) && string.IsNullOrEmpty(request.ImageBase64))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Either ImageUrl or ImageBase64 must be provided",
                Code = "INVALID_REQUEST"
            });
        }

        var imageSource = !string.IsNullOrEmpty(request.ImageUrl) 
            ? request.ImageUrl 
            : $"data:image/jpeg;base64,{request.ImageBase64}";

        var result = await _aiProductService.ProcessProductAsync(
            request.ProductId,
            imageSource,
            cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse
            {
                Error = result.ErrorMessage ?? "AI Service unavailable",
                Code = "AI_SERVICE_ERROR"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Smart scan that automatically determines the appropriate AI endpoint based on image content.
    /// Supports both packaged products (with barcode) and fresh produce (vegetables, fruits, meat, seafood).
    /// Includes special support for Vietnamese products (barcode starting with 893).
    /// </summary>
    [HttpPost("smart-scan")]
    [ProducesResponseType(typeof(SmartScanResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SmartScan(
        [FromBody] SmartScanRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ImageUrl) && string.IsNullOrEmpty(request.ImageBase64))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Either ImageUrl or ImageBase64 must be provided",
                Code = "INVALID_REQUEST"
            });
        }

        var imageSource = !string.IsNullOrEmpty(request.ImageUrl) 
            ? request.ImageUrl 
            : $"data:image/jpeg;base64,{request.ImageBase64}";

        var result = await _aiProductService.SmartScanAsync(
            imageSource,
            request.ProductTypeHint,
            request.LookupBarcode,
            cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse
            {
                Error = result.ErrorMessage ?? "AI Service unavailable",
                Code = "AI_SERVICE_ERROR"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Identify fresh produce from image (vegetables, fruits, meat, seafood)
    /// Returns Vietnamese names, shelf life, storage recommendations
    /// </summary>
    [HttpPost("fresh-produce")]
    [ProducesResponseType(typeof(FreshProduceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> IdentifyFreshProduce(
        [FromBody] FreshProduceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ImageUrl) && string.IsNullOrEmpty(request.ImageBase64))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Either ImageUrl or ImageBase64 must be provided",
                Code = "INVALID_REQUEST"
            });
        }

        var imageSource = !string.IsNullOrEmpty(request.ImageUrl) 
            ? request.ImageUrl 
            : $"data:image/jpeg;base64,{request.ImageBase64}";

        var result = await _aiProductService.IdentifyFreshProduceAsync(
            imageSource,
            cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse
            {
                Error = result.ErrorMessage ?? "AI Service unavailable",
                Code = "AI_SERVICE_ERROR"
            });
        }

        return Ok(result);
    }
}

#region Request/Response Models

public class AIHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Service { get; set; } = string.Empty;
}

public class ExtractProductRequest
{
    public Guid? ProductId { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
    
    /// <summary>
    /// If true, will lookup barcode info from Open Food Facts API
    /// Default is true
    /// </summary>
    public bool LookupBarcode { get; set; } = true;
}

public class PricingRequest
{
    public string Category { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public decimal OriginalPrice { get; set; }
    public string? Brand { get; set; }
}

public class AnalyzeShelfRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
}

public class ProcessProductRequest
{
    public Guid ProductId { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
}

public class SmartScanRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
    
    /// <summary>
    /// Hint about the product type to improve accuracy
    /// Options: "auto", "packaged", "fresh_produce", "barcode"
    /// Default is "auto" which will analyze the image to determine type
    /// </summary>
    public string ProductTypeHint { get; set; } = "auto";
    
    /// <summary>
    /// If true, will lookup barcode info from external databases
    /// </summary>
    public bool LookupBarcode { get; set; } = true;
}

public class FreshProduceRequest
{
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
}

public class SmartScanResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// The detected type of scan performed: "packaged", "fresh_produce", "mixed"
    /// </summary>
    public string ScanType { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates if this is a Vietnamese product (based on barcode starting with 893)
    /// </summary>
    public bool IsVietnameseProduct { get; set; }
    
    /// <summary>
    /// Vietnamese company info (if barcode starts with 893)
    /// </summary>
    public VietnameseBarcodeInfo? VietnameseBarcodeInfo { get; set; }
    
    /// <summary>
    /// Extracted product name
    /// </summary>
    public string? ProductName { get; set; }
    
    /// <summary>
    /// Extracted brand name
    /// </summary>
    public string? Brand { get; set; }
    
    /// <summary>
    /// Extracted barcode
    /// </summary>
    public string? Barcode { get; set; }
    
    /// <summary>
    /// Expiry date (if detected)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>
    /// Manufacturing date (if detected)
    /// </summary>
    public DateTime? ManufacturedDate { get; set; }
    
    /// <summary>
    /// Suggested product category
    /// </summary>
    public string? SuggestedCategory { get; set; }
    
    /// <summary>
    /// Suggested shelf life in days
    /// </summary>
    public int? SuggestedShelfLifeDays { get; set; }
    
    /// <summary>
    /// Storage recommendations
    /// </summary>
    public string? StorageRecommendation { get; set; }
    
    /// <summary>
    /// Usage instructions (Hướng dẫn sử dụng)
    /// </summary>
    public string? UsageInstructions { get; set; }
    
    /// <summary>
    /// Extracted ingredients
    /// </summary>
    public List<string>? Ingredients { get; set; }
    
    /// <summary>
    /// Nutrition facts (Giá trị dinh dưỡng)
    /// </summary>
    public Dictionary<string, object>? NutritionFacts { get; set; }
    
    /// <summary>
    /// Product weight
    /// </summary>
    public string? Weight { get; set; }
    
    /// <summary>
    /// Origin/country of origin
    /// </summary>
    public string? Origin { get; set; }
    
    /// <summary>
    /// Certifications (HACCP, ISO, VietGAP, etc.)
    /// </summary>
    public List<string>? Certifications { get; set; }
    
    /// <summary>
    /// Quality standards (Chỉ tiêu chất lượng - TCVN, QCVN, etc.)
    /// </summary>
    public List<string>? QualityStandards { get; set; }
    
    /// <summary>
    /// Product warnings and notes (Cảnh báo)
    /// </summary>
    public List<string>? Warnings { get; set; }
    
    /// <summary>
    /// Manufacturer/Distributor information (Nhà sản xuất/Phân phối)
    /// </summary>
    public ManufacturerInfoResult? ManufacturerInfo { get; set; }
    
    /// <summary>
    /// Product codes (SKU, Batch, MSKTVSTY)
    /// </summary>
    public ProductCodesResult? ProductCodes { get; set; }
    
    /// <summary>
    /// Fresh produce detections (for vegetables, fruits, meat, seafood)
    /// </summary>
    public List<FreshProduceDetection>? FreshProduceItems { get; set; }
    
    /// <summary>
    /// Overall confidence score
    /// </summary>
    public float Confidence { get; set; }
    
    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public float ProcessingTimeMs { get; set; }
}

public class ManufacturerInfoResult
{
    /// <summary>
    /// Manufacturer name (Nhà sản xuất)
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Distributor name (Nhà phân phối)
    /// </summary>
    public string? Distributor { get; set; }
    
    /// <summary>
    /// Address (Địa chỉ)
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// Contact information (Hotline, website, email)
    /// </summary>
    public List<string>? Contact { get; set; }
}

public class ProductCodesResult
{
    /// <summary>
    /// SKU/Product code
    /// </summary>
    public string? Sku { get; set; }
    
    /// <summary>
    /// Batch/Lot number (Số lô)
    /// </summary>
    public string? Batch { get; set; }
    
    /// <summary>
    /// Veterinary hygiene code (Mã số kinh tế vệ sinh thú y)
    /// </summary>
    public string? Msktvsty { get; set; }
}

public class VietnameseBarcodeInfo
{
    public string Barcode { get; set; } = string.Empty;
    public bool IsVietnamese { get; set; }
    public string? Company { get; set; }
    public string? Category { get; set; }
    public string? Prefix { get; set; }
    public string? Note { get; set; }
}

public class FreshProduceDetection
{
    public string Category { get; set; } = string.Empty;
    public string? NameVi { get; set; }
    public string? NameEn { get; set; }
    public int? TypicalShelfLifeDays { get; set; }
    public string? StorageRecommendation { get; set; }
    public List<string>? FreshnessIndicators { get; set; }
    public float Confidence { get; set; }
}

public class FreshProduceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<FreshProduceDetection> DetectedItems { get; set; } = new();
    public float ProcessingTimeMs { get; set; }
    public List<string>? Warnings { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

#endregion
