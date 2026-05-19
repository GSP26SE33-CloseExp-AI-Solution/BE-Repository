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
/// FN015 — UTCID01–UTCID10 per <c>.github/instructions/update-product-async-test-sheet.md</c>
/// (<see cref="ProductService.UpdateProductAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~ProductServiceUpdateProductAsyncTests"</c>
/// </summary>
public sealed class ProductServiceUpdateProductAsyncTests : IDisposable
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private static readonly Guid SupermarketId = Guid.Parse("10000000-0000-4000-a015-000000000015");
    private static readonly Guid UnitIdSeed = Guid.Parse("20000000-0000-4000-a015-000000000015");
    private static readonly DateTime SeedTime = new(2026, 5, 10, 8, 0, 0, DateTimeKind.Utc);

    public ProductServiceUpdateProductAsyncTests() => _conn.Open();

    public void Dispose() => _conn.Dispose();

    private ApplicationDbContext CreateContext()
    {
        var opt = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_conn).Options;
        var ctx = new ApplicationDbContext(opt);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private void SeedBaseline(ApplicationDbContext ctx)
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
            CreatedAt = SeedTime
        });
        ctx.UnitOfMeasures.Add(new UnitOfMeasure
        {
            UnitId = UnitIdSeed,
            Name = "u",
            Type = "mass",
            Symbol = "kg",
            ConversionRate = 1m,
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime
        });
        ctx.SaveChanges();
    }

    private static Product ExistingWithDetail(Guid superId, Guid productId)
    {
        return new Product
        {
            ProductId = productId,
            SupermarketId = superId,
            UnitId = UnitIdSeed,
            Barcode = "__bc_upd_" + productId.ToString("N")[..10],
            Sku = "sk",
            Status = ProductState.Draft,
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime,
            CreatedBy = "t",
            ProductDetail = new ProductDetail
            {
                ProductDetailId = Guid.NewGuid(),
                ProductId = productId,
                Brand = "OldBrand",
                Description = "Old"
            }
        };
    }

    private static UpdateProductRequestDto ReqFrom(Product target, Guid superId)
    {
        return new UpdateProductRequestDto
        {
            SupermarketId = superId,
            Name = target.Name,
            Barcode = target.Barcode,
            Type = ProductType.Standard,
            Sku = target.Sku,
            Status = target.Status,
            ResponsibleOrg = "r",
            CategoryName = "",
            isFeatured = false,
            Detail = new ProductDetailRequestDto
            {
                Brand = "OldBrand",
                Description = "Old"
            }
        };
    }

    [Fact]
    public async Task UTCID01_MissingProduct_ThrowsKeyNotFound_WithId()
    {
        await using var ctx = CreateContext();
        SeedBaseline(ctx);
        var sut = ProductServiceTestInfrastructure.CreateProductService(ctx);
        var id = Guid.NewGuid();
        var req = ReqFrom(new Product
            {
                ProductId = id,
                SupermarketId = SupermarketId,
                UnitId = UnitIdSeed,
                Name = "x",
                Barcode = "b",
                Sku = "s",
                Status = ProductState.Draft,
                CreatedAt = SeedTime,
                UpdatedAt = SeedTime,
                CreatedBy = ""
            },
            SupermarketId);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.UpdateProductAsync(id, req, CancellationToken.None));

        Assert.Contains(id.ToString(), ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID02_HappyPath_UpdateDetail_UseUpdateNotAdd()
    {
        await using var ctx = CreateContext();
        SeedBaseline(ctx);
        var pid = Guid.NewGuid();
        ctx.Products.Add(ExistingWithDetail(SupermarketId, pid));
        await ctx.SaveChangesAsync();

        var detIdBefore = ctx.ProductDetails.Single(d => d.ProductId == pid).ProductDetailId;
        var req = ReqFrom(ctx.Products.Include(p => p.ProductDetail).Single(p => p.ProductId == pid), SupermarketId);
        req.Detail!.Brand = "NewBrand";

        await ProductServiceTestInfrastructure.CreateProductService(ctx).UpdateProductAsync(pid, req, CancellationToken.None);

        var detAfter = ctx.ProductDetails.AsNoTracking().Single(d => d.ProductId == pid);
        Assert.Equal(detIdBefore, detAfter.ProductDetailId);
        Assert.Equal("NewBrand", detAfter.Brand);
    }

    [Fact]
    public async Task UTCID03_CategoryNameMatch_AssignsCategoryId()
    {
        await using var ctx = CreateContext();
        SeedBaseline(ctx);
        var catId = Guid.NewGuid();
        ctx.Categories.Add(new Category { CategoryId = catId, Name = "DairyProd", IsActive = true });
        var pid = Guid.NewGuid();
        ctx.Products.Add(ExistingWithDetail(SupermarketId, pid));
        await ctx.SaveChangesAsync();

        var req =
            ReqFrom(ctx.Products.Include(p => p.ProductDetail).Single(p => p.ProductId == pid), SupermarketId);
        req.CategoryName = "dairyprod";

        await ProductServiceTestInfrastructure.CreateProductService(ctx).UpdateProductAsync(pid, req, CancellationToken.None);

        Assert.Equal(catId, ctx.Products.AsNoTracking().Single(p => p.ProductId == pid).CategoryId);
    }

    [Fact]
    public async Task UTCID04_UnknownCategory_NoAssignment()
    {
        await using var ctx = CreateContext();
        SeedBaseline(ctx);
        var pid = Guid.NewGuid();
        ctx.Products.Add(ExistingWithDetail(SupermarketId, pid));
        await ctx.SaveChangesAsync();

        var req =
            ReqFrom(ctx.Products.Include(p => p.ProductDetail).Single(p => p.ProductId == pid), SupermarketId);
        req.CategoryName = "___missing___";

        await ProductServiceTestInfrastructure.CreateProductService(ctx).UpdateProductAsync(pid, req, CancellationToken.None);

        Assert.Null(ctx.Products.AsNoTracking().Single(p => p.ProductId == pid).CategoryId);
    }

    [Fact]
    public async Task UTCID05_WhitespaceCategory_NoLookup()
    {
        await using var ctx = CreateContext();
        SeedBaseline(ctx);
        var catId = Guid.NewGuid();
        ctx.Categories.Add(new Category { CategoryId = catId, Name = "X", IsActive = true });
        var pid = Guid.NewGuid();
        ctx.Products.Add(ExistingWithDetail(SupermarketId, pid));
        await ctx.SaveChangesAsync();

        var req =
            ReqFrom(ctx.Products.Include(p => p.ProductDetail).Single(p => p.ProductId == pid), SupermarketId);
        req.CategoryName = "   ";

        await ProductServiceTestInfrastructure.CreateProductService(ctx).UpdateProductAsync(pid, req, CancellationToken.None);

        Assert.Null(ctx.Products.AsNoTracking().Single(p => p.ProductId == pid).CategoryId);
    }

    [Fact]
    public async Task UTCID06_NoProductDetail_Id_AddDetailRow()
    {
        await using var ctx = CreateContext();
        SeedBaseline(ctx);
        var pid = Guid.NewGuid();
        ctx.Products.Add(new Product
        {
            ProductId = pid,
            SupermarketId = SupermarketId,
            UnitId = UnitIdSeed,
            Name = "NoDet",
            Barcode = "Nd" + pid.ToString("N"),
            Sku = "s",
            Status = ProductState.Draft,
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime,
            CreatedBy = "",
            ProductDetail = null
        });
        await ctx.SaveChangesAsync();

        var req =
            ReqFrom(await ctx.Products.AsNoTracking().SingleAsync(p => p.ProductId == pid), SupermarketId);
        req.Detail = new ProductDetailRequestDto { Brand = "Inserted", Description = "D2" };

        await ProductServiceTestInfrastructure.CreateProductService(ctx).UpdateProductAsync(pid, req, CancellationToken.None);

        var det = ctx.ProductDetails.Single(d => d.ProductId == pid);
        Assert.Equal("Inserted", det.Brand);
    }

    [Fact]
    public async Task UTCID07_StatusMapped_ToEnumOnEntity()
    {
        await using var ctx = CreateContext();
        SeedBaseline(ctx);
        var pid = Guid.NewGuid();
        ctx.Products.Add(ExistingWithDetail(SupermarketId, pid));
        await ctx.SaveChangesAsync();

        var req =
            ReqFrom(ctx.Products.Include(p => p.ProductDetail).Single(p => p.ProductId == pid), SupermarketId);
        req.Status = ProductState.Published;

        await ProductServiceTestInfrastructure.CreateProductService(ctx).UpdateProductAsync(pid, req, CancellationToken.None);

        Assert.Equal(ProductState.Published, ctx.Products.AsNoTracking().Single(p => p.ProductId == pid).Status);
    }

    [Fact]
    public async Task UTCID08_CustomToken_SingleSaveReceived()
    {
        await using var ctx = CreateContext();
        SeedBaseline(ctx);
        var pid = Guid.NewGuid();
        ctx.Products.Add(ExistingWithDetail(SupermarketId, pid));
        await ctx.SaveChangesAsync();

        var inner = new UnitOfWork(ctx);
        var spy = new Mock<IUnitOfWork>();
        spy.Setup(u => u.ProductRepository).Returns(inner.ProductRepository);
        spy.Setup(u => u.Repository<ProductDetail>()).Returns(inner.Repository<ProductDetail>());

        var tokens = new List<CancellationToken>();
        spy.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                tokens.Add(ct);
                return await inner.SaveChangesAsync(ct);
            });

        using var ts = new CancellationTokenSource();
        var token = ts.Token;

        var mapper = ProductServiceTestInfrastructure.CreateMapper();
        var sut = new ProductService(spy.Object, ctx, mapper, ProductServiceTestInfrastructure.LooseAiClient(),
            NullLogger<ProductService>.Instance);

        var req =
            ReqFrom(await ctx.Products.Include(p => p.ProductDetail).SingleAsync(p => p.ProductId == pid), SupermarketId);
        await sut.UpdateProductAsync(pid, req, token);

        Assert.Single(tokens);
        Assert.Equal(token, tokens[0]);
    }

    [Fact]
    public async Task UTCID09_SparseDetail_PersistsNulls()
    {
        await using var ctx = CreateContext();
        SeedBaseline(ctx);
        var pid = Guid.NewGuid();
        ctx.Products.Add(ExistingWithDetail(SupermarketId, pid));
        await ctx.SaveChangesAsync();

        var req =
            ReqFrom(ctx.Products.Include(p => p.ProductDetail).Single(p => p.ProductId == pid), SupermarketId);
        req.Detail = new ProductDetailRequestDto();

        await ProductServiceTestInfrastructure.CreateProductService(ctx).UpdateProductAsync(pid, req, CancellationToken.None);

        var det = ctx.ProductDetails.AsNoTracking().Single(d => d.ProductId == pid);
        Assert.Null(det.Brand);
        Assert.Null(det.Description);
    }

    [Fact]
    public async Task UTCID10_IsFeaturedTrue_OnProduct()
    {
        await using var ctx = CreateContext();
        SeedBaseline(ctx);
        var pid = Guid.NewGuid();
        ctx.Products.Add(ExistingWithDetail(SupermarketId, pid));
        await ctx.SaveChangesAsync();

        var req =
            ReqFrom(ctx.Products.Include(p => p.ProductDetail).Single(p => p.ProductId == pid), SupermarketId);
        req.isFeatured = true;

        await ProductServiceTestInfrastructure.CreateProductService(ctx).UpdateProductAsync(pid, req, CancellationToken.None);

        Assert.True(ctx.Products.AsNoTracking().Single(p => p.ProductId == pid).IsFeatured);
    }
}
