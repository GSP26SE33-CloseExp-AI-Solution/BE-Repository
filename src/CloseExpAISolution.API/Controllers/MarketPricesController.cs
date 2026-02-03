using CloseExpAISolution.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

/// <summary>
/// Controller for market price operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MarketPricesController : ControllerBase
{
    private readonly IMarketPriceService _marketPriceService;
    private readonly ILogger<MarketPricesController> _logger;

    public MarketPricesController(
        IMarketPriceService marketPriceService,
        ILogger<MarketPricesController> logger)
    {
        _marketPriceService = marketPriceService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy giá thị trường theo barcode
    /// </summary>
    [HttpGet("{barcode}")]
    public async Task<ActionResult<MarketPriceResult>> GetByBarcode(
        string barcode,
        CancellationToken cancellationToken)
    {
        var result = await _marketPriceService.GetMarketPriceAsync(barcode, cancellationToken);
        
        if (result == null)
        {
            return NotFound(new { message = $"No market prices found for barcode: {barcode}" });
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Tìm kiếm giá thị trường theo tên sản phẩm
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<MarketPriceResult>> SearchByName(
        [FromQuery] string productName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return BadRequest(new { message = "Product name is required" });
        }

        var result = await _marketPriceService.SearchMarketPriceAsync(productName, cancellationToken);
        
        if (result == null)
        {
            return NotFound(new { message = $"No market prices found for product: {productName}" });
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Trigger crawl giá thị trường từ AI service
    /// </summary>
    [HttpPost("crawl")]
    public async Task<ActionResult<CrawlResult>> TriggerCrawl(
        [FromBody] CrawlRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return BadRequest(new { message = "Barcode is required" });
        }

        var result = await _marketPriceService.TriggerCrawlAsync(
            request.Barcode,
            request.ProductName,
            cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Lưu giá từ nhân viên (crowdsource)
    /// </summary>
    [HttpPost("crowdsource")]
    public async Task<ActionResult> SaveCrowdsourcePrice(
        [FromBody] CrowdsourcePriceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return BadRequest(new { message = "Barcode is required" });
        }

        if (request.Price <= 0)
        {
            return BadRequest(new { message = "Price must be greater than 0" });
        }

        var result = await _marketPriceService.SaveCrowdsourcePriceAsync(request, cancellationToken);
        
        return CreatedAtAction(
            nameof(GetByBarcode),
            new { barcode = result.Barcode },
            new { message = "Price saved successfully", marketPriceId = result.MarketPriceId });
    }

    /// <summary>
    /// Lưu feedback từ nhân viên về giá AI đề xuất
    /// </summary>
    [HttpPost("feedback")]
    public async Task<ActionResult> SavePriceFeedback(
        [FromBody] PriceFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return BadRequest(new { message = "Barcode is required" });
        }

        var result = await _marketPriceService.SavePriceFeedbackAsync(request, cancellationToken);
        
        return Ok(new 
        { 
            message = "Feedback saved successfully", 
            feedbackId = result.Id,
            wasAccepted = result.WasAccepted
        });
    }

    /// <summary>
    /// Lấy thống kê độ chính xác của AI theo category
    /// </summary>
    [HttpGet("accuracy/by-category")]
    public async Task<ActionResult<Dictionary<string, float>>> GetAIAccuracyByCategory(
        CancellationToken cancellationToken)
    {
        var result = await _marketPriceService.GetAIAccuracyByCategoryAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xóa giá cũ (admin only)
    /// </summary>
    [HttpDelete("cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CleanupExpiredPrices(
        [FromQuery] int daysOld = 30,
        CancellationToken cancellationToken = default)
    {
        var deletedCount = await _marketPriceService.CleanupExpiredPricesAsync(daysOld, cancellationToken);
        return Ok(new { message = $"Deleted {deletedCount} expired prices" });
    }
}

/// <summary>
/// Request for triggering price crawl
/// </summary>
public class CrawlRequest
{
    public string Barcode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
}
