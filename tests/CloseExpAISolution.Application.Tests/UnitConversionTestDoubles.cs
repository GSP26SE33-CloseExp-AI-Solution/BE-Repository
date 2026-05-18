using CloseExpAISolution.Application.Services.Interface;
using Moq;

namespace CloseExpAISolution.Application.Tests;

internal static class UnitConversionTestDoubles
{
    /// <summary>
    /// Supports OrderService tests using mocked <see cref="CloseExpAISolution.Infrastructure.UnitOfWork.IUnitOfWork"/> where unit conversion is unused or uses aligned units.
    /// </summary>
    internal static IUnitConversionRateService PassiveIdentity()
    {
        var m = new Mock<IUnitConversionRateService>();
        m.Setup(x => x.LoadUnitInfoAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) =>
                ids.Distinct().ToDictionary(id => id, id => new UnitConversionInfo(id, "Weight", 1m)));
        return m.Object;
    }
}
