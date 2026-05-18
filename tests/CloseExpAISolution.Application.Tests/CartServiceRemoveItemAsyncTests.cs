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
/// FN019 — UTCID01–UTCID07 per <c>.github/instructions/remove-cart-item-async-test-sheet.md</c>
/// (<see cref="CartService.RemoveItemAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~CartServiceRemoveItemAsyncTests"</c>
/// </summary>
public sealed class CartServiceRemoveItemAsyncTests : IDisposable
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
    public async Task UTCID01_WrongCartItemIdWhileCartHasLines_ThrowsKeyNotFoundException()
    {
        var presentId = Guid.NewGuid();
        var wrongId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(DateTime.UtcNow, new CartLineSeed(presentId, lotId, 1)));

        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.RemoveItemAsync(_userId, wrongId, CancellationToken.None));

        Assert.Contains("cart item", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID02_NoStoredCart_ThrowsKeyNotFoundException()
    {
        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.RemoveItemAsync(_userId, Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("cart item", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID03_InvalidJson_ThrowsKeyNotFoundException()
    {
        SeedCartJson(_userId, "{ broken-json ");
        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.RemoveItemAsync(_userId, Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("cart item", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID04_SingleLine_RemoveYieldsEmptyCart_TotalsZero_UpdatedAtForward()
    {
        var cartItemId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var staleUpdatedAt = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc);
        SeedCartJson(_userId, SerializeRedisCartPayload(staleUpdatedAt, new CartLineSeed(cartItemId, lotId, 3)));

        var lot = new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            ExpiryDate = DateTime.UtcNow.AddDays(1),
            FinalUnitPrice = 5m,
            SuggestedUnitPrice = 5m
        };
        var uow = CreateUowMock([lot], [new Product { ProductId = productId, Name = "A" }]);
        var sut = CreateSut(uow.Object);

        var before = DateTime.UtcNow;
        var result = await sut.RemoveItemAsync(_userId, cartItemId, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0m, result.TotalAmount);
        Assert.True(result.UpdatedAt >= before.AddSeconds(-2));
        Assert.True(result.UpdatedAt > staleUpdatedAt);

        var reread = await sut.GetMyCartAsync(_userId, CancellationToken.None);
        Assert.Empty(reread.Items);
    }

    [Fact]
    public async Task UTCID05_MultipleLines_RemoveMiddle_NonTargetsUnchanged_UpdatedAtForward()
    {
        var midId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var lastId = Guid.NewGuid();
        var lot1 = Guid.NewGuid();
        var lot2 = Guid.NewGuid();
        var lot3 = Guid.NewGuid();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var seedAt = DateTime.UtcNow.AddHours(-1);
        SeedCartJson(_userId, SerializeRedisCartPayload(
            seedAt,
            new CartLineSeed(firstId, lot1, 1),
            new CartLineSeed(midId, lot2, 2),
            new CartLineSeed(lastId, lot3, 3)));

        var lots = new[]
        {
            new StockLot { LotId = lot1, ProductId = p1, ExpiryDate = DateTime.UtcNow, FinalUnitPrice = 1m, SuggestedUnitPrice = 1m },
            new StockLot { LotId = lot2, ProductId = p2, ExpiryDate = DateTime.UtcNow, FinalUnitPrice = 2m, SuggestedUnitPrice = 2m },
            new StockLot { LotId = lot3, ProductId = p3, ExpiryDate = DateTime.UtcNow, FinalUnitPrice = 3m, SuggestedUnitPrice = 3m }
        };
        var products = new[]
        {
            new Product { ProductId = p1, Name = "X" },
            new Product { ProductId = p2, Name = "Y" },
            new Product { ProductId = p3, Name = "Z" }
        };
        var uow = CreateUowMock(lots, products);
        var sut = CreateSut(uow.Object);

        var before = DateTime.UtcNow;
        var result = await sut.RemoveItemAsync(_userId, midId, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.DoesNotContain(result.Items, i => i.CartItemId == midId);
        Assert.Equal(1, result.Items.Single(i => i.CartItemId == firstId).Quantity);
        Assert.Equal(3, result.Items.Single(i => i.CartItemId == lastId).Quantity);
        Assert.Equal(1m * 1 + 3m * 3, result.TotalAmount);
        Assert.True(result.UpdatedAt > seedAt);
        Assert.True(result.UpdatedAt >= before.AddSeconds(-2));
    }

    [Fact]
    public async Task UTCID06_DuplicateCartItemId_RemoveAll_BothDropped()
    {
        var dupId = Guid.NewGuid();
        var lotA = Guid.NewGuid();
        var lotB = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(
            DateTime.UtcNow,
            new CartLineSeed(dupId, lotA, 1),
            new CartLineSeed(dupId, lotB, 2)));

        var uow = CreateUowMock([], []);
        var sut = CreateSut(uow.Object);

        var result = await sut.RemoveItemAsync(_userId, dupId, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0m, result.TotalAmount);
    }

    [Fact]
    public async Task UTCID07_RemovePublishedLine_SurvivorOrphanLot_MappedNA()
    {
        var targetId = Guid.NewGuid();
        var survivorId = Guid.NewGuid();
        var lotPublished = Guid.NewGuid();
        var lotOrphan = Guid.NewGuid();
        var productId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(
            DateTime.UtcNow,
            new CartLineSeed(targetId, lotPublished, 1),
            new CartLineSeed(survivorId, lotOrphan, 4)));

        var published = new StockLot
        {
            LotId = lotPublished,
            ProductId = productId,
            ExpiryDate = DateTime.UtcNow,
            FinalUnitPrice = 10m,
            SuggestedUnitPrice = 10m
        };
        var uow = CreateUowMock([published], [new Product { ProductId = productId, Name = "Ok" }]);
        var sut = CreateSut(uow.Object);

        var result = await sut.RemoveItemAsync(_userId, targetId, CancellationToken.None);

        var line = Assert.Single(result.Items);
        Assert.Equal(survivorId, line.CartItemId);
        Assert.Equal(lotOrphan, line.LotId);
        Assert.Equal(0m, line.UnitPrice);
        Assert.Equal(0m, line.LineTotal);
        Assert.Equal("N/A", line.ProductName);
        Assert.Equal(4, line.Quantity);
    }
}
