using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services;

public sealed class OrderStockQuantityHelper
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUnitConversionRateService _unitConversion;

    public OrderStockQuantityHelper(IUnitOfWork unitOfWork, IUnitConversionRateService unitConversion)
    {
        _unitOfWork = unitOfWork;
        _unitConversion = unitConversion;
    }

    public async Task<Dictionary<Guid, decimal>> ComputeRequiredStockQuantityByLotAsync(
        IEnumerable<OrderItem> orderItems,
        CancellationToken cancellationToken = default)
    {
        var items = orderItems.ToList();
        if (items.Count == 0)
            return new Dictionary<Guid, decimal>();

        var byLot = items.GroupBy(oi => oi.LotId).ToList();
        var lotIds = byLot.Select(g => g.Key).ToList();

        var lots = (await _unitOfWork.Repository<StockLot>()
            .FindAsync(l => lotIds.Contains(l.LotId)))
            .ToDictionary(l => l.LotId);

        var productIds = lots.Values.Select(l => l.ProductId).Distinct().ToList();
        var products = (await _unitOfWork.Repository<Product>()
            .FindAsync(p => productIds.Contains(p.ProductId)))
            .ToDictionary(p => p.ProductId);

        var unitIds = lots.Values.Select(l => l.UnitId)
            .Concat(products.Values.Select(p => p.UnitId))
            .Distinct();

        var units = await _unitConversion.LoadUnitInfoAsync(unitIds, cancellationToken);

        var result = new Dictionary<Guid, decimal>();
        foreach (var group in byLot)
        {
            if (!lots.TryGetValue(group.Key, out var lot))
                throw new InvalidOperationException($"Không tìm thấy StockLot {group.Key}.");

            if (!products.TryGetValue(lot.ProductId, out var product))
                throw new InvalidOperationException($"Không tìm thấy Product {lot.ProductId} cho StockLot {group.Key}.");

            var productQtySum = (decimal)group.Sum(x => x.Quantity);
            result[group.Key] = UnitConversionRateConverter.ConvertQuantity(
                product.UnitId,
                lot.UnitId,
                productQtySum,
                units);
        }

        return result;
    }
}
