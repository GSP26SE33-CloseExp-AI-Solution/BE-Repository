using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

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

    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<AIHealthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AIHealthResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<AIHealthResponse>>> CheckHealth(CancellationToken cancellationToken)
    {
        var isAvailable = await _aiProductService.IsServiceAvailableAsync(cancellationToken);

        var response = new AIHealthResponse
        {
            Status = isAvailable ? "healthy" : "unhealthy",
            Timestamp = DateTime.UtcNow,
            Service = "AI Service"
        };

        return isAvailable
            ? Ok(ApiResponse<AIHealthResponse>.SuccessResponse(response))
            : StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<AIHealthResponse>.ErrorResponse("AI Service unhealthy"));
    }

    [HttpPost("extract")]
    [ProducesResponseType(typeof(ApiResponse<ProductExtractionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductExtractionResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductExtractionResult>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<ProductExtractionResult>>> ExtractProductInfo(
        [FromBody] ExtractProductRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ImageUrl) && string.IsNullOrEmpty(request.ImageBase64))
        {
            return BadRequest(ApiResponse<ProductExtractionResult>.ErrorResponse("Either ImageUrl or ImageBase64 must be provided"));
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
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<ProductExtractionResult>.ErrorResponse(result.ErrorMessage ?? "AI Service unavailable"));
        }

        return Ok(ApiResponse<ProductExtractionResult>.SuccessResponse(result));
    }

    [HttpPost("pricing")]
    [ProducesResponseType(typeof(ApiResponse<PricingSuggestionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PricingSuggestionResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PricingSuggestionResult>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PricingSuggestionResult>>> GetPricingSuggestion(
        [FromBody] PricingRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Category))
        {
            return BadRequest(ApiResponse<PricingSuggestionResult>.ErrorResponse("Category is required"));
        }

        if (request.OriginalPrice <= 0)
        {
            return BadRequest(ApiResponse<PricingSuggestionResult>.ErrorResponse("OriginalPrice must be greater than 0"));
        }

        if (request.ExpiryDate <= DateTime.UtcNow)
        {
            return BadRequest(ApiResponse<PricingSuggestionResult>.ErrorResponse("ExpiryDate must be in the future"));
        }

        var result = await _aiProductService.GetPriceSuggestionAsync(
            request.Category,
            request.ExpiryDate,
            request.OriginalPrice,
            request.Brand,
            cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<PricingSuggestionResult>.ErrorResponse(result.ErrorMessage ?? "AI Service unavailable"));
        }

        return Ok(ApiResponse<PricingSuggestionResult>.SuccessResponse(result));
    }

}
