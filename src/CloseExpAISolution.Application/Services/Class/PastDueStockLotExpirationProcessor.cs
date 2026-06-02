using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class PastDueStockLotExpirationProcessor : IPastDueStockLotExpirationProcessor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PastDueStockLotExpirationProcessor> _logger;

    public PastDueStockLotExpirationProcessor(
        IUnitOfWork unitOfWork,
        ILogger<PastDueStockLotExpirationProcessor> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var lotsToExpire = (await _unitOfWork.Repository<StockLot>().FindAsync(l =>
                l.Status == ProductState.Published
                && l.ExpiryDate <= nowUtc))
            .ToList();

        if (lotsToExpire.Count == 0)
            return 0;

        foreach (var lot in lotsToExpire)
        {
            lot.Status = ProductState.Expired;
            lot.Quantity = 0;
            lot.UpdatedAt = nowUtc;
            _unitOfWork.Repository<StockLot>().Update(lot);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Expired {Count} published stock lot(s) that were past ExpiryDate.",
            lotsToExpire.Count);

        return lotsToExpire.Count;
    }
}
