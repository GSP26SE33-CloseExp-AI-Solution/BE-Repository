using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

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
