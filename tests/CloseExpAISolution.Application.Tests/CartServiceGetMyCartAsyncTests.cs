using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Moq;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN016 — UTCID01–UTCID10 per <c>.github/instructions/get-my-cart-async-test-sheet.md</c>
/// (<see cref="CartService.GetMyCartAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~CartServiceGetMyCartAsyncTests"</c>
/// </summary>
public sealed class CartServiceGetMyCartAsyncTests : IDisposable
{
    private readonly Guid _userId = Guid.NewGuid();

    private static ConcurrentDictionary<string, string> GetInMemoryCartStore()
    {
        var field = typeof(CartService).GetField("InMemoryCartStore", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("CartService.InMemoryCartStore field not found.");
        return (ConcurrentDictionary<string, string>)field.GetValue(null)!;
    }

    private static string CartKey(Guid userId) => $"cart:{userId:D}";

    private void SeedCartJson(Guid userId, string json)
    {
        GetInMemoryCartStore()[CartKey(userId)] = json;
    }

    private static string SerializeRedisCartPayload(DateTime updatedAt, params CartLineSeed[] lines)
    {
        var payload = new
        {
            UpdatedAt = updatedAt,
            Items = lines.Select(l => new
            {
                l.CartItemId,
                l.LotId,
                Quantity = l.Quantity
            }).ToList()
        };
        return JsonSerializer.Serialize(payload);
    }

    private sealed record CartLineSeed(Guid CartItemId, Guid LotId, decimal Quantity);

    public void Dispose()
    {
        GetInMemoryCartStore().TryRemove(CartKey(_userId), out _);
    }

    private static CartService CreateSut(IUnitOfWork uow) => new(uow, redis: null);

    private static Mock<IUnitOfWork> CreateUowMock(IReadOnlyList<StockLot> lots, IReadOnlyList<Product> products)
    {
        var uow = new Mock<IUnitOfWork>();

        var lotRepo = new Mock<IGenericRepository<StockLot>>();
        lotRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .Returns((Expression<Func<StockLot, bool>> pred) =>
                Task.FromResult<IEnumerable<StockLot>>(lots.Where(pred.Compile()).ToList()));
        uow.Setup(x => x.Repository<StockLot>()).Returns(lotRepo.Object);

        var prodRepo = new Mock<IGenericRepository<Product>>();
        prodRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .Returns((Expression<Func<Product, bool>> pred) =>
                Task.FromResult<IEnumerable<Product>>(products.Where(pred.Compile()).ToList()));
        uow.Setup(x => x.Repository<Product>()).Returns(prodRepo.Object);

        return uow;
    }

    [Fact]
    public async Task UTCID01_NoStoredCart_ReturnsEmptyCart()
    {
        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var result = await sut.GetMyCartAsync(_userId, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0m, result.TotalAmount);
        Assert.Equal(_userId, result.UserId);
    }

    [Fact]
    public async Task UTCID02_OneLine_LotAndProductFound_UsesFinalUnitPrice()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        var expiry = DateTime.UtcNow.Date.AddDays(7);
        var updatedAt = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        SeedCartJson(_userId, SerializeRedisCartPayload(updatedAt, new CartLineSeed(cartItemId, lotId, 3)));

        var lot = new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            ExpiryDate = expiry,
            FinalUnitPrice = 12.5m,
            SuggestedUnitPrice = 99m
        };
        var product = new Product { ProductId = productId, Name = "Milk" };
        var uow = CreateUowMock([lot], [product]);
        var sut = CreateSut(uow.Object);

        var result = await sut.GetMyCartAsync(_userId, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("Milk", item.ProductName);
        Assert.Equal(12.5m, item.UnitPrice);
        Assert.Equal(37.5m, item.LineTotal);
        Assert.Equal(37.5m, result.TotalAmount);
        Assert.Equal(1, result.TotalItems);
    }

    [Fact]
    public async Task UTCID03_FinalUnitPriceNull_UsesSuggestedUnitPrice()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(DateTime.UtcNow, new CartLineSeed(cartItemId, lotId, 2)));

        var lot = new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            ExpiryDate = DateTime.UtcNow,
            FinalUnitPrice = null,
            SuggestedUnitPrice = 8m
        };
        var product = new Product { ProductId = productId, Name = "Tea" };
        var uow = CreateUowMock([lot], [product]);
        var sut = CreateSut(uow.Object);

        var result = await sut.GetMyCartAsync(_userId, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(8m, item.UnitPrice);
        Assert.Equal(16m, item.LineTotal);
    }

    [Fact]
    public async Task UTCID04_OrphanLot_NotInDb_ZeroUnitPrice_NA_Product()
    {
        var orphanLotId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(DateTime.UtcNow, new CartLineSeed(cartItemId, orphanLotId, 5)));

        var prodRepo = new Mock<IGenericRepository<Product>>();
        prodRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(Array.Empty<Product>());
        var lotRepo = new Mock<IGenericRepository<StockLot>>();
        lotRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .ReturnsAsync(Array.Empty<StockLot>());
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<StockLot>()).Returns(lotRepo.Object);
        uow.Setup(x => x.Repository<Product>()).Returns(prodRepo.Object);

        var sut = CreateSut(uow.Object);

        var result = await sut.GetMyCartAsync(_userId, CancellationToken.None);

        var line = Assert.Single(result.Items);
        Assert.Equal(0m, line.UnitPrice);
        Assert.Equal(0m, line.LineTotal);
        Assert.Equal("N/A", line.ProductName);
        Assert.Equal(Guid.Empty, line.ProductId);
        prodRepo.Verify(
            r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()),
            Times.Never);
    }

    [Fact]
    public async Task UTCID05_LotExists_ProductMissing_NA_Name_ProductIdFromLot()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(DateTime.UtcNow, new CartLineSeed(cartItemId, lotId, 1)));

        var lot = new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            ExpiryDate = DateTime.UtcNow,
            FinalUnitPrice = 5m,
            SuggestedUnitPrice = 5m
        };
        var uow = CreateUowMock([lot], []);
        var sut = CreateSut(uow.Object);

        var result = await sut.GetMyCartAsync(_userId, CancellationToken.None);

        var line = Assert.Single(result.Items);
        Assert.Equal("N/A", line.ProductName);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal(5m, line.UnitPrice);
    }

    [Fact]
    public async Task UTCID06_TwoDistinctLots_TotalAmountSummed()
    {
        var lotA = Guid.NewGuid();
        var lotB = Guid.NewGuid();
        var pidA = Guid.NewGuid();
        var pidB = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(
            DateTime.UtcNow,
            new CartLineSeed(Guid.NewGuid(), lotA, 2),
            new CartLineSeed(Guid.NewGuid(), lotB, 3)));

        var lots = new[]
        {
            new StockLot { LotId = lotA, ProductId = pidA, ExpiryDate = DateTime.UtcNow, FinalUnitPrice = 10m, SuggestedUnitPrice = 10m },
            new StockLot { LotId = lotB, ProductId = pidB, ExpiryDate = DateTime.UtcNow, FinalUnitPrice = 4m, SuggestedUnitPrice = 4m }
        };
        var products = new[]
        {
            new Product { ProductId = pidA, Name = "A" },
            new Product { ProductId = pidB, Name = "B" }
        };
        var uow = CreateUowMock(lots, products);
        var sut = CreateSut(uow.Object);

        var result = await sut.GetMyCartAsync(_userId, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2 * 10m + 3 * 4m, result.TotalAmount);
    }

    [Fact]
    public async Task UTCID07_TwoLinesSameLotId_SeparateRowsSummedTotal()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var unitPrice = 7m;
        SeedCartJson(_userId, SerializeRedisCartPayload(
            DateTime.UtcNow,
            new CartLineSeed(Guid.NewGuid(), lotId, 1),
            new CartLineSeed(Guid.NewGuid(), lotId, 2)));

        var lot = new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            ExpiryDate = DateTime.UtcNow,
            FinalUnitPrice = unitPrice,
            SuggestedUnitPrice = unitPrice
        };
        var uow = CreateUowMock([lot], [new Product { ProductId = productId, Name = "SameLot" }]);
        var sut = CreateSut(uow.Object);

        var result = await sut.GetMyCartAsync(_userId, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(unitPrice * 1 + unitPrice * 2, result.TotalAmount);
    }

    [Fact]
    public async Task UTCID08_InvalidJson_ReturnsEmptyCart()
    {
        SeedCartJson(_userId, "{ not-valid-json ");
        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var result = await sut.GetMyCartAsync(_userId, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
    }

    [Fact]
    public async Task UTCID09_WhitespacePayload_ReturnsEmptyCart()
    {
        SeedCartJson(_userId, "   \t\n");
        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var result = await sut.GetMyCartAsync(_userId, CancellationToken.None);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task UTCID10_UpdatedAt_RoundTripsFromPayload()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        var updatedAt = new DateTime(2026, 5, 1, 8, 30, 0, DateTimeKind.Utc);
        SeedCartJson(_userId, SerializeRedisCartPayload(updatedAt, new CartLineSeed(cartItemId, lotId, 1)));

        var lot = new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            ExpiryDate = DateTime.UtcNow,
            FinalUnitPrice = 1m,
            SuggestedUnitPrice = 1m
        };
        var uow = CreateUowMock([lot], [new Product { ProductId = productId, Name = "X" }]);
        var sut = CreateSut(uow.Object);

        var result = await sut.GetMyCartAsync(_userId, CancellationToken.None);

        Assert.Equal(updatedAt, result.UpdatedAt);
    }
}
