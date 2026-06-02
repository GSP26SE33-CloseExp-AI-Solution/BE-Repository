using CloseExpAISolution.Application.Services.Interface;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Email.Jobs;

[DisallowConcurrentExecution]
public class ExpirePastDueStockLotsJob : IJob
{
    private readonly IPastDueStockLotExpirationProcessor _processor;
    private readonly ILogger<ExpirePastDueStockLotsJob> _logger;

    public ExpirePastDueStockLotsJob(
        IPastDueStockLotExpirationProcessor processor,
        ILogger<ExpirePastDueStockLotsJob> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var count = await _processor.ProcessAsync(context.CancellationToken);
            if (count > 0)
            {
                _logger.LogInformation(
                    "ExpirePastDueStockLotsJob expired {Count} lot(s).",
                    count);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExpirePastDueStockLotsJob failed.");
        }
    }
}
