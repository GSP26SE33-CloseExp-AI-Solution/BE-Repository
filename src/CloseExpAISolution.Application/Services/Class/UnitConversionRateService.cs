using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public sealed class UnitConversionRateService : IUnitConversionRateService
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitConversionRateService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Dictionary<Guid, UnitConversionInfo>> LoadUnitInfoAsync(
        IEnumerable<Guid> unitIds,
        CancellationToken cancellationToken = default)
    {
        var ids = unitIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, UnitConversionInfo>();

        var units = await _unitOfWork.Repository<UnitOfMeasure>()
            .FindAsync(u => ids.Contains(u.UnitId));

        return units.ToDictionary(
            u => u.UnitId,
            u => new UnitConversionInfo(u.UnitId, u.Type, u.ConversionRate));
    }

    public async Task<IReadOnlyList<Guid>> GetUnitIdsByTypeAsync(
        string unitType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(unitType))
            return Array.Empty<Guid>();

        var normalized = unitType.Trim();
        var units = await _unitOfWork.Repository<UnitOfMeasure>().FindAsync(_ => true);
        return units
            .Where(u => string.Equals(u.Type, normalized, StringComparison.OrdinalIgnoreCase))
            .Select(u => u.UnitId)
            .ToList();
    }

    public decimal ConvertQuantity(Guid fromUnitId, Guid toUnitId, decimal quantity)
    {
        var units = LoadUnitInfoSync(fromUnitId, toUnitId);
        return UnitConversionRateConverter.ConvertQuantity(fromUnitId, toUnitId, quantity, units);
    }

    public decimal ConvertUnitPrice(Guid fromUnitId, Guid toUnitId, decimal unitPrice)
    {
        var units = LoadUnitInfoSync(fromUnitId, toUnitId);
        return UnitConversionRateConverter.ConvertUnitPrice(fromUnitId, toUnitId, unitPrice, units);
    }

    public short ConvertQuantityToShort(Guid fromUnitId, Guid toUnitId, decimal quantity)
    {
        var units = LoadUnitInfoSync(fromUnitId, toUnitId);
        return UnitConversionRateConverter.ConvertQuantityToShort(fromUnitId, toUnitId, quantity, units);
    }

    private Dictionary<Guid, UnitConversionInfo> LoadUnitInfoSync(Guid fromUnitId, Guid toUnitId)
    {
        var repo = _unitOfWork.Repository<UnitOfMeasure>();
        var from = repo.FirstOrDefaultAsync(u => u.UnitId == fromUnitId).GetAwaiter().GetResult()
            ?? throw new KeyNotFoundException($"Không tìm thấy đơn vị đo lường: {fromUnitId}.");
        var to = repo.FirstOrDefaultAsync(u => u.UnitId == toUnitId).GetAwaiter().GetResult()
            ?? throw new KeyNotFoundException($"Không tìm thấy đơn vị đo lường: {toUnitId}.");

        return new Dictionary<Guid, UnitConversionInfo>
        {
            [from.UnitId] = new UnitConversionInfo(from.UnitId, from.Type, from.ConversionRate),
            [to.UnitId] = new UnitConversionInfo(to.UnitId, to.Type, to.ConversionRate)
        };
    }
}
