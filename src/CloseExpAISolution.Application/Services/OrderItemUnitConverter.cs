using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services;

public sealed class OrderItemUnitConverter
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUnitConversionRateService _unitConversion;
    private readonly PurchaseUnitOrderHelper _purchaseUnitHelper;

    public OrderItemUnitConverter(
        IUnitOfWork unitOfWork,
        IUnitConversionRateService unitConversion,
        PurchaseUnitOrderHelper purchaseUnitHelper)
    {
        _unitOfWork = unitOfWork;
        _unitConversion = unitConversion;
        _purchaseUnitHelper = purchaseUnitHelper;
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

        var now = DateTime.UtcNow;
        var publishedLotsByProduct = (await _unitOfWork.Repository<StockLot>()
            .FindAsync(l =>
                productIds.Contains(l.ProductId)
                && l.Status == ProductState.Published
                && l.Quantity > 0
                && l.ExpiryDate > now))
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<StockLot>)g.ToList());

        var unitIds = lots.Values.Select(l => l.UnitId)
            .Concat(products.Values.Select(p => p.UnitId))
            .Concat(items.Where(i => i.PurchaseUnitId.HasValue).Select(i => i.PurchaseUnitId!.Value))
            .Distinct();

        var units = await _unitConversion.LoadUnitInfoAsync(unitIds, cancellationToken);
        var result = new List<ConvertedOrderLine>(items.Count);

        foreach (var item in items)
        {
            if (!lots.TryGetValue(item.LotId, out var lot))
                throw new InvalidOperationException($"Không tìm thấy StockLot {item.LotId}.");

            if (!products.TryGetValue(lot.ProductId, out var product))
                throw new InvalidOperationException($"Không tìm thấy Product {lot.ProductId} cho StockLot {item.LotId}.");

            publishedLotsByProduct.TryGetValue(product.ProductId, out var publishedLots);
            publishedLots ??= Array.Empty<StockLot>();

            var allowedUnitIds = await _purchaseUnitHelper.GetAllowedPurchaseUnitIdsAsync(
                product,
                publishedLots,
                cancellationToken);

            var (qtyProduct, unitPriceProduct, purchaseUnitId) = _purchaseUnitHelper.ConvertLineForOrder(
                item,
                lot,
                product,
                units,
                allowedUnitIds);

            result.Add(new ConvertedOrderLine(item.LotId, qtyProduct, unitPriceProduct, purchaseUnitId));
        }

        return result;
    }

    public async Task<ConvertedOrderLine> ConvertUpdateItemToProductUnitAsync(
        OrderItem existing,
        UpdateOrderItemRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var lotId = request.LotId ?? existing.LotId;
        var lot = await _unitOfWork.Repository<StockLot>().FirstOrDefaultAsync(l => l.LotId == lotId)
            ?? throw new InvalidOperationException($"Không tìm thấy StockLot {lotId}.");

        var product = await _unitOfWork.Repository<Product>().FirstOrDefaultAsync(p => p.ProductId == lot.ProductId)
            ?? throw new InvalidOperationException($"Không tìm thấy Product {lot.ProductId} cho StockLot {lotId}.");

        var now = DateTime.UtcNow;
        var publishedLots = (await _unitOfWork.Repository<StockLot>()
            .FindAsync(l =>
                l.ProductId == product.ProductId
                && l.Status == ProductState.Published
                && l.Quantity > 0
                && l.ExpiryDate > now))
            .ToList();

        var allowedUnitIds = await _purchaseUnitHelper.GetAllowedPurchaseUnitIdsAsync(
            product,
            publishedLots,
            cancellationToken);

        var purchaseUnitId = request.PurchaseUnitId ?? existing.PurchaseUnitId ?? lot.UnitId;

        var unitIds = new HashSet<Guid> { lot.UnitId, product.UnitId, purchaseUnitId };
        if (existing.PurchaseUnitId.HasValue)
            unitIds.Add(existing.PurchaseUnitId.Value);

        var units = await _unitConversion.LoadUnitInfoAsync(unitIds, cancellationToken);

        int purchaseQuantity;
        decimal purchaseUnitPrice;

        if (request.Quantity.HasValue)
        {
            purchaseQuantity = request.Quantity.Value;
        }
        else
        {
            var fromProductUnit = existing.PurchaseUnitId ?? product.UnitId;
            purchaseQuantity = (int)Math.Max(
                1,
                Math.Round(
                    _purchaseUnitHelper.TryConvertProductQuantityToPurchaseUnit(
                        existing.Quantity,
                        product.UnitId,
                        purchaseUnitId,
                        units) ?? existing.Quantity));
        }

        if (request.UnitPrice.HasValue)
        {
            purchaseUnitPrice = request.UnitPrice.Value;
        }
        else
        {
            var priceProductUnit = existing.UnitPrice;
            purchaseUnitPrice = UnitConversionRateConverter.ConvertUnitPrice(
                product.UnitId,
                purchaseUnitId,
                priceProductUnit,
                units);
        }

        var createLine = new CreateOrderItemDto
        {
            LotId = lotId,
            PurchaseUnitId = purchaseUnitId,
            Quantity = purchaseQuantity,
            UnitPrice = purchaseUnitPrice
        };

        var (qtyProduct, unitPriceProduct, resolvedPurchaseUnitId) = _purchaseUnitHelper.ConvertLineForOrder(
            createLine,
            lot,
            product,
            units,
            allowedUnitIds);

        return new ConvertedOrderLine(lotId, qtyProduct, unitPriceProduct, resolvedPurchaseUnitId);
    }
}

public sealed record ConvertedOrderLine(
    Guid LotId,
    short Quantity,
    decimal UnitPrice,
    Guid PurchaseUnitId);
