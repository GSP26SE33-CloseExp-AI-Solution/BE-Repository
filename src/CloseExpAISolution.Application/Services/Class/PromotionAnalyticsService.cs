using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class PromotionAnalyticsService : IPromotionAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;

    public PromotionAnalyticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PromotionAnalyticsOverviewDto> GetOverviewAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var usages = await _unitOfWork.Repository<PromotionUsage>().GetAllAsync();
        var orders = await _unitOfWork.Repository<Order>().GetAllAsync();

        var filteredUsages = usages.Where(x =>
            (!fromUtc.HasValue || x.UsedAt >= fromUtc.Value) &&
            (!toUtc.HasValue || x.UsedAt <= toUtc.Value))
            .ToList();

        var orderMap = orders.ToDictionary(o => o.OrderId, o => o);
        var linkedOrders = filteredUsages
            .Where(x => orderMap.ContainsKey(x.OrderId))
            .Select(x => orderMap[x.OrderId])
            .ToList();

        var totalDiscount = filteredUsages.Sum(x => x.DiscountAmount);
        var usageCount = filteredUsages.Count;

        return new PromotionAnalyticsOverviewDto
        {
            TotalPromotionUsages = usageCount,
            UniqueUsers = filteredUsages.Select(x => x.UserId).Distinct().Count(),
            TotalDiscountAmount = totalDiscount,
            GrossRevenueAffected = linkedOrders.Sum(x => x.TotalAmount),
            NetRevenueAffected = linkedOrders.Sum(x => x.FinalAmount),
            AvgDiscountPerUsage = usageCount == 0 ? 0 : Math.Round(totalDiscount / usageCount, 2)
        };
    }

    public async Task<IEnumerable<AdminPromotionDto>> GetTopPromotionsAsync(string metric, int limit, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 50);
        var usages = await _unitOfWork.Repository<PromotionUsage>().GetAllAsync();
        var promotions = await _unitOfWork.Repository<Promotion>().GetAllAsync();
        var orders = await _unitOfWork.Repository<Order>().GetAllAsync();

        var filteredUsages = usages.Where(x =>
            (!fromUtc.HasValue || x.UsedAt >= fromUtc.Value) &&
            (!toUtc.HasValue || x.UsedAt <= toUtc.Value));
        var usageList = filteredUsages.ToList();

        var usageByPromotion = usageList.GroupBy(x => x.PromotionId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var orderMap = orders.ToDictionary(o => o.OrderId, o => o);

        Func<Promotion, decimal> metricSelector = metric.ToLowerInvariant() switch
        {
            "discount" => p => usageByPromotion.TryGetValue(p.PromotionId, out var items)
                ? items.Sum(x => x.DiscountAmount)
                : 0m,
            "revenue" => p => usageByPromotion.TryGetValue(p.PromotionId, out var items)
                ? items.Where(x => orderMap.ContainsKey(x.OrderId)).Sum(x => orderMap[x.OrderId].FinalAmount)
                : 0m,
            _ => p => usageByPromotion.TryGetValue(p.PromotionId, out var items) ? items.Count : 0
        };

        return promotions
            .OrderByDescending(metricSelector)
            .Take(safeLimit)
            .Select(PromotionService.MapPromotion)
            .ToList();
    }

    public async Task<IEnumerable<PromotionTrendPointDto>> GetPromotionTrendAsync(Guid promotionId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;
        var start = fromUtc?.Date ?? now.AddDays(-29);
        var end = toUtc?.Date ?? now;
        if (end < start)
            (start, end) = (end, start);

        var usages = await _unitOfWork.Repository<PromotionUsage>()
            .FindAsync(x => x.PromotionId == promotionId && x.UsedAt.Date >= start && x.UsedAt.Date <= end);
        var orders = await _unitOfWork.Repository<Order>().GetAllAsync();
        var orderMap = orders.ToDictionary(o => o.OrderId, o => o.FinalAmount);

        var grouped = usages
            .GroupBy(x => x.UsedAt.Date)
            .ToDictionary(
                g => g.Key,
                g => new PromotionTrendPointDto
                {
                    Date = g.Key,
                    UsageCount = g.Count(),
                    DiscountAmount = g.Sum(x => x.DiscountAmount),
                    NetRevenueAffected = g.Where(x => orderMap.ContainsKey(x.OrderId)).Sum(x => orderMap[x.OrderId])
                });

        var results = new List<PromotionTrendPointDto>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            results.Add(grouped.TryGetValue(date, out var point)
                ? point
                : new PromotionTrendPointDto
                {
                    Date = date,
                    UsageCount = 0,
                    DiscountAmount = 0,
                    NetRevenueAffected = 0
                });
        }

        return results;
    }
}
