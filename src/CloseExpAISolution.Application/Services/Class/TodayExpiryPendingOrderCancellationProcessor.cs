using CloseExpAISolution.Application.Policies;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class TodayExpiryPendingOrderCancellationProcessor : ITodayExpiryPendingOrderCancellationProcessor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProviders _services;
    private readonly ILogger<TodayExpiryPendingOrderCancellationProcessor> _logger;

    public TodayExpiryPendingOrderCancellationProcessor(
        IUnitOfWork unitOfWork,
        IServiceProviders services,
        ILogger<TodayExpiryPendingOrderCancellationProcessor> logger)
    {
        _unitOfWork = unitOfWork;
        _services = services;
        _logger = logger;
    }

    public async Task<(int ExpiredLots, int CanceledOrders)> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        if (!DailyExpiryOrderingPolicy.IsOrderCutoffReached(nowUtc))
            return (0, 0);

        var (todayStartUtc, todayEndUtc) = DailyExpiryOrderingPolicy.GetVietnamDateRangeUtc(nowUtc);

        var lotsToExpire = (await _unitOfWork.Repository<StockLot>().FindAsync(l =>
                l.Status == ProductState.Published
                && l.ExpiryDate >= todayStartUtc
                && l.ExpiryDate < todayEndUtc))
            .ToList();

        if (lotsToExpire.Count == 0)
            return (0, 0);

        foreach (var lot in lotsToExpire)
        {
            lot.Status = ProductState.Expired;
            lot.Quantity = 0;
            lot.UpdatedAt = nowUtc;
            _unitOfWork.Repository<StockLot>().Update(lot);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var expiredLotIds = lotsToExpire.Select(x => x.LotId).ToList();
        var pendingOrderIds = await _unitOfWork.Repository<Order>()
            .AsQueryable()
            .Where(o => o.Status == OrderState.Pending)
            .Where(o => _unitOfWork.Repository<OrderItem>()
                .AsQueryable()
                .Any(oi => oi.OrderId == o.OrderId && expiredLotIds.Contains(oi.LotId)))
            .Select(o => o.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var canceledOrders = 0;
        var cancellationNote = "Auto-canceled at 21:00 VN: contains lot expiring in current day.";
        foreach (var orderId in pendingOrderIds)
        {
            try
            {
                await _services.OrderService.UpdateStatusAsync(
                    orderId,
                    OrderState.Canceled,
                    cancellationNote,
                    cancellationToken);
                canceledOrders++;
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
            {
                _logger.LogWarning(
                    ex,
                    "Skip canceling order {OrderId} during today-expiry cleanup because order state changed.",
                    orderId);
            }
        }

        return (lotsToExpire.Count, canceledOrders);
    }
}
