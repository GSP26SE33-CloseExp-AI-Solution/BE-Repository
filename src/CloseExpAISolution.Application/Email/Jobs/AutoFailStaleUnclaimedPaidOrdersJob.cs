using CloseExpAISolution.Application.Services.Interface;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Email.Jobs;

[DisallowConcurrentExecution]
public sealed class AutoFailStaleUnclaimedPaidOrdersJob : IJob
{
    private readonly IStalePaidUnclaimedPackagingProcessor _processor;
    private readonly ILogger<AutoFailStaleUnclaimedPaidOrdersJob> _logger;

    public AutoFailStaleUnclaimedPaidOrdersJob(
        IStalePaidUnclaimedPackagingProcessor processor,
        ILogger<AutoFailStaleUnclaimedPaidOrdersJob> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startedAt = DateTime.UtcNow;
        try
        {
            var failedOrders = await _processor.ProcessAsync(context.CancellationToken);
            var durationMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
            _logger.LogInformation(
                "AutoFailStaleUnclaimedPaidOrdersJob completed. failedOrders={FailedOrders}, durationMs={DurationMs}",
                failedOrders,
                durationMs);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoFailStaleUnclaimedPaidOrdersJob failed.");
        }
    }
}
