using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Moq;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN018 — UTCID01–UTCID09 per <c>.github/instructions/update-cart-item-async-test-sheet.md</c>
/// (<see cref="CartService.UpdateItemAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~CartServiceUpdateItemAsyncTests"</c>
/// </summary>
public sealed class CartServiceUpdateItemAsyncTests : IDisposable
{
    private readonly Guid _userId = Guid.NewGuid();

    private static ConcurrentDictionary<string, string> GetInMemoryCartStore()
    {
        var field = typeof(CartService).GetField("InMemoryCartStore", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("CartService.InMemoryCartStore field not found.");
        return (ConcurrentDictionary<string, string>)field.GetValue(null)!;
    }

    private static string CartKey(Guid userId) => $"cart:{userId:D}";

    private void SeedCartJson(Guid userId, string json) => GetInMemoryCartStore()[CartKey(userId)] = json;

    private sealed record CartLineSeed(Guid CartItemId, Guid LotId, decimal Quantity);

    private static string SerializeRedisCartPayload(DateTime updatedAt, params CartLineSeed[] lines)
    {
        var payload = new
        {
            UpdatedAt = updatedAt,
            Items = lines.Select(l => new { l.CartItemId, l.LotId, Quantity = l.Quantity }).ToList()
        };
        return JsonSerializer.Serialize(payload);
    }

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
    public async Task UTCID01_NonPositiveQuantity_ThrowsArgumentException()
    {
        var cartItemId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(DateTime.UtcNow, new CartLineSeed(cartItemId, lotId, 1)));

        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.UpdateItemAsync(_userId, cartItemId, new UpdateCartItemRequestDto { Quantity = 0 }, CancellationToken.None));

        Assert.Contains("greater than 0", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID02_CartItemIdNotInCart_ThrowsKeyNotFoundException()
    {
        var lineItemId = Guid.NewGuid();
        var wrongId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(DateTime.UtcNow, new CartLineSeed(lineItemId, lotId, 2)));

        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.UpdateItemAsync(_userId, wrongId, new UpdateCartItemRequestDto { Quantity = 1 }, CancellationToken.None));

        Assert.Contains("cart item", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID03_SingleLine_UpdatesQuantityAndTotals()
    {
        var cartItemId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(
            DateTime.UtcNow.AddMinutes(-30),
            new CartLineSeed(cartItemId, lotId, 3)));

        var lot = new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            ExpiryDate = DateTime.UtcNow.AddDays(1),
            FinalUnitPrice = 10m,
            SuggestedUnitPrice = 9m
        };
        var uow = CreateUowMock([lot], [new Product { ProductId = productId, Name = "P" }]);
        var sut = CreateSut(uow.Object);

        var result = await sut.UpdateItemAsync(_userId, cartItemId, new UpdateCartItemRequestDto { Quantity = 7 }, CancellationToken.None);

        var line = Assert.Single(result.Items);
        Assert.Equal(7, line.Quantity);
        Assert.Equal(70m, line.LineTotal);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(70m, result.TotalAmount);
    }

    [Fact]
    public async Task UTCID04_TwoLines_UpdatesOnlyTarget_OtherQuantityUnchanged()
    {
        var targetId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var lotA = Guid.NewGuid();
        var lotB = Guid.NewGuid();
        var pidA = Guid.NewGuid();
        var pidB = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(
            DateTime.UtcNow,
            new CartLineSeed(targetId, lotA, 2),
            new CartLineSeed(otherId, lotB, 5)));

        var lots = new[]
        {
            new StockLot
            {
                LotId = lotA, ProductId = pidA, ExpiryDate = DateTime.UtcNow, FinalUnitPrice = 10m, SuggestedUnitPrice = 10m
            },
            new StockLot
            {
                LotId = lotB, ProductId = pidB, ExpiryDate = DateTime.UtcNow, FinalUnitPrice = 4m, SuggestedUnitPrice = 4m
            }
        };
        var products = new[]
        {
            new Product { ProductId = pidA, Name = "A" },
            new Product { ProductId = pidB, Name = "B" }
        };
        var uow = CreateUowMock(lots, products);
        var sut = CreateSut(uow.Object);

        var result = await sut.UpdateItemAsync(_userId, targetId, new UpdateCartItemRequestDto { Quantity = 3 }, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        var updated = result.Items.Single(i => i.CartItemId == targetId);
        var other = result.Items.Single(i => i.CartItemId == otherId);
        Assert.Equal(3, updated.Quantity);
        Assert.Equal(5, other.Quantity);
        Assert.Equal(10m * 3 + 4m * 5, result.TotalAmount);
    }

    [Fact]
    public async Task UTCID05_BoundaryQuantity_PointZeroZeroZeroOne_ServicePath()
    {
        var cartItemId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(DateTime.UtcNow, new CartLineSeed(cartItemId, lotId, 1)));

        var lot = new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            ExpiryDate = DateTime.UtcNow.AddDays(1),
            FinalUnitPrice = 100m,
            SuggestedUnitPrice = 100m
        };
        var uow = CreateUowMock([lot], [new Product { ProductId = productId, Name = "Grain" }]);
        var sut = CreateSut(uow.Object);

        var result = await sut.UpdateItemAsync(_userId, cartItemId, new UpdateCartItemRequestDto { Quantity = 0.0001m }, CancellationToken.None);

        var line = Assert.Single(result.Items);
        Assert.Equal(0.0001m, line.Quantity);
        Assert.Equal(0.01m, line.LineTotal);
    }

    [Fact]
    public async Task UTCID06_InvalidJsonEmptyCart_ThrowsKeyNotFoundException()
    {
        SeedCartJson(_userId, "{ not-json ");
        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.UpdateItemAsync(_userId, Guid.NewGuid(), new UpdateCartItemRequestDto { Quantity = 1 }, CancellationToken.None));

        Assert.Contains("cart item", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID07_NoStoredCart_ThrowsKeyNotFoundException()
    {
        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.UpdateItemAsync(_userId, Guid.NewGuid(), new UpdateCartItemRequestDto { Quantity = 1 }, CancellationToken.None));

        Assert.Contains("cart item", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID08_OrphanLotAfterUpdate_MapShowsZeroUnitPriceForThatLine()
    {
        var cartItemId = Guid.NewGuid();
        var orphanLotId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(DateTime.UtcNow, new CartLineSeed(cartItemId, orphanLotId, 2)));

        var lotRepo = new Mock<IGenericRepository<StockLot>>();
        lotRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .ReturnsAsync(Array.Empty<StockLot>());
        var prodRepo = new Mock<IGenericRepository<Product>>();
        prodRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(Array.Empty<Product>());
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<StockLot>()).Returns(lotRepo.Object);
        uow.Setup(x => x.Repository<Product>()).Returns(prodRepo.Object);

        var sut = CreateSut(uow.Object);

        var result = await sut.UpdateItemAsync(_userId, cartItemId, new UpdateCartItemRequestDto { Quantity = 4 }, CancellationToken.None);

        var line = Assert.Single(result.Items);
        Assert.Equal(4, line.Quantity);
        Assert.Equal(0m, line.UnitPrice);
        Assert.Equal(0m, line.LineTotal);
        Assert.Equal("N/A", line.ProductName);
    }

    [Fact]
    public async Task UTCID09_UpdatedAt_NotBeforeSave_BeatsPreviousCartTimestamp()
    {
        var cartItemId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var staleUpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        SeedCartJson(_userId, SerializeRedisCartPayload(staleUpdatedAt, new CartLineSeed(cartItemId, lotId, 1)));

        var lot = new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            ExpiryDate = DateTime.UtcNow.AddDays(1),
            FinalUnitPrice = 2m,
            SuggestedUnitPrice = 2m
        };
        var uow = CreateUowMock([lot], [new Product { ProductId = productId, Name = "X" }]);
        var sut = CreateSut(uow.Object);

        var before = DateTime.UtcNow;
        var result = await sut.UpdateItemAsync(_userId, cartItemId, new UpdateCartItemRequestDto { Quantity = 9 }, CancellationToken.None);

        Assert.True(result.UpdatedAt >= before.AddSeconds(-2));
        Assert.True(result.UpdatedAt > staleUpdatedAt);

        var reread = await sut.GetMyCartAsync(_userId, CancellationToken.None);
        Assert.Equal(9, Assert.Single(reread.Items).Quantity);
    }
}
