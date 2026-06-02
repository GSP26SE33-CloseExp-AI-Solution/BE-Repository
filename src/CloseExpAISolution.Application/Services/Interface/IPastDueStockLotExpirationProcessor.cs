namespace CloseExpAISolution.Application.Services.Interface;

public interface IPastDueStockLotExpirationProcessor
{
    Task<int> ProcessAsync(CancellationToken cancellationToken = default);
}
