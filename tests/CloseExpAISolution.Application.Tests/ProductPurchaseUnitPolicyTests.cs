using CloseExpAISolution.Application.Services;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class ProductPurchaseUnitPolicyTests
{
    [Fact]
    public void TofuEggCategory_OnlyAllowsPieceAndBox()
    {
        var allowed = ProductPurchaseUnitPolicy.GetPurchasableUnitIds(
            ProductPurchaseUnitPolicy.UnitPackId,
            ProductPurchaseUnitPolicy.CategoryTofuEggId);

        Assert.Equal(2, allowed.Count);
        Assert.Contains(ProductPurchaseUnitPolicy.UnitPieceId, allowed);
        Assert.Contains(ProductPurchaseUnitPolicy.UnitBoxId, allowed);
        Assert.DoesNotContain(ProductPurchaseUnitPolicy.UnitPackId, allowed);
        Assert.DoesNotContain(ProductPurchaseUnitPolicy.UnitBagId, allowed);
    }

    [Fact]
    public void DairyCategory_BottleProduct_AllowsBottleAndThung()
    {
        var allowed = ProductPurchaseUnitPolicy.GetPurchasableUnitIds(
            ProductPurchaseUnitPolicy.UnitBottleId,
            ProductPurchaseUnitPolicy.CategoryDairyId);

        Assert.Equal(2, allowed.Count);
        Assert.Contains(ProductPurchaseUnitPolicy.UnitBottleId, allowed);
        Assert.Contains(ProductPurchaseUnitPolicy.UnitThungId, allowed);
        Assert.DoesNotContain(ProductPurchaseUnitPolicy.UnitPieceId, allowed);
        Assert.DoesNotContain(ProductPurchaseUnitPolicy.UnitPackId, allowed);
        Assert.DoesNotContain(ProductPurchaseUnitPolicy.UnitBoxId, allowed);
    }

    [Fact]
    public void InstantFoodCategory_PackProduct_AllowsPackAndPiece()
    {
        var allowed = ProductPurchaseUnitPolicy.GetPurchasableUnitIds(
            ProductPurchaseUnitPolicy.UnitPackId,
            ProductPurchaseUnitPolicy.CategoryInstantFoodId);

        Assert.Contains(ProductPurchaseUnitPolicy.UnitPackId, allowed);
        Assert.Contains(ProductPurchaseUnitPolicy.UnitPieceId, allowed);
        Assert.DoesNotContain(ProductPurchaseUnitPolicy.UnitBagId, allowed);
        Assert.DoesNotContain(ProductPurchaseUnitPolicy.UnitBoxId, allowed);
    }

    [Fact]
    public void MeatCategory_KgProduct_AllowsKgAndGram()
    {
        var allowed = ProductPurchaseUnitPolicy.GetPurchasableUnitIds(
            ProductPurchaseUnitPolicy.UnitKgId,
            ProductPurchaseUnitPolicy.CategoryMeatSeafoodId);

        Assert.Equal(2, allowed.Count);
        Assert.Contains(ProductPurchaseUnitPolicy.UnitKgId, allowed);
        Assert.Contains(ProductPurchaseUnitPolicy.UnitGramId, allowed);
        Assert.DoesNotContain(ProductPurchaseUnitPolicy.UnitPieceId, allowed);
    }

    [Fact]
    public void MissingCategory_FallsBackToProductUnitRules()
    {
        var allowed = ProductPurchaseUnitPolicy.GetPurchasableUnitIds(
            ProductPurchaseUnitPolicy.UnitBottleId,
            Guid.Empty);

        Assert.Contains(ProductPurchaseUnitPolicy.UnitBottleId, allowed);
        Assert.Contains(ProductPurchaseUnitPolicy.UnitThungId, allowed);
    }
}
