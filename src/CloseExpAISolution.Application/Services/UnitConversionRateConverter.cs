using CloseExpAISolution.Application.Services.Interface;

namespace CloseExpAISolution.Application.Services;

public static class UnitConversionRateConverter
{
    private const decimal IntegerEpsilon = 0.0001m;

    public static decimal ConvertQuantity(
        Guid fromUnitId,
        Guid toUnitId,
        decimal quantity,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        var from = GetUnit(units, fromUnitId);
        var to = GetUnit(units, toUnitId);

        if (fromUnitId == toUnitId)
            return quantity;

        if (!string.Equals(from.Type, to.Type, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể quy đổi giữa đơn vị khác loại ({from.Type} -> {to.Type}).");
        }

        if (from.ConversionRate <= 0 || to.ConversionRate <= 0)
        {
            throw new InvalidOperationException("Hệ số quy đổi đơn vị phải lớn hơn 0.");
        }

        var qtyBase = quantity * from.ConversionRate;
        return qtyBase / to.ConversionRate;
    }

    public static decimal ConvertUnitPrice(
        Guid fromUnitId,
        Guid toUnitId,
        decimal unitPrice,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        if (fromUnitId == toUnitId)
            return unitPrice;

        var from = GetUnit(units, fromUnitId);
        var to = GetUnit(units, toUnitId);

        if (!string.Equals(from.Type, to.Type, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể quy đổi giá giữa đơn vị khác loại ({from.Type} -> {to.Type}).");
        }

        if (from.ConversionRate <= 0 || to.ConversionRate <= 0)
        {
            throw new InvalidOperationException("Hệ số quy đổi đơn vị phải lớn hơn 0.");
        }

        return unitPrice * (to.ConversionRate / from.ConversionRate);
    }

    public static short ConvertQuantityToShort(
        Guid fromUnitId,
        Guid toUnitId,
        decimal quantity,
        IReadOnlyDictionary<Guid, UnitConversionInfo> units)
    {
        var converted = ConvertQuantity(fromUnitId, toUnitId, quantity, units);
        if (!IsNearInteger(converted))
        {
            throw new InvalidOperationException(
                $"Số lượng sau quy đổi đơn vị phải là số nguyên (từ {fromUnitId} sang {toUnitId}, qty={quantity}).");
        }

        if (converted < short.MinValue || converted > short.MaxValue)
        {
            throw new InvalidOperationException(
                $"Số lượng sau quy đổi vượt giới hạn short ({converted}).");
        }

        return (short)Math.Round(converted, MidpointRounding.AwayFromZero);
    }

    private static UnitConversionInfo GetUnit(IReadOnlyDictionary<Guid, UnitConversionInfo> units, Guid unitId)
    {
        if (!units.TryGetValue(unitId, out var unit))
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn vị đo lường: {unitId}.");
        }

        return unit;
    }

    private static bool IsNearInteger(decimal value)
    {
        var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        return Math.Abs(value - rounded) <= IntegerEpsilon;
    }
}
