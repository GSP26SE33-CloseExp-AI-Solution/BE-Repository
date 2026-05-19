using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN013 — UTCID01–UTCID10 per <c>.github/instructions/get-product-detail-async-test-sheet.md</c>
/// (<see cref="ProductService.GetProductDetailAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~ProductServiceGetProductDetailAsyncTests"</c>
/// </summary>
public sealed class ProductServiceGetProductDetailAsyncTests : IDisposable
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");

    private static readonly Guid SupermarketId = Guid.Parse("10000000-0000-4000-a013-000000000013");
    private static readonly Guid UnitId = Guid.Parse("20000000-0000-4000-a013-000000000013");
    private static readonly DateTime T0 = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);

    public ProductServiceGetProductDetailAsyncTests()
    {
        _conn.Open();
    }

    public void Dispose() => _conn.Dispose();

    private ApplicationDbContext CreateContext()
    {
        var opt = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_conn).Options;
        var ctx = new ApplicationDbContext(opt);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static void SeedCore(ApplicationDbContext ctx)
    {
        ctx.Supermarkets.Add(new Supermarket
        {
            SupermarketId = SupermarketId,
            Name = "S",
            Address = "",
            Latitude = 0,
            Longitude = 0,
            ContactPhone = "1",
            Status = SupermarketState.Active,
            CreatedAt = T0
        });
        ctx.UnitOfMeasures.Add(new UnitOfMeasure
        {
            UnitId = UnitId,
            Name = "g",
            Type = "mass",
            Symbol = "g",
            ConversionRate = 1m,
            CreatedAt = T0,
            UpdatedAt = T0
        });
        ctx.SaveChanges();
    }

    private Guid AddVerifiedProduct(ApplicationDbContext ctx, string barcodeSuffix)
    {
        var id = Guid.NewGuid();
        var p = new Product
        {
            ProductId = id,
            SupermarketId = SupermarketId,
            UnitId = UnitId,
            Status = ProductState.Verified,
            Name = "Ver",
            Barcode = barcodeSuffix + id.ToString("N")[..6],
            Sku = "sk",
            CreatedAt = T0,
            UpdatedAt = T0,
            CreatedBy = "t",
            ProductDetail = new ProductDetail
            {
                ProductDetailId = Guid.NewGuid(),
                ProductId = id,
                Description = "D",
                Brand = "Brand"
            }
        };
        ctx.Products.Add(p);
        ctx.SaveChanges();
        return id;
    }

    private static StockLot NewLot(Guid lotId, Guid productId, DateTime expiryUtc, decimal qty)
    {
        return new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            UnitId = UnitId,
            ExpiryDate = expiryUtc,
            ManufactureDate = expiryUtc.AddDays(-10),
            Quantity = qty,
            OriginalUnitPrice = 12,
            SuggestedUnitPrice = 10,
            Weight = 1,
            Status = ProductState.Published,
            CreatedAt = T0,
            UpdatedAt = T0
        };
    }

    [Fact]
    public async Task UTCID01_UnknownProductId_ReturnsNull()
    {
        await using var ctx = CreateContext();
        SeedCore(ctx);
        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);

        var result = await sut.GetProductDetailAsync(Guid.NewGuid(), false);

        Assert.Null(result);
    }

    [Fact]
    public async Task UTCID02_Hidden_WithIncludeFalse_ReturnsNull()
    {
        await using var ctx = CreateContext();
        SeedCore(ctx);
        var pid = Guid.NewGuid();
        ctx.Products.Add(new Product
        {
            ProductId = pid,
            SupermarketId = SupermarketId,
            UnitId = UnitId,
            Status = ProductState.Hidden,
            Name = "H",
            Barcode = Guid.NewGuid().ToString()[..15],
            Sku = "s",
            CreatedAt = T0,
            UpdatedAt = T0,
            CreatedBy = ""
        });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);

        Assert.Null(await sut.GetProductDetailAsync(pid, false));
    }

    [Fact]
    public async Task UTCID03_Deleted_WithIncludeFalse_ReturnsNull()
    {
        await using var ctx = CreateContext();
        SeedCore(ctx);
        var pid = Guid.NewGuid();
        ctx.Products.Add(new Product
        {
            ProductId = pid,
            SupermarketId = SupermarketId,
            UnitId = UnitId,
            Status = ProductState.Deleted,
            Name = "D",
            Barcode = Guid.NewGuid().ToString()[..15],
            Sku = "s",
            CreatedAt = T0,
            UpdatedAt = T0,
            CreatedBy = ""
        });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);

        Assert.Null(await sut.GetProductDetailAsync(pid, false));
    }

    [Fact]
    public async Task UTCID04_Hidden_WithIncludeTrue_ReturnsDetail()
    {
        await using var ctx = CreateContext();
        SeedCore(ctx);
        var pid = Guid.NewGuid();
        ctx.Products.Add(new Product
        {
            ProductId = pid,
            SupermarketId = SupermarketId,
            UnitId = UnitId,
            Status = ProductState.Hidden,
            Name = "H2",
            Barcode = Guid.NewGuid().ToString()[..15],
            Sku = "x",
            CreatedAt = T0,
            UpdatedAt = T0,
            CreatedBy = ""
        });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);

        var detail = await sut.GetProductDetailAsync(pid, true);

        Assert.NotNull(detail);
        Assert.Equal(ProductState.Hidden, detail!.Status);
    }

    [Fact]
    public async Task UTCID05_HappyPath_Images_PricingFarExpiry_VerifiedBaseline()
    {
        await using var ctx = CreateContext();
        SeedCore(ctx);
        var pid = AddVerifiedProduct(ctx, "bc-detail-utc05");
        ctx.ProductImages.Add(new ProductImage
        {
            ProductImageId = Guid.NewGuid(),
            ProductId = pid,
            ImageUrl = "/detail.jpg",
            CreatedAt = T0,
            IsPrimary = true
        });
        var lot = NewLot(Guid.NewGuid(), pid, DateTime.UtcNow.AddDays(45), 4);
        ctx.StockLots.Add(lot);
        ctx.PricingHistories.Add(new PricingHistory
        {
            AIPriceId = Guid.NewGuid(),
            LotId = lot.LotId,
            SuggestedPrice = 12m,
            AIConfidence = 0.9m,
            ConfirmedBy = "c",
            ConfirmedAt = T0.AddMinutes(30),
            CreatedAt = T0
        });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);

        var d = await sut.GetProductDetailAsync(pid, false);

        Assert.NotNull(d);
        Assert.Equal("Đang cập nhật", d!.UnitName);
        Assert.Equal("/detail.jpg", d.MainImageUrl);
        Assert.Single(d.ProductImages);
        Assert.Equal(12m, d.SuggestedPrice);
        Assert.Equal(4m, d.Quantity);
        Assert.True(d.DaysToExpiry is > 2);
        Assert.Equal(ExpiryStatus.LongTerm, d.ExpiryStatus);
    }

    [Fact]
    public async Task UTCID06_NoProductImages_HasZeroTotals()
    {
        await using var ctx = CreateContext();
        SeedCore(ctx);
        var pid = AddVerifiedProduct(ctx, "utc06-no-img");
        var lot = NewLot(Guid.NewGuid(), pid, DateTime.UtcNow.AddDays(18), 1);
        ctx.StockLots.Add(lot);
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var d = await sut.GetProductDetailAsync(pid, false);

        Assert.NotNull(d);
        Assert.Null(d!.MainImageUrl);
        Assert.Equal(0, d.TotalImages);
    }

    [Fact]
    public async Task UTCID07_NoStockLots_NoSuggestedPrice_FromPricing()
    {
        await using var ctx = CreateContext();
        SeedCore(ctx);
        var pid = AddVerifiedProduct(ctx, "utc07-lot-less");
        ctx.ProductImages.Add(new ProductImage
        {
            ProductImageId = Guid.NewGuid(),
            ProductId = pid,
            ImageUrl = "/z.png",
            CreatedAt = T0,
            IsPrimary = false
        });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var d = await sut.GetProductDetailAsync(pid, false);

        Assert.NotNull(d);
        Assert.Equal(0m, d!.SuggestedPrice);
        Assert.Equal(0m, d.Quantity);
    }

    [Fact]
    public async Task UTCID08_WithPricing_SuggestedPriceSet_WhenGreaterThanZero()
    {
        await using var ctx = CreateContext();
        SeedCore(ctx);
        var pid = AddVerifiedProduct(ctx, "utc08-price");
        var lot = NewLot(Guid.NewGuid(), pid, DateTime.UtcNow.AddDays(30), 2);
        ctx.StockLots.Add(lot);
        ctx.PricingHistories.Add(new PricingHistory
        {
            AIPriceId = Guid.NewGuid(),
            LotId = lot.LotId,
            SuggestedPrice = 3.33m,
            AIConfidence = 0.5m,
            CreatedAt = T0.AddDays(-2),
            ConfirmedAt = T0.AddDays(-1),
            ConfirmedBy = "u"
        });
        ctx.ProductImages.Add(new ProductImage
        {
            ProductImageId = Guid.NewGuid(),
            ProductId = pid,
            ImageUrl = "/img.png",
            CreatedAt = T0,
            IsPrimary = false
        });
        await ctx.SaveChangesAsync();

        var d = await ProductServiceTestInfrastructure.CreateProductService(ctx).GetProductDetailAsync(pid, false);

        Assert.Equal(3.33m, d!.SuggestedPrice);
    }

    [Fact]
    public async Task UTCID09_NearestExpiryWithinTwoDays_ReturnsExpiringSoon()
    {
        await using var ctx = CreateContext();
        SeedCore(ctx);
        var pid = AddVerifiedProduct(ctx, "utc09-expSoon");
        var far = DateTime.UtcNow.Date.AddMonths(6).AddHours(10);
        var near = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        ctx.StockLots.Add(NewLot(Guid.NewGuid(), pid, far, 1));
        ctx.StockLots.Add(NewLot(Guid.NewGuid(), pid, near, 1));
        ctx.ProductImages.Add(new ProductImage
        {
            ProductImageId = Guid.NewGuid(),
            ProductId = pid,
            ImageUrl = "/soon.jpg",
            CreatedAt = T0,
            IsPrimary = false
        });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);

        var d = await sut.GetProductDetailAsync(pid, false);

        Assert.NotNull(d);
        Assert.True(d!.DaysToExpiry is >= 1 and <= 2);
        Assert.Equal(ExpiryStatus.ExpiringSoon, d.ExpiryStatus);
    }

    [Fact]
    public async Task UTCID10_MultipleStockLots_QuantityIsSumOfQuantities()
    {
        await using var ctx = CreateContext();
        SeedCore(ctx);
        var pid = AddVerifiedProduct(ctx, "utc10-sum");
        var future = DateTime.UtcNow.Date.AddMonths(5);
        ctx.StockLots.Add(NewLot(Guid.NewGuid(), pid, future, 3));
        ctx.StockLots.Add(NewLot(Guid.NewGuid(), pid, future.AddDays(1), 7));
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var d = await sut.GetProductDetailAsync(pid, false);

        Assert.NotNull(d);
        Assert.Equal(10m, d!.Quantity);
    }
}
