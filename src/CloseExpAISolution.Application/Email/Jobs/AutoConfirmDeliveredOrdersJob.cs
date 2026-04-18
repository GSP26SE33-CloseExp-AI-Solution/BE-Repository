using CloseExpAISolution.Application.Services.Interface;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Email.Jobs;

[DisallowConcurrentExecution]
public class AutoConfirmDeliveredOrdersJob : IJob
{
   private readonly IDeliveryService _deliveryService;
   private readonly ILogger<AutoConfirmDeliveredOrdersJob> _logger;

   public AutoConfirmDeliveredOrdersJob(
       IDeliveryService deliveryService,
       ILogger<AutoConfirmDeliveredOrdersJob> logger)
   {
      _deliveryService = deliveryService;
      _logger = logger;
   }

   public async Task Execute(IJobExecutionContext context)
   {
      var startedAt = DateTime.UtcNow;

      try
      {
         var affectedOrders = await _deliveryService.AutoConfirmDeliveredOrdersAsync(context.CancellationToken);
         var durationMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;

         _logger.LogInformation(
             "AutoConfirmDeliveredOrdersJob completed. affectedOrders={AffectedOrders}, durationMs={DurationMs}",
             affectedOrders,
             durationMs);
      }
      catch (OperationCanceledException)
      {
         throw;
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "AutoConfirmDeliveredOrdersJob failed.");
      }
   }
}
