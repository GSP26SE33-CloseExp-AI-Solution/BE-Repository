using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class PurchaseUnitOrderHelperTests
{
    private static readonly Guid PieceId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid PackId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid BoxId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private sealed class FakeUnitConversion : IUnitConversionRateService
    {
        private readonly Dictionary<Guid, UnitConversionInfo> _units;

        public FakeUnitConversion(Dictionary<Guid, UnitConversionInfo> units) => _units = units;

        public Task<Dictionary<Guid, UnitConversionInfo>> LoadUnitInfoAsync(
            IEnumerable<Guid> unitIds,
            CancellationToken cancellationToken = default)
        {
            var dict = unitIds
                .Distinct()
                .Where(_units.ContainsKey)
                .ToDictionary(id => id, id => _units[id]);
            return Task.FromResult(dict);
        }

        public decimal ConvertQuantity(Guid fromUnitId, Guid toUnitId, decimal quantity) =>
            UnitConversionRateConverter.ConvertQuantity(fromUnitId, toUnitId, quantity, _units);

        public decimal ConvertUnitPrice(Guid fromUnitId, Guid toUnitId, decimal unitPrice) =>
            UnitConversionRateConverter.ConvertUnitPrice(fromUnitId, toUnitId, unitPrice, _units);

        public short ConvertQuantityToShort(Guid fromUnitId, Guid toUnitId, decimal quantity) =>
            UnitConversionRateConverter.ConvertQuantityToShort(fromUnitId, toUnitId, quantity, _units);

        public Task<IReadOnlyList<Guid>> GetUnitIdsByTypeAsync(
            string unitType,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(unitType))
                return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());

            var normalized = unitType.Trim();
            return Task.FromResult<IReadOnlyList<Guid>>(_units.Values
                .Where(u => string.Equals(u.Type, normalized, StringComparison.OrdinalIgnoreCase))
                .Select(u => u.UnitId)
                .ToList());
        }
    }

    [Fact]
    public async Task GetAllowedPurchaseUnitIds_ExcludesSmallerUnits_WhenLotUsesLargerPack()
    {
        var packId = ProductPurchaseUnitPolicy.UnitPackId;
        var pieceId = ProductPurchaseUnitPolicy.UnitPieceId;
        var units = new Dictionary<Guid, UnitConversionInfo>
        {
            [packId] = new UnitConversionInfo(packId, "Đếm", 6m),
            [pieceId] = new UnitConversionInfo(pieceId, "Đếm", 1m),
        };

        var helper = new PurchaseUnitOrderHelper(new FakeUnitConversion(units));
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            UnitId = packId,
            CategoryId = ProductPurchaseUnitPolicy.CategoryInstantFoodId,
        };
        var lots = new[]
        {
            new StockLot
            {
                LotId = Guid.NewGuid(),
                ProductId = product.ProductId,
                UnitId = packId,
                Status = ProductState.Published,
                Quantity = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(5),
            },
        };

        var allowed = await helper.GetAllowedPurchaseUnitIdsAsync(product, lots);

        Assert.Contains(packId, allowed);
        Assert.DoesNotContain(pieceId, allowed);
    }

    [Fact]
    public void EnsurePurchaseUnitAllowed_RejectsSmallerUnit_WhenLotUsesLargerPack()
    {
        var units = new Dictionary<Guid, UnitConversionInfo>
        {
            [PieceId] = new UnitConversionInfo(PieceId, "Count", 1m),
            [PackId] = new UnitConversionInfo(PackId, "Count", 6m),
        };

        var helper = new PurchaseUnitOrderHelper(new FakeUnitConversion(units));
        var product = new Product { ProductId = Guid.NewGuid(), UnitId = PieceId };
        var lot = new StockLot
        {
            LotId = Guid.NewGuid(),
            ProductId = product.ProductId,
            UnitId = PackId,
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            helper.EnsurePurchaseUnitAllowed(PieceId, product, lot, units));

        Assert.Contains("hệ số quy đổi lớn", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllowedPurchaseUnitIds_ExcludesLotUnit_WhenTypeDiffersFromProduct()
    {
        var kgId = Guid.Parse("00000000-0000-0000-0000-000000000004");
        var units = new Dictionary<Guid, UnitConversionInfo>
        {
            [PieceId] = new UnitConversionInfo(PieceId, "Count", 1m),
            [kgId] = new UnitConversionInfo(kgId, "Weight", 1m),
        };

        var helper = new PurchaseUnitOrderHelper(new FakeUnitConversion(units));
        var product = new Product { ProductId = Guid.NewGuid(), UnitId = PieceId };
        var lots = new[]
        {
            new StockLot
            {
                LotId = Guid.NewGuid(),
                ProductId = product.ProductId,
                UnitId = kgId,
                Status = ProductState.Published,
                Quantity = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(5),
            },
        };

        var allowed = await helper.GetAllowedPurchaseUnitIdsAsync(product, lots);

        Assert.Empty(allowed);
    }

    [Fact]
    public async Task GetAllowedPurchaseUnitIds_ReturnsEmpty_WhenLegacyPackLotDoesNotMatchBottlePolicy()
    {
        var bottleId = ProductPurchaseUnitPolicy.UnitBottleId;
        var packId = ProductPurchaseUnitPolicy.UnitPackId;
        var units = new Dictionary<Guid, UnitConversionInfo>
        {
            [bottleId] = new UnitConversionInfo(bottleId, "Đếm", 1m),
            [packId] = new UnitConversionInfo(packId, "Đếm", 6m),
        };

        var helper = new PurchaseUnitOrderHelper(new FakeUnitConversion(units));
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            UnitId = bottleId,
            CategoryId = ProductPurchaseUnitPolicy.CategoryDairyId,
        };
        var lots = new[]
        {
            new StockLot
            {
                LotId = Guid.NewGuid(),
                ProductId = product.ProductId,
                UnitId = packId,
                Status = ProductState.Published,
                Quantity = 4,
                ExpiryDate = DateTime.UtcNow.AddDays(5),
            },
        };

        var allowed = await helper.GetAllowedPurchaseUnitIdsAsync(product, lots);

        Assert.Empty(allowed);
    }

    [Fact]
    public void EnsurePurchaseUnitAllowed_RejectsPack_WhenBottleProductUsesDairyPolicy()
    {
        var bottleId = ProductPurchaseUnitPolicy.UnitBottleId;
        var packId = ProductPurchaseUnitPolicy.UnitPackId;
        var units = new Dictionary<Guid, UnitConversionInfo>
        {
            [bottleId] = new UnitConversionInfo(bottleId, "Đếm", 1m),
            [packId] = new UnitConversionInfo(packId, "Đếm", 6m),
        };

        var helper = new PurchaseUnitOrderHelper(new FakeUnitConversion(units));
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            UnitId = bottleId,
            CategoryId = ProductPurchaseUnitPolicy.CategoryDairyId,
        };
        var lot = new StockLot
        {
            LotId = Guid.NewGuid(),
            ProductId = product.ProductId,
            UnitId = packId,
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            helper.EnsurePurchaseUnitAllowed(packId, product, lot, units));

        Assert.Contains("không được phép", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsurePurchaseUnitAllowed_RejectsPurchaseUnit_WhenTypeDiffersFromProduct()
    {
        var kgId = Guid.Parse("00000000-0000-0000-0000-000000000004");
        var units = new Dictionary<Guid, UnitConversionInfo>
        {
            [PieceId] = new UnitConversionInfo(PieceId, "Count", 1m),
            [kgId] = new UnitConversionInfo(kgId, "Weight", 1m),
        };

        var helper = new PurchaseUnitOrderHelper(new FakeUnitConversion(units));
        var product = new Product { ProductId = Guid.NewGuid(), UnitId = PieceId };
        var lot = new StockLot
        {
            LotId = Guid.NewGuid(),
            ProductId = product.ProductId,
            UnitId = kgId,
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            helper.EnsurePurchaseUnitAllowed(PieceId, product, lot, units));

        Assert.Contains("cùng loại", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
