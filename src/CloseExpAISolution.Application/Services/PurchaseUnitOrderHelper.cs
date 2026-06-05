using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services;

public sealed class PurchaseUnitOrderHelper
{
    private readonly IUnitConversionRateService _unitConversion;

    public PurchaseUnitOrderHelper(IUnitConversionRateService unitConversion)
    {
        _unitConversion = unitConversion;
    }

    public static Guid ResolvePurchaseUnitId(Guid? requested, StockLot lot) =>
        requested is { } id && id != Guid.Empty ? id : lot.UnitId;

    public async Task<IReadOnlyList<Guid>> GetAllowedPurchaseUnitIdsAsync(
        Product product,
        IReadOnlyCollection<StockLot> publishedLots,
        CancellationToken cancellationToken = default)
    {
        var lotUnitIds = publishedLots.Select(l => l.UnitId).Distinct().ToList();
        if (lotUnitIds.Count == 0)
            return Array.Empty<Guid>();

        var bootstrapUnits = await _unitConversion.LoadUnitInfoAsync(
            lotUnitIds.Append(product.UnitId),
            cancellationToken);

        if (!bootstrapUnits.TryGetValue(product.UnitId, out var baseUnit))
        {
            return lotUnitIds.Where(bootstrapUnits.ContainsKey).ToList();
        }

        var sameTypeUnitIds = await _unitConversion.GetUnitIdsByTypeAsync(
            baseUnit.Type,
            cancellationToken);

        var units = await _unitConversion.LoadUnitInfoAsync(
            sameTypeUnitIds.Concat(lotUnitIds).Append(product.UnitId).Distinct(),
            cancellationToken);

        return sameTypeUnitIds
            .Where(units.ContainsKey)
            .Where(id => lotUnitIds.Any(lotUnitId => CanConvertBetweenUnits(id, lotUnitId, units)))
            .Distinct()
            .ToList();
    }

    private static bool CanConvertBetweenUnits(
        Guid fromUnitId,
        Guid toUnitId,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        if (fromUnitId == toUnitId)
            return true;

        try
        {
            _ = UnitConversionRateConverter.ConvertQuantity(fromUnitId, toUnitId, 1m, units);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void EnsurePurchaseUnitAllowed(
        Guid purchaseUnitId,
        Product product,
        StockLot lot,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        if (!units.TryGetValue(purchaseUnitId, out var purchaseUnit))
            throw new InvalidOperationException($"Không tìm thấy đơn vị mua: {purchaseUnitId}.");

        if (!units.TryGetValue(lot.UnitId, out var lotUnit))
            throw new InvalidOperationException($"Không tìm thấy đơn vị lô: {lot.UnitId}.");

        if (!units.TryGetValue(product.UnitId, out var productUnit))
            throw new InvalidOperationException($"Không tìm thấy đơn vị sản phẩm: {product.UnitId}.");

        if (!UnitMeasureTypeCompatibility.AreCompatible(purchaseUnit.Type, productUnit.Type))
        {
            throw new InvalidOperationException(
                "Đơn vị mua phải cùng loại với đơn vị chuẩn sản phẩm.");
        }

        if (!UnitMeasureTypeCompatibility.AreCompatible(purchaseUnit.Type, lotUnit.Type))
        {
            throw new InvalidOperationException(
                "Đơn vị mua phải cùng loại với đơn vị lô hàng.");
        }

        if (purchaseUnitId != lot.UnitId)
        {
            try
            {
                UnitConversionRateConverter.ConvertQuantity(
                    purchaseUnitId,
                    lot.UnitId,
                    1m,
                    units);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Không thể quy đổi đơn vị mua sang đơn vị tồn kho của lô hàng.",
                    ex);
            }
        }
    }

    public decimal ConvertPurchaseQuantityToLotQuantity(
        Guid purchaseUnitId,
        Guid lotUnitId,
        decimal purchaseQuantity,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        if (purchaseQuantity <= 0)
            throw new ArgumentException("Số lượng phải lớn hơn 0.");

        return UnitConversionRateConverter.ConvertQuantity(
            purchaseUnitId,
            lotUnitId,
            purchaseQuantity,
            units);
    }

    public decimal ConvertPurchaseUnitPriceToLotUnitPrice(
        Guid purchaseUnitId,
        Guid lotUnitId,
        decimal purchaseUnitPrice,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        if (purchaseUnitPrice < 0)
            throw new ArgumentException("Đơn giá không hợp lệ.");

        return UnitConversionRateConverter.ConvertUnitPrice(
            purchaseUnitId,
            lotUnitId,
            purchaseUnitPrice,
            units);
    }

    public (short QuantityProduct, decimal UnitPriceProduct, Guid PurchaseUnitId) ConvertLineForOrder(
        CreateOrderItemDto item,
        StockLot lot,
        Product product,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        var purchaseUnitId = ResolvePurchaseUnitId(item.PurchaseUnitId, lot);
        EnsurePurchaseUnitAllowed(purchaseUnitId, product, lot, units);

        if (item.Quantity <= 0 || item.Quantity > short.MaxValue)
            throw new InvalidOperationException("Số lượng đặt hàng không hợp lệ.");

        _ = ConvertPurchaseQuantityToLotQuantity(
            purchaseUnitId,
            lot.UnitId,
            item.Quantity,
            units);

        return ((short)item.Quantity, item.UnitPrice, purchaseUnitId);
    }

    public decimal? ResolveDisplayPurchaseQuantity(
        short quantity,
        Guid productUnitId,
        Guid? purchaseUnitId,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        if (!purchaseUnitId.HasValue || purchaseUnitId.Value == Guid.Empty)
            return quantity;

        if (purchaseUnitId.Value != productUnitId)
            return quantity;

        return TryConvertProductQuantityToPurchaseUnit(
            quantity,
            productUnitId,
            purchaseUnitId,
            units);
    }

    public async Task<Dictionary<Guid, decimal>> SumRequiredQuantitiesInLotUnitAsync(
        IReadOnlyCollection<CreateOrderItemDto> items,
        IReadOnlyDictionary<Guid, StockLot> lots,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return new Dictionary<Guid, decimal>();

        var unitIds = new HashSet<Guid>();
        foreach (var lot in lots.Values)
            unitIds.Add(lot.UnitId);

        foreach (var item in items)
        {
            if (!lots.TryGetValue(item.LotId, out var lot))
                continue;
            unitIds.Add(ResolvePurchaseUnitId(item.PurchaseUnitId, lot));
        }

        var units = await _unitConversion.LoadUnitInfoAsync(unitIds, cancellationToken);
        var requiredByLot = new Dictionary<Guid, decimal>();

        foreach (var item in items)
        {
            if (!lots.TryGetValue(item.LotId, out var lot))
                throw new InvalidOperationException($"Không tìm thấy StockLot {item.LotId}.");

            var purchaseUnitId = ResolvePurchaseUnitId(item.PurchaseUnitId, lot);
            var qtyLot = UnitConversionRateConverter.ConvertQuantity(
                purchaseUnitId,
                lot.UnitId,
                item.Quantity,
                units);

            requiredByLot[lot.LotId] = requiredByLot.GetValueOrDefault(lot.LotId) + qtyLot;
        }

        return requiredByLot;
    }

    public decimal? TryConvertProductQuantityToPurchaseUnit(
        short quantityInProductUnit,
        Guid productUnitId,
        Guid? purchaseUnitId,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        if (!purchaseUnitId.HasValue || purchaseUnitId.Value == productUnitId)
            return quantityInProductUnit;

        if (!units.ContainsKey(productUnitId) || !units.ContainsKey(purchaseUnitId.Value))
            return null;

        try
        {
            return UnitConversionRateConverter.ConvertQuantity(
                productUnitId,
                purchaseUnitId.Value,
                quantityInProductUnit,
                units);
        }
        catch
        {
            return null;
        }
    }

    public void ValidateCartQuantityAgainstLot(
        StockLot lot,
        Guid purchaseUnitId,
        decimal newTotalInPurchaseUnit,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        var qtyLot = ConvertPurchaseQuantityToLotQuantity(
            purchaseUnitId,
            lot.UnitId,
            newTotalInPurchaseUnit,
            units);

        if (qtyLot > lot.Quantity)
        {
            throw new InvalidOperationException(
                $"Số lượng vượt tồn kho của lô. Tối đa {lot.Quantity} (đơn vị lô).");
        }
    }
}
