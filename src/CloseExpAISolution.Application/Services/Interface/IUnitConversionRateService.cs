namespace CloseExpAISolution.Application.Services.Interface;

public interface IUnitConversionRateService
{
    decimal ConvertQuantity(Guid fromUnitId, Guid toUnitId, decimal quantity);

    decimal ConvertUnitPrice(Guid fromUnitId, Guid toUnitId, decimal unitPrice);

    short ConvertQuantityToShort(Guid fromUnitId, Guid toUnitId, decimal quantity);

    Task<Dictionary<Guid, UnitConversionInfo>> LoadUnitInfoAsync(
        IEnumerable<Guid> unitIds,
        CancellationToken cancellationToken = default);
}

public sealed record UnitConversionInfo(Guid UnitId, string Type, decimal ConversionRate);
