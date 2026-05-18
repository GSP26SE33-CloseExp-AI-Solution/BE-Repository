namespace CloseExpAISolution.Application.Services.Interface;

public sealed class StockLotUnitCleanupResult
{
    public int DeletedCount { get; init; }
    public int ArchivedCount { get; init; }
    public int TotalIncompatible { get; init; }
}

public interface IStockLotUnitCompatibilityService
{
    Task<StockLotUnitCleanupResult> RemoveIncompatibleStockLotsAsync(
        CancellationToken cancellationToken = default);
}
