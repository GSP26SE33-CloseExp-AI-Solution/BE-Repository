using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services;

public static class CategoryUnitTypePolicy
{
    public const string CountUnitType = "Đếm";
    public const string WeightUnitType = "Khối lượng";

    public static string ResolveAllowedUnitType(bool isFreshFood, string? freshFoodUnitType = null, string? nonFreshFoodUnitType = null) =>
        isFreshFood ? (freshFoodUnitType ?? WeightUnitType) : (nonFreshFoodUnitType ?? CountUnitType);

    public static void EnsureProductUnitMatchesCategory(Category category, UnitOfMeasure unit, string? allowedUnitType = null)
    {
        var allowedType = allowedUnitType ?? ResolveAllowedUnitType(category.IsFreshFood);

        if (!UnitMeasureTypeCompatibility.AreCompatible(unit.Type, allowedType))
        {
            throw new InvalidOperationException(
                $"Danh mục \"{category.Name}\" chỉ cho phép đơn vị loại {allowedType}. " +
                $"Đơn vị đã chọn: {unit.Type}.");
        }

        if (unit.ConversionRate != 1m)
        {
            throw new InvalidOperationException(
                "Đơn vị chuẩn sản phẩm phải là đơn vị gốc trong nhóm (hệ số quy đổi = 1).");
        }
    }

    public static void EnsureProductReadyForStockLot(
        string name,
        string barcode,
        string categoryName,
        Category? category,
        UnitOfMeasure unit,
        string? brand,
        string? allowedUnitType = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên sản phẩm là bắt buộc.");

        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Mã vạch là bắt buộc.");

        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("Danh mục sản phẩm là bắt buộc.");

        if (category == null)
            throw new InvalidOperationException("Không tìm thấy danh mục sản phẩm.");

        EnsureProductUnitMatchesCategory(category, unit, allowedUnitType);

        if (!category.IsFreshFood && string.IsNullOrWhiteSpace(brand))
        {
            throw new ArgumentException(
                "Thương hiệu là bắt buộc với sản phẩm không thuộc nhóm tươi sống.");
        }
    }
}
