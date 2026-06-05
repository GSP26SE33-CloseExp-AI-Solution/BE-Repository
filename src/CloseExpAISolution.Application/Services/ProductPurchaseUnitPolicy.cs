namespace CloseExpAISolution.Application.Services;

public static class ProductPurchaseUnitPolicy
{
    public static readonly Guid UnitKgId = Guid.Parse("aaaa0001-0001-0001-0001-000000000001");
    public static readonly Guid UnitGramId = Guid.Parse("aaaa0002-0002-0002-0002-000000000002");
    public static readonly Guid UnitBoxId = Guid.Parse("aaaa0005-0005-0005-0005-000000000005");
    public static readonly Guid UnitBottleId = Guid.Parse("aaaa0006-0006-0006-0006-000000000006");
    public static readonly Guid UnitPackId = Guid.Parse("aaaa0007-0007-0007-0007-000000000007");
    public static readonly Guid UnitPieceId = Guid.Parse("aaaa0008-0008-0008-0008-000000000008");
    public static readonly Guid UnitCanId = Guid.Parse("aaaa0009-0009-0009-0009-000000000009");
    public static readonly Guid UnitBagId = Guid.Parse("aaaa000a-000a-000a-000a-00000000000a");
    public static readonly Guid UnitThungId = Guid.Parse("aaaa000b-000b-000b-000b-00000000000b");

    public static readonly Guid CategoryDairyId = Guid.Parse("ccca0001-0001-0001-0001-000000000001");
    public static readonly Guid CategoryMeatSeafoodId = Guid.Parse("ccca0002-0002-0002-0002-000000000002");
    public static readonly Guid CategoryVegetableId = Guid.Parse("ccca0003-0003-0003-0003-000000000003");
    public static readonly Guid CategoryDryFoodId = Guid.Parse("ccca0004-0004-0004-0004-000000000004");
    public static readonly Guid CategoryFrozenId = Guid.Parse("ccca0005-0005-0005-0005-000000000005");
    public static readonly Guid CategorySpiceId = Guid.Parse("ccca0006-0006-0006-0006-000000000006");
    public static readonly Guid CategorySnackSubId = Guid.Parse("ccca0007-0007-0007-0007-000000000007");
    public static readonly Guid CategoryFruitFreshId = Guid.Parse("ccca0008-0008-0008-0008-000000000008");
    public static readonly Guid CategoryBreakfastCerealId = Guid.Parse("ccca0009-0009-0009-0009-000000000009");
    public static readonly Guid CategoryCannedGoodsId = Guid.Parse("ccca000a-000a-000a-000a-00000000000a");
    public static readonly Guid CategoryVegetarianId = Guid.Parse("ccca000b-000b-000b-000b-00000000000b");
    public static readonly Guid CategoryTofuEggId = Guid.Parse("ccca000c-000c-000c-000c-00000000000c");
    public static readonly Guid CategoryLeafyGreensId = Guid.Parse("ccca000d-000d-000d-000d-00000000000d");
    public static readonly Guid CategoryBiscuitCandyId = Guid.Parse("ccca000e-000e-000e-000e-00000000000e");
    public static readonly Guid CategoryInstantFoodId = Guid.Parse("ccca000f-000f-000f-000f-00000000000f");

    public static IReadOnlySet<Guid> GetPurchasableUnitIds(Guid productUnitId, Guid categoryId)
    {
        if (categoryId != Guid.Empty)
            return GetPurchasableUnitIdsByCategory(categoryId, productUnitId);

        return GetPurchasableUnitIdsByProductUnit(productUnitId);
    }

    public static bool IsPurchasableUnit(Guid unitId, Guid productUnitId, Guid categoryId) =>
        GetPurchasableUnitIds(productUnitId, categoryId).Contains(unitId);

    private static IReadOnlySet<Guid> GetPurchasableUnitIdsByCategory(Guid categoryId, Guid productUnitId) =>
        categoryId switch
        {
            var id when id == CategoryTofuEggId => UnitSet(UnitPieceId, UnitBoxId),
            var id when IsWeightCategory(id) => GetWeightUnits(productUnitId),
            var id when id == CategoryDairyId => GetBeverageUnits(productUnitId),
            var id when id == CategoryCannedGoodsId => GetCannedUnits(productUnitId),
            var id when id == CategorySpiceId => GetSpiceUnits(productUnitId),
            var id when IsPackagedSnackCategory(id) => GetPackagedCountUnits(productUnitId),
            var id when id == CategoryDryFoodId => GetDryFoodUnits(productUnitId),
            var id when id == CategoryFrozenId => GetFrozenUnits(productUnitId),
            var id when id == CategoryVegetarianId => GetVegetarianUnits(productUnitId),
            _ => GetPurchasableUnitIdsByProductUnit(productUnitId)
        };

    private static bool IsWeightCategory(Guid categoryId) =>
        categoryId == CategoryMeatSeafoodId
        || categoryId == CategoryVegetableId
        || categoryId == CategoryLeafyGreensId
        || categoryId == CategoryFruitFreshId;

    private static bool IsPackagedSnackCategory(Guid categoryId) =>
        categoryId == CategorySnackSubId
        || categoryId == CategoryBiscuitCandyId
        || categoryId == CategoryInstantFoodId
        || categoryId == CategoryBreakfastCerealId;

    private static IReadOnlySet<Guid> GetWeightUnits(Guid productUnitId) =>
        productUnitId switch
        {
            var id when id == UnitGramId => UnitSet(UnitGramId, UnitKgId),
            _ => UnitSet(UnitKgId, UnitGramId)
        };

    private static IReadOnlySet<Guid> GetBeverageUnits(Guid productUnitId) =>
        productUnitId switch
        {
            var id when id == UnitBottleId => UnitSet(UnitBottleId, UnitThungId),
            var id when id == UnitCanId => UnitSet(UnitCanId, UnitThungId),
            var id when id == UnitPackId => UnitSet(UnitPackId, UnitPieceId),
            var id when id == UnitBoxId => UnitSet(UnitBoxId, UnitPieceId),
            _ => GetPackagedCountUnits(productUnitId)
        };

    private static IReadOnlySet<Guid> GetCannedUnits(Guid productUnitId) =>
        productUnitId == UnitCanId
            ? UnitSet(UnitCanId, UnitThungId)
            : UnitSet(productUnitId, UnitCanId, UnitThungId);

    private static IReadOnlySet<Guid> GetSpiceUnits(Guid productUnitId) =>
        productUnitId switch
        {
            var id when id == UnitBottleId => UnitSet(UnitBottleId, UnitThungId),
            var id when id == UnitPackId => UnitSet(UnitPackId, UnitPieceId),
            var id when id == UnitBagId => UnitSet(UnitBagId, UnitPieceId),
            _ => UnitSet(productUnitId, UnitPieceId)
        };

    private static IReadOnlySet<Guid> GetPackagedCountUnits(Guid productUnitId) =>
        productUnitId switch
        {
            var id when id == UnitBoxId => UnitSet(UnitBoxId, UnitPieceId),
            var id when id == UnitPackId => UnitSet(UnitPackId, UnitPieceId),
            _ => UnitSet(productUnitId, UnitPieceId, UnitBoxId)
        };

    private static IReadOnlySet<Guid> GetDryFoodUnits(Guid productUnitId) =>
        productUnitId switch
        {
            var id when id == UnitKgId => UnitSet(UnitKgId, UnitGramId),
            var id when id == UnitBagId => UnitSet(UnitBagId, UnitKgId),
            var id when id == UnitPackId => UnitSet(UnitPackId, UnitPieceId),
            var id when id == UnitBoxId => UnitSet(UnitBoxId, UnitPieceId),
            _ => GetPackagedCountUnits(productUnitId)
        };

    private static IReadOnlySet<Guid> GetFrozenUnits(Guid productUnitId) =>
        productUnitId switch
        {
            var id when id == UnitKgId => UnitSet(UnitKgId, UnitGramId),
            var id when id == UnitPackId => UnitSet(UnitPackId, UnitPieceId),
            var id when id == UnitBoxId => UnitSet(UnitBoxId, UnitPieceId),
            _ => UnitSet(productUnitId, UnitPieceId)
        };

    private static IReadOnlySet<Guid> GetVegetarianUnits(Guid productUnitId) =>
        productUnitId switch
        {
            var id when id == UnitKgId => UnitSet(UnitKgId, UnitGramId),
            var id when id == UnitPackId => UnitSet(UnitPackId, UnitPieceId),
            _ => UnitSet(UnitPieceId, UnitBoxId)
        };

    private static IReadOnlySet<Guid> GetPurchasableUnitIdsByProductUnit(Guid productUnitId) =>
        productUnitId switch
        {
            var id when id == UnitKgId => UnitSet(UnitKgId, UnitGramId),
            var id when id == UnitGramId => UnitSet(UnitGramId, UnitKgId),
            var id when id == UnitBottleId => UnitSet(UnitBottleId, UnitThungId),
            var id when id == UnitCanId => UnitSet(UnitCanId, UnitThungId),
            var id when id == UnitPackId => UnitSet(UnitPackId, UnitPieceId),
            var id when id == UnitBoxId => UnitSet(UnitBoxId, UnitPieceId),
            var id when id == UnitPieceId => UnitSet(UnitPieceId, UnitBoxId),
            _ => UnitSet(productUnitId, UnitPieceId, UnitBoxId)
        };

    private static HashSet<Guid> UnitSet(params Guid[] unitIds) => new(unitIds);
}
