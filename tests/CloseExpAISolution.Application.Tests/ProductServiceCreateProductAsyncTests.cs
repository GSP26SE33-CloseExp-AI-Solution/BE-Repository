using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN014 — UTCID01–UTCID09 per <c>.github/instructions/create-product-async-test-sheet.md</c>
/// (<see cref="ProductService.CreateProductAsync"/>). UTCID06 requires mocking post-save Include reload — skipped here.
/// Run: <c>dotnet test --filter "FullyQualifiedName~ProductServiceCreateProductAsyncTests"</c>
/// </summary>
public sealed class ProductServiceCreateProductAsyncTests : IDisposable
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");

    private static readonly Guid SupermarketId = Guid.Parse("10000000-0000-4000-a014-000000000014");
    private static readonly DateTime SeedTime = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    public ProductServiceCreateProductAsyncTests() => _conn.Open();

    public void Dispose() => _conn.Dispose();

    private ApplicationDbContext CreateContext()
    {
        var opt = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_conn).Options;
        var ctx = new ApplicationDbContext(opt);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static void SeedSupermarketUnit(ApplicationDbContext ctx)
    {
        ctx.Supermarkets.Add(new Supermarket
        {
            SupermarketId = SupermarketId,
            Name = "SM",
            Address = "a",
            Latitude = 0,
            Longitude = 0,
            ContactPhone = "1",
            Status = SupermarketState.Active,
            CreatedAt = SeedTime
        });
        ctx.UnitOfMeasures.Add(new UnitOfMeasure
        {
            UnitId = Guid.Empty,
            Name = "unit",
            Type = "mass",
            Symbol = "kg",
            ConversionRate = 1m,
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime
        });
        ctx.SaveChanges();
    }

    private static CreateProductRequestDto BaseReq(string barcodeSeed)
    {
        return new CreateProductRequestDto
        {
            SupermarketId = SupermarketId,
            Name = "Product " + barcodeSeed[..8],
            Barcode = "bc_" + barcodeSeed,
            CategoryName = "",
            Type = ProductType.Standard,
            Sku = "sku",
            ResponsibleOrg = "org",
            isFeatured = false,
            Detail = new ProductDetailRequestDto
            {
                Brand = "B",
                Description = "Desc",
                Ingredients = "[\"a\",\"b\"]"
            }
        };
    }

    [Fact]
    public async Task UTCID01_BaseCreate_ReturnsDto_NoCategory_NoPricing_NoImages()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var req = BaseReq(Guid.NewGuid().ToString("N"));
        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);

        var dto = await sut.CreateProductAsync(req, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, dto.ProductId);
        var reloaded = await ctx.Products.AsNoTracking()
            .Include(p => p.ProductDetail).FirstAsync(p => p.ProductId == dto.ProductId);
        Assert.Null(reloaded.CategoryId);
        Assert.Equal(ProductState.Hidden, reloaded.Status);
        Assert.NotNull(reloaded.ProductDetail);
        Assert.Equal("B", reloaded.ProductDetail!.Brand);
        Assert.Equal(0, dto.TotalImages);
        Assert.Null(dto.MainImageUrl);
        Assert.Null(dto.PricedAt);
        Assert.False(reloaded.IsFeatured);
    }

    [Fact]
    public async Task UTCID02_MatchingCategory_AssignsCategoryId_CaseInsensitive()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var catId = Guid.NewGuid();
        ctx.Categories.Add(new Category { CategoryId = catId, Name = "Snack", IsActive = true, IsFreshFood = false });
        await ctx.SaveChangesAsync();

        var req = BaseReq(Guid.NewGuid().ToString("N"));
        req.CategoryName = "snACK";
        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var dto = await sut.CreateProductAsync(req, CancellationToken.None);

        var saved = await ctx.Products.AsNoTracking().SingleAsync(p => p.ProductId == dto.ProductId);
        Assert.Equal(catId, saved.CategoryId);
    }

    [Fact]
    public async Task UTCID03_UnknownCategoryName_DoesNotSetCategoryId()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var req = BaseReq(Guid.NewGuid().ToString("N"));
        req.CategoryName = "__no_such_cat__";
        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var dto = await sut.CreateProductAsync(req, CancellationToken.None);

        Assert.Null(ctx.Products.AsNoTracking().Single(p => p.ProductId == dto.ProductId).CategoryId);
    }

    [Fact]
    public async Task UTCID04_WhitespaceCategoryName_NoLookup()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var catId = Guid.NewGuid();
        ctx.Categories.Add(new Category { CategoryId = catId, Name = "Snack", IsActive = true });
        await ctx.SaveChangesAsync();

        var req = BaseReq(Guid.NewGuid().ToString("N"));
        req.CategoryName = "  \t ";
        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var dto = await sut.CreateProductAsync(req, CancellationToken.None);

        Assert.Null(ctx.Products.AsNoTracking().Single(p => p.ProductId == dto.ProductId).CategoryId);
    }

    [Fact]
    public async Task UTCID05_SparseDetail_StillPersistedRow()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var req = BaseReq(Guid.NewGuid().ToString("N"));
        req.Detail = new ProductDetailRequestDto();
        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);

        var dto = await sut.CreateProductAsync(req, CancellationToken.None);

        var det = ctx.ProductDetails.AsNoTracking().Single(d => d.ProductId == dto.ProductId);
        Assert.Null(det.Brand);
        Assert.Null(det.Description);
    }

    [Fact(Skip =
        "Sheet UTC06: không thực hiện với SQLite thuần — cần chặt query Products.Include(...) sau hai lần SaveChanges.")]
    public void UTCID06_Reload_Returns_InvalidOperation_NotExecuted()
    {
    }

    [Fact]
    public async Task UTCID07_CustomCancellationToken_PropagatesToBothSaveCalls()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var inner = new UnitOfWork(ctx);
        var spy = new Mock<IUnitOfWork>();
        spy.Setup(u => u.ProductRepository).Returns(inner.ProductRepository);
        spy.Setup(u => u.Repository<ProductDetail>()).Returns(inner.Repository<ProductDetail>());

        var captured = new List<CancellationToken>();
        spy.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                captured.Add(ct);
                return await inner.SaveChangesAsync(ct);
            });

        using var src = new CancellationTokenSource();
        var token = src.Token;

        var mapper = ProductServiceTestInfrastructure.CreateMapper();
        var sut = new ProductService(spy.Object, ctx, mapper, ProductServiceTestInfrastructure.LooseAiClient(),
            NullLogger<ProductService>.Instance);
        await sut.CreateProductAsync(BaseReq(Guid.NewGuid().ToString("N")), token);

        Assert.Equal(2, captured.Count);
        Assert.True(captured.All(ct => ct == token));
    }

    [Fact]
    public async Task UTCID08_ProductCreated_Hidden_AndUtcTimestamps()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var before = DateTime.UtcNow.AddMinutes(-1);

        var dto = await sut.CreateProductAsync(BaseReq(Guid.NewGuid().ToString("N")), CancellationToken.None);

        var after = DateTime.UtcNow.AddMinutes(1);
        var p = ctx.Products.AsNoTracking().Single(x => x.ProductId == dto.ProductId);
        Assert.Equal(ProductState.Hidden, p.Status);
        Assert.InRange(p.CreatedAt, before, after);
        Assert.InRange(p.UpdatedAt, before, after);
    }

    [Fact]
    public async Task UTCID09_IsFeaturedTrue_Persisted_OnEntity_NotOnDtoConvention()
    {
        await using var ctx = CreateContext();
        SeedSupermarketUnit(ctx);
        var req = BaseReq(Guid.NewGuid().ToString("N"));
        req.isFeatured = true;
        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);

        var dto = await sut.CreateProductAsync(req, CancellationToken.None);

        var p = ctx.Products.AsNoTracking().Single(x => x.ProductId == dto.ProductId);
        Assert.True(p.IsFeatured);
    }
}
