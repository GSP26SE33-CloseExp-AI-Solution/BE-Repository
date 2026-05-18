using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Policies;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Moq;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN017 — UTCID01–UTCID10 per <c>.github/instructions/add-cart-item-async-test-sheet.md</c>
/// (<see cref="CartService.AddItemAsync"/>; UTCID06 uses <see cref="DailyExpiryOrderingPolicy"/> as documented on the sheet).
/// Run: <c>dotnet test --filter "FullyQualifiedName~CartServiceAddItemAsyncTests"</c>
/// </summary>
public sealed class CartServiceAddItemAsyncTests : IDisposable
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

    private static Mock<IUnitOfWork> CreateUowForAddItem(IReadOnlyList<StockLot> seedLots, IReadOnlyList<Product> seedProducts)
    {
        var list = seedLots.ToList();

        var uow = new Mock<IUnitOfWork>();
        var lotRepo = new Mock<IGenericRepository<StockLot>>();
        lotRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .ReturnsAsync((Expression<Func<StockLot, bool>> pred) => list.FirstOrDefault(pred.Compile()));

        lotRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .ReturnsAsync((Expression<Func<StockLot, bool>> pred) => list.Where(pred.Compile()).ToList());

        uow.Setup(x => x.Repository<StockLot>()).Returns(lotRepo.Object);

        var prods = seedProducts.ToList();
        var prodRepo = new Mock<IGenericRepository<Product>>();
        prodRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync((Expression<Func<Product, bool>> pred) => prods.Where(pred.Compile()).ToList());

        uow.Setup(x => x.Repository<Product>()).Returns(prodRepo.Object);
        return uow;
    }

    private StockLot PublishedLot(Guid lotId, Guid productId, decimal stockQty = 100m)
    {
        return new StockLot
        {
            LotId = lotId,
            ProductId = productId,
            Status = ProductState.Published,
            Quantity = stockQty,
            ExpiryDate = DateTime.UtcNow.AddDays(3),
            FinalUnitPrice = 10m,
            SuggestedUnitPrice = 9m
        };
    }

    [Fact]
    public async Task UTCID01_NonPositiveQuantity_ThrowsArgumentException()
    {
        var lid = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var uow = CreateUowForAddItem([PublishedLot(lid, pid)], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.AddItemAsync(_userId, new AddCartItemRequestDto { LotId = lid, Quantity = 0 }, CancellationToken.None));

        Assert.Contains("greater than 0", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID02_LotNotFound_ThrowsKeyNotFoundException()
    {
        var requestedLotId = Guid.NewGuid();
        var uow = CreateUowForAddItem([], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.AddItemAsync(_userId, new AddCartItemRequestDto { LotId = requestedLotId, Quantity = 1 }, CancellationToken.None));

        Assert.Contains("lô", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID03_StatusNotPublished_ThrowsInvalidOperationException()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var lot = PublishedLot(lotId, productId);
        lot.Status = ProductState.Draft;
        var uow = CreateUowForAddItem([lot], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddItemAsync(_userId, new AddCartItemRequestDto { LotId = lotId, Quantity = 1 }, CancellationToken.None));

        Assert.Contains("khả dụng", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID04_PublishedLotWithZeroWarehouseQuantity_ThrowsInvalidOperationException()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var lot = PublishedLot(lotId, productId, stockQty: 0);
        var uow = CreateUowForAddItem([lot], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddItemAsync(_userId, new AddCartItemRequestDto { LotId = lotId, Quantity = 1 }, CancellationToken.None));

        Assert.Contains("khả dụng", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID05_ExpiryNotAfterUtcNow_ThrowsInvalidOperationException()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var lot = PublishedLot(lotId, productId);
        lot.ExpiryDate = DateTime.UtcNow.AddMinutes(-30);
        var uow = CreateUowForAddItem([lot], []);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddItemAsync(_userId, new AddCartItemRequestDto { LotId = lotId, Quantity = 1 }, CancellationToken.None));

        Assert.Contains("khả dụng", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// UTCID06 policy predicate used by <see cref="CartService"/> (sheet § Automated notes option b — no injectable clock on service).
    /// </summary>
    [Fact]
    public void UTCID06_DailyExpiryOrderingPolicy_AfterCutoffWithExpiryEndingSameVietnamDay_ReturnsBlocked()
    {
        // 2026-05-14 14:35 UTC = 21:35 Vietnam (UTC+7) ⇒ past 21:00 cutoff
        var utcNow = new DateTime(2026, 5, 14, 14, 35, 0, DateTimeKind.Utc);
        // Same Vietnam calendar date (still 2026-05-14 locally), strictly after utcNow — was 17:00Z (= next VN date at midnight → policy false)
        var expiryUtc = new DateTime(2026, 5, 14, 15, 30, 0, DateTimeKind.Utc);

        Assert.True(DailyExpiryOrderingPolicy.IsLotBlockedForOrdering(expiryUtc, utcNow));
        Assert.False(DailyExpiryOrderingPolicy.IsLotBlockedForOrdering(expiryUtc, utcNow.AddHours(-1)));
    }

    [Fact]
    public async Task UTCID07_NewLineOnEmptyCart_PersistsAndMapsQuantity()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var lot = PublishedLot(lotId, productId);
        var uow = CreateUowForAddItem([lot], [new Product { ProductId = productId, Name = "P" }]);
        var sut = CreateSut(uow.Object);

        var result = await sut.AddItemAsync(_userId, new AddCartItemRequestDto { LotId = lotId, Quantity = 5 }, CancellationToken.None);

        var line = Assert.Single(result.Items);
        Assert.Equal(5, line.Quantity);
        Assert.Equal(lotId, line.LotId);
        Assert.Equal(50m, line.LineTotal);

        var reread = await sut.GetMyCartAsync(_userId, CancellationToken.None);
        Assert.Equal(5, Assert.Single(reread.Items).Quantity);
    }

    [Fact]
    public async Task UTCID08_ExistingLotId_MergesQuantity()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(DateTime.UtcNow.AddHours(-1), new CartLineSeed(cartItemId, lotId, 5)));

        var lot = PublishedLot(lotId, productId);
        var uow = CreateUowForAddItem([lot], [new Product { ProductId = productId, Name = "P" }]);
        var sut = CreateSut(uow.Object);

        var result = await sut.AddItemAsync(_userId, new AddCartItemRequestDto { LotId = lotId, Quantity = 3 }, CancellationToken.None);

        var line = Assert.Single(result.Items);
        Assert.Equal(8, line.Quantity);
        Assert.Equal(80m, line.LineTotal);
    }

    [Fact]
    public async Task UTCID09_PreExistingOtherLot_AddThirdLine_TotalItemsTwo()
    {
        var lotA = Guid.NewGuid();
        var lotB = Guid.NewGuid();
        var pidA = Guid.NewGuid();
        var pidB = Guid.NewGuid();
        SeedCartJson(_userId, SerializeRedisCartPayload(DateTime.UtcNow, new CartLineSeed(Guid.NewGuid(), lotA, 1)));

        var lotPublishedA = PublishedLot(lotA, pidA);
        var lotPublishedB = PublishedLot(lotB, pidB);
        var uow = CreateUowForAddItem([lotPublishedA, lotPublishedB], [
            new Product { ProductId = pidA, Name = "A" },
            new Product { ProductId = pidB, Name = "B" }
        ]);
        var sut = CreateSut(uow.Object);

        var result = await sut.AddItemAsync(_userId, new AddCartItemRequestDto { LotId = lotB, Quantity = 2 }, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(30m, result.TotalAmount);
    }

    [Fact]
    public async Task UTCID10_MinimumServiceQuantity_PointZeroZeroZeroOne()
    {
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var lot = PublishedLot(lotId, productId);
        lot.FinalUnitPrice = 100m;
        var uow = CreateUowForAddItem([lot], [new Product { ProductId = productId, Name = "Grain" }]);
        var sut = CreateSut(uow.Object);

        var result = await sut.AddItemAsync(_userId, new AddCartItemRequestDto { LotId = lotId, Quantity = 0.0001m }, CancellationToken.None);

        var line = Assert.Single(result.Items);
        Assert.Equal(0.0001m, line.Quantity);
        Assert.Equal(0.01m, line.LineTotal);
    }

    /// <summary>
    /// Supplemental UTCID06: full <see cref="CartService"/> path when walls-clock VN ≥ 21:00 and expiry is still strictly in the future UTC but same VN calendar day (skipped when near local-day boundary edge).
    /// </summary>
    [Fact]
    public async Task UTCID06_MessageWhenLotBlocked_EndToEnd_ThrowsWhenWallClockPassesPolicyGate()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var utcNowProbe = DailyExpiryOrderingPolicy.GetVietnamNow(DateTime.UtcNow);
        if (utcNowProbe.TimeOfDay < new TimeSpan(21, 0, 0))
            return;

        var (_, vnDayEndUtc) = DailyExpiryOrderingPolicy.GetVietnamDateRangeUtc(DateTime.UtcNow);
        var candidateExpiry = vnDayEndUtc.AddMinutes(-30);
        if (candidateExpiry <= DateTime.UtcNow)
            return;

        Assert.True(DailyExpiryOrderingPolicy.IsLotBlockedForOrdering(candidateExpiry, DateTime.UtcNow));

        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var lot = PublishedLot(lotId, productId);
        lot.ExpiryDate = candidateExpiry;

        var uow = CreateUowForAddItem([lot], [new Product { ProductId = productId, Name = "X" }]);
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddItemAsync(_userId, new AddCartItemRequestDto { LotId = lotId, Quantity = 1 }, CancellationToken.None));

        Assert.Contains("21:00", ex.Message);
    }
}
