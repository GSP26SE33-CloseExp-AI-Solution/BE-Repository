namespace CloseExpAISolution.Application.Services;

public static class StockLotUnitRules
{
    public static void EnsureLotUnitMatchesProductType(
        string productUnitType,
        string lotUnitType)
    {
        if (!UnitMeasureTypeCompatibility.AreCompatible(lotUnitType, productUnitType))
        {
            throw new InvalidOperationException(
                $"Đơn vị lô phải cùng loại với đơn vị chuẩn sản phẩm ({productUnitType}). " +
                $"Đơn vị lô hiện tại: {lotUnitType}.");
        }
    }
}
