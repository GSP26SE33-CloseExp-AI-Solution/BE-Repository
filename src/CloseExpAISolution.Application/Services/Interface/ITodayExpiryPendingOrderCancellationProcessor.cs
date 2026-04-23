namespace CloseExpAISolution.Application.Services.Interface;

public interface ITodayExpiryPendingOrderCancellationProcessor
{
    Task<(int ExpiredLots, int CanceledOrders)> ProcessAsync(CancellationToken cancellationToken = default);
}
