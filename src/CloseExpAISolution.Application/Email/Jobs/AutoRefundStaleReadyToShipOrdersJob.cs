using CloseExpAISolution.Application.Services.Interface;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Email.Jobs;

[DisallowConcurrentExecution]
public class AutoRefundStaleReadyToShipOrdersJob : IJob
{
    private readonly IStaleReadyToShipRefundProcessor _processor;
    private readonly ILogger<AutoRefundStaleReadyToShipOrdersJob> _logger;

    public AutoRefundStaleReadyToShipOrdersJob(
        IStaleReadyToShipRefundProcessor processor,
        ILogger<AutoRefundStaleReadyToShipOrdersJob> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startedAt = DateTime.UtcNow;
        try
        {
            var refundedOrders = await _processor.ProcessAsync(context.CancellationToken);
            var durationMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
            _logger.LogInformation(
                "AutoRefundStaleReadyToShipOrdersJob completed. refundedOrders={RefundedOrders}, durationMs={DurationMs}",
                refundedOrders,
                durationMs);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoRefundStaleReadyToShipOrdersJob failed.");
        }
    }
}
