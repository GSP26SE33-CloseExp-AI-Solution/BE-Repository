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

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

#endregion
