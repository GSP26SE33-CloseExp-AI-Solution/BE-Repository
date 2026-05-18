using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services;

public sealed class OrderItemUnitConverter
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUnitConversionRateService _unitConversion;

    public OrderItemUnitConverter(IUnitOfWork unitOfWork, IUnitConversionRateService unitConversion)
    {
        _unitOfWork = unitOfWork;
        _unitConversion = unitConversion;
    }

    public async Task<IReadOnlyList<ConvertedOrderLine>> ConvertCreateItemsToProductUnitAsync(
        IReadOnlyCollection<CreateOrderItemDto> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return Array.Empty<ConvertedOrderLine>();

        var lotIds = items.Select(x => x.LotId).Distinct().ToList();
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
        var result = new List<ConvertedOrderLine>(items.Count);

        foreach (var item in items)
        {
            if (!lots.TryGetValue(item.LotId, out var lot))
                throw new InvalidOperationException($"Không tìm thấy StockLot {item.LotId}.");

            if (!products.TryGetValue(lot.ProductId, out var product))
                throw new InvalidOperationException($"Không tìm thấy Product {lot.ProductId} cho StockLot {item.LotId}.");

            var qtyProduct = UnitConversionRateConverter.ConvertQuantityToShort(
                lot.UnitId,
                product.UnitId,
                item.Quantity,
                units);

            var unitPriceProduct = UnitConversionRateConverter.ConvertUnitPrice(
                lot.UnitId,
                product.UnitId,
                item.UnitPrice,
                units);

            result.Add(new ConvertedOrderLine(item.LotId, qtyProduct, unitPriceProduct));
        }

        return result;
    }
}

public sealed record ConvertedOrderLine(Guid LotId, short Quantity, decimal UnitPrice);
