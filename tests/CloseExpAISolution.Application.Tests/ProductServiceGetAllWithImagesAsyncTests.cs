using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN012 — UTCID01–UTCID10 per <c>.github/instructions/get-all-with-images-async-test-sheet.md</c>
/// (<see cref="ProductService.GetAllWithImagesAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~ProductServiceGetAllWithImagesAsyncTests"</c>
/// </summary>
public sealed class ProductServiceGetAllWithImagesAsyncTests : IDisposable
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");

    private static readonly Guid SupermarketId = Guid.Parse("10000000-0000-4000-a000-000000000012");
    private static readonly Guid UnitId = Guid.Parse("20000000-0000-4000-a000-000000000012");

    private static readonly DateTime T0 = new(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);

    public ProductServiceGetAllWithImagesAsyncTests()
    {
        _conn.Open();
    }

    public void Dispose() => _conn.Dispose();

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_conn)
            .Options;
        var ctx = new ApplicationDbContext(options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static void SeedSupermarketUnit(ApplicationDbContext ctx)
    {
        ctx.Supermarkets.Add(new Supermarket
        {
            SupermarketId = SupermarketId,
            Name = "S",
            Address = "a",
            Latitude = 0,
            Longitude = 0,
            ContactPhone = "x",
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

    private Product AddProduct(ApplicationDbContext ctx, Guid pid, ProductState status, string name)
    {
        var p = new Product
        {
            ProductId = pid,
            SupermarketId = SupermarketId,
            UnitId = UnitId,
            Name = name,
            Barcode = name + pid.ToString("N")[..6],
            Sku = "sku-" + pid.ToString("N")[..6],
            Status = status,
            CreatedAt = T0,
            UpdatedAt = T0,
            CreatedBy = "t",
            StockLots = new List<StockLot>()
        };
        ctx.Products.Add(p);
        return p;
    }

    [Fact]
    public async Task UTCID01_EmptyProducts_ReturnsEmptySequence()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var list = await sut.GetAllWithImagesAsync(false);

        Assert.Empty(list);
    }

    [Fact]
    public async Task UTCID02_IncludeHiddenFalse_FiltersHiddenAndDeleted()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var draftId = Guid.NewGuid();
        AddProduct(ctx, draftId, ProductState.Draft, "d");
        AddProduct(ctx, Guid.NewGuid(), ProductState.Hidden, "h");
        AddProduct(ctx, Guid.NewGuid(), ProductState.Deleted, "x");
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var list = (await sut.GetAllWithImagesAsync(false)).ToList();

        Assert.Single(list);
        Assert.Equal(draftId, list[0].ProductId);
    }

    [Fact]
    public async Task UTCID03_IncludeHiddenTrue_ReturnsHiddenAndDeleted()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        AddProduct(ctx, Guid.NewGuid(), ProductState.Draft, "d");
        AddProduct(ctx, Guid.NewGuid(), ProductState.Hidden, "h");
        AddProduct(ctx, Guid.NewGuid(), ProductState.Deleted, "x");
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var list = (await sut.GetAllWithImagesAsync(true)).ToList();

        Assert.Equal(3, list.Count);
    }

    [Fact]
    public async Task UTCID04_TwoProducts_GroupBy_ImageMappingPerProductId()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        AddProduct(ctx, id1, ProductState.Draft, "p1");
        AddProduct(ctx, id2, ProductState.Draft, "p2");
        ctx.ProductImages.AddRange(
            new ProductImage
            {
                ProductImageId = Guid.NewGuid(), ProductId = id1, ImageUrl = "/a.jpg", CreatedAt = T0, IsPrimary = true
            },
            new ProductImage
            {
                ProductImageId = Guid.NewGuid(), ProductId = id2, ImageUrl = "/b.jpg",
                CreatedAt = T0.AddMinutes(5), IsPrimary = false
            });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var list = (await sut.GetAllWithImagesAsync(false)).OrderBy(e => e.Name).ToList();

        Assert.Equal(2, list.Count);
        var e1 = list.Single(x => x.ProductId == id1);
        var e2 = list.Single(x => x.ProductId == id2);
        Assert.Equal("/a.jpg", e1.MainImageUrl);
        Assert.Equal(1, e1.TotalImages);
        Assert.Equal("/b.jpg", e2.MainImageUrl);
        Assert.Equal(1, e2.TotalImages);
    }

    [Fact]
    public async Task UTCID05_NoProductImage_HasZeroTotals()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var pid = Guid.NewGuid();
        AddProduct(ctx, pid, ProductState.Draft, "noimg");
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var dto = (await sut.GetAllWithImagesAsync(false)).Single();

        Assert.Null(dto.MainImageUrl);
        Assert.Equal(0, dto.TotalImages);
        Assert.Empty(dto.ProductImages);
    }

    [Fact]
    public async Task UTCID06_TwoImagesOrderedByCreatedAt_MainFirst()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var pid = Guid.NewGuid();
        AddProduct(ctx, pid, ProductState.Draft, "dual");
        ctx.ProductImages.AddRange(
            new ProductImage { ProductImageId = Guid.NewGuid(), ProductId = pid, ImageUrl = "first.jpg", CreatedAt = T0 },
            new ProductImage
            {
                ProductImageId = Guid.NewGuid(), ProductId = pid, ImageUrl = "second.jpg", CreatedAt = T0.AddMinutes(10)
            });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var dto = (await sut.GetAllWithImagesAsync(false)).Single();

        Assert.Equal("first.jpg", dto.MainImageUrl);
        Assert.Equal(2, dto.TotalImages);
    }

    [Fact]
    public async Task UTCID07_NoLots_NoPricingFields()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var pid = Guid.NewGuid();
        AddProduct(ctx, pid, ProductState.Draft, "nolot");
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var dto = (await sut.GetAllWithImagesAsync(false)).Single();

        Assert.Null(dto.PricedBy);
        Assert.Null(dto.PricedAt);
    }

    [Fact]
    public async Task UTCID08_OnePricingHistory_AppliesSuggestedPriceFields()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var pid = Guid.NewGuid();
        AddProduct(ctx, pid, ProductState.Draft, "priced");
        var lot = new StockLot
        {
            LotId = Guid.NewGuid(),
            ProductId = pid,
            UnitId = UnitId,
            ExpiryDate = T0.AddMonths(6),
            ManufactureDate = T0,
            Quantity = 5,
            OriginalUnitPrice = 10,
            SuggestedUnitPrice = 9,
            Weight = 1,
            Status = ProductState.Published,
            CreatedAt = T0,
            UpdatedAt = T0
        };
        ctx.StockLots.Add(lot);
        ctx.PricingHistories.Add(new PricingHistory
        {
            AIPriceId = Guid.NewGuid(),
            LotId = lot.LotId,
            SuggestedPrice = 7.77m,
            AIConfidence = 0.93m,
            ConfirmedBy = "staff-a",
            ConfirmedAt = T0.AddDays(2),
            CreatedAt = T0.AddDays(1)
        });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var dto = (await sut.GetAllWithImagesAsync(false)).Single();

        Assert.Equal(7.77m, dto.SuggestedPrice);
        Assert.Equal("staff-a", dto.PricedBy);
        Assert.NotNull(dto.PricedAt);
    }

    [Fact]
    public async Task UTCID09_TwoPricingHistories_LatestConfirmedWins()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var pid = Guid.NewGuid();
        AddProduct(ctx, pid, ProductState.Draft, "doublePrice");
        var lot = new StockLot
        {
            LotId = Guid.NewGuid(),
            ProductId = pid,
            UnitId = UnitId,
            ExpiryDate = T0.AddMonths(6),
            ManufactureDate = T0,
            Quantity = 1,
            OriginalUnitPrice = 10,
            SuggestedUnitPrice = 9,
            Weight = 1,
            Status = ProductState.Published,
            CreatedAt = T0,
            UpdatedAt = T0
        };
        ctx.StockLots.Add(lot);
        ctx.PricingHistories.AddRange(
            new PricingHistory
            {
                AIPriceId = Guid.NewGuid(),
                LotId = lot.LotId,
                SuggestedPrice = 4m,
                AIConfidence = 0.8m,
                CreatedAt = T0.AddDays(1),
                ConfirmedAt = T0.AddDays(2),
                ConfirmedBy = "old"
            },
            new PricingHistory
            {
                AIPriceId = Guid.NewGuid(),
                LotId = lot.LotId,
                SuggestedPrice = 8.5m,
                AIConfidence = 0.92m,
                CreatedAt = T0.AddHours(36),
                ConfirmedAt = T0.AddDays(10),
                ConfirmedBy = "new"
            });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var dto = (await sut.GetAllWithImagesAsync(false)).Single();

        Assert.Equal(8.5m, dto.SuggestedPrice);
        Assert.Equal("new", dto.PricedBy);
    }

    [Fact]
    public async Task UTCID10_Published_WithOneImage_ReturnsSingleVisibleDto()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var pid = Guid.NewGuid();
        AddProduct(ctx, pid, ProductState.Published, "pub-visible");
        ctx.ProductImages.Add(new ProductImage
        {
            ProductImageId = Guid.NewGuid(),
            ProductId = pid,
            ImageUrl = "main.pub.jpg",
            CreatedAt = T0,
            IsPrimary = true
        });
        await ctx.SaveChangesAsync();

        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var list = (await sut.GetAllWithImagesAsync(false)).ToList();

        Assert.Single(list);
        Assert.Equal(ProductState.Published, list[0].Status);
        Assert.Equal("main.pub.jpg", list[0].MainImageUrl);
    }
}
