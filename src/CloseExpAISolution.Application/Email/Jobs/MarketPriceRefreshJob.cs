using CloseExpAISolution.Application.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Email.Jobs;

[DisallowConcurrentExecution]
public class MarketPriceRefreshJob : IJob
{
    private readonly IMarketPriceService _marketPriceService;
    private readonly ILogger<MarketPriceRefreshJob> _logger;

    public MarketPriceRefreshJob(IMarketPriceService marketPriceService, ILogger<MarketPriceRefreshJob> logger)
    {
        _marketPriceService = marketPriceService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startedAt = DateTime.UtcNow;
        try
        {
            var refreshed = await _marketPriceService.RefreshPublishedProductsAsync(
                concurrency: 4,
                context.CancellationToken);
            _logger.LogInformation(
                "MarketPriceRefreshJob completed. refreshed={Refreshed}, durationMs={DurationMs}",
                refreshed,
                (DateTime.UtcNow - startedAt).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarketPriceRefreshJob failed after {DurationMs}ms", (DateTime.UtcNow - startedAt).TotalMilliseconds);
        }
    }
}
