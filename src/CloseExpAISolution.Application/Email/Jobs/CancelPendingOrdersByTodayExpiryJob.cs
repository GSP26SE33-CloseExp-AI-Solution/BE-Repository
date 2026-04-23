using CloseExpAISolution.Application.Services.Interface;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Email.Jobs;

[DisallowConcurrentExecution]
public class CancelPendingOrdersByTodayExpiryJob : IJob
{
    private readonly ITodayExpiryPendingOrderCancellationProcessor _processor;
    private readonly ILogger<CancelPendingOrdersByTodayExpiryJob> _logger;

    public CancelPendingOrdersByTodayExpiryJob(
        ITodayExpiryPendingOrderCancellationProcessor processor,
        ILogger<CancelPendingOrdersByTodayExpiryJob> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startedAt = DateTime.UtcNow;
        try
        {
            var (expiredLots, canceledOrders) = await _processor.ProcessAsync(context.CancellationToken);
            var durationMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
            _logger.LogInformation(
                "CancelPendingOrdersByTodayExpiryJob completed. expiredLots={ExpiredLots}, canceledOrders={CanceledOrders}, durationMs={DurationMs}",
                expiredLots,
                canceledOrders,
                durationMs);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelPendingOrdersByTodayExpiryJob failed.");
        }
    }
}
