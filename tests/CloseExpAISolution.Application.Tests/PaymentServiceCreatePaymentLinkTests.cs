using CloseExpAISolution.Application.Payment;
using CloseExpAISolution.Application.Policies;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PayOS.Models.V2.PaymentRequests;
using System.Globalization;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN029 — Columns 01–09 of <c>.github/instructions/create-payment-link-async-test-sheet.md</c>
/// (<see cref="PaymentService.CreatePaymentLinkAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~PaymentServiceCreatePaymentLinkTests"</c>
/// </summary>
public sealed class PaymentServiceCreatePaymentLinkTests : IDisposable
{
    private readonly SqliteConnection _conn;

    private static readonly Guid TimeSlotId = Guid.Parse("aaaaaaaa-7001-0001-0001-000000000001");
    private static readonly Guid CollectionId = Guid.Parse("bbbbbbbb-7001-0001-0001-000000000001");
    private static readonly Guid CustomerId = Guid.Parse("cccccccc-7001-0001-0001-000000000001");
    private static readonly Guid OtherUserId = Guid.Parse("dddddddd-7001-0001-0001-000000000001");
    private static readonly Guid UnitId = Guid.Parse("eeeeeeee-7001-0001-0001-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("ffffffff-7001-0001-0001-000000000001");
    private static readonly Guid SupermarketId = Guid.Parse("11111111-7001-1111-1111-111111111111");
    private static readonly Guid ProductId = Guid.Parse("22222222-7001-2222-2222-222222222222");
    private static readonly Guid LotId = Guid.Parse("33333333-7001-3333-3333-333333333333");
    private static readonly Guid OrderId = Guid.Parse("44444444-7001-4444-4444-444444444444");
    private static readonly Guid OrderItemId = Guid.Parse("55555555-7001-5555-5555-555555555555");

    private const string ValidReturn = "https://app.closeexp.test/return";
    private const string ValidCancel = "https://app.closeexp.test/cancel";

    public PaymentServiceCreatePaymentLinkTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
    }

    public void Dispose() => _conn.Dispose();

    private static PayOsSettings ValidPayOsSettings =>
        new()
        {
            ClientId = "test-client",
            ApiKey = "test-api",
            ChecksumKey = "test-checksum"
        };

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

    private static void SeedBaselineGraph(ApplicationDbContext ctx, DateTime lotExpiryUtc, decimal lotQuantity, string orderCode)
    {
        var now = DateTime.UtcNow;
        ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
        ctx.Users.AddRange(
            new User
            {
                UserId = CustomerId,
                FullName = "Buyer",
                Email = "buyer-pay.test@local",
                Phone = "1",
                PasswordHash = "x",
                RoleId = (int)RoleUser.Vendor,
                Status = UserState.Active,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                UserId = OtherUserId,
                FullName = "Other",
                Email = "other-pay.test@local",
                Phone = "2",
                PasswordHash = "x",
                RoleId = (int)RoleUser.Vendor,
                Status = UserState.Active,
                CreatedAt = now,
                UpdatedAt = now
            });
        ctx.DeliveryTimeSlots.Add(new DeliveryTimeSlot
        {
            DeliveryTimeSlotId = TimeSlotId,
            StartTime = TimeSpan.FromHours(19),
            EndTime = TimeSpan.FromHours(20.5)
        });
        ctx.CollectionPoints.Add(new CollectionPoint
        {
            CollectionId = CollectionId,
            Name = "CP",
            AddressLine = "A",
            Latitude = 10,
            Longitude = 106
        });
        ctx.UnitOfMeasures.Add(new UnitOfMeasure
        {
            UnitId = UnitId,
            Name = "Kg",
            Symbol = "kg",
            Type = "Weight",
            CreatedAt = now,
            UpdatedAt = now
        });
        ctx.Categories.Add(new Category
        {
            CategoryId = CategoryId,
            Name = "C",
            IsFreshFood = false,
            IsActive = true
        });
        ctx.Supermarkets.Add(new Supermarket
        {
            SupermarketId = SupermarketId,
            Name = "SM",
            Address = "A",
            Latitude = 10,
            Longitude = 106,
            ContactPhone = "0",
            Status = SupermarketState.Active,
            CreatedAt = now
        });
        ctx.Products.Add(new Product
        {
            ProductId = ProductId,
            CategoryId = CategoryId,
            SupermarketId = SupermarketId,
            UnitId = UnitId,
            Name = "P",
            Barcode = "b",
            Sku = "s",
            Status = ProductState.Verified,
            CreatedBy = "t",
            CreatedAt = now,
            UpdatedAt = now,
            IsFeatured = false
        });
        ctx.StockLots.Add(new StockLot
        {
            LotId = LotId,
            ProductId = ProductId,
            UnitId = UnitId,
            ExpiryDate = lotExpiryUtc,
            ManufactureDate = now.AddDays(-2),
            Quantity = lotQuantity,
            OriginalUnitPrice = 1,
            SuggestedUnitPrice = 1,
            Weight = 1,
            Status = ProductState.Published,
            CreatedAt = now,
            UpdatedAt = now
        });
        ctx.Orders.Add(new Order
        {
            OrderId = OrderId,
            OrderCode = orderCode,
            UserId = CustomerId,
            TimeSlotId = TimeSlotId,
            CollectionId = CollectionId,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = 100000,
            DiscountAmount = 0,
            FinalAmount = 100000,
            DeliveryFee = 0,
            SystemUsageFeeAmount = 0,
            Status = OrderState.Pending,
            OrderDate = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        ctx.OrderItems.Add(new OrderItem
        {
            OrderItemId = OrderItemId,
            OrderId = OrderId,
            LotId = LotId,
            Quantity = 1,
            UnitPrice = 100000,
            TotalPrice = 100000,
            PackagingStatus = PackagingState.Pending
        });
        ctx.SaveChanges();
    }

    private sealed class FixedUtcTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utc;
        public FixedUtcTimeProvider(DateTimeOffset utc) => _utc = utc;
        public override DateTimeOffset GetUtcNow() => _utc;
    }

    private static PaymentService CreateSut(
        IUnitOfWork uow,
        IPayOsPaymentLinkClient payLinks,
        TimeProvider? time = null,
        ILogger<PaymentService>? logger = null)
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        return new PaymentService(
            uow,
            Options.Create(ValidPayOsSettings),
            logger ?? Mock.Of<ILogger<PaymentService>>(),
            sp,
            time ?? TimeProvider.System,
            payLinks);
    }

    private static CreatePaymentLinkResponse OkLinkResponse() =>
        new()
        {
            PaymentLinkId = "pl_test_1",
            CheckoutUrl = "https://pay.dev.closeexp.test/checkout/1"
        };

    [Fact]
    public async Task Column01_BaselineSuccess_CheckoutUrl_AndPendingTransactionUpdated()
    {
        using var ctx = CreateContext();
        var lotExpiry = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(3), DateTimeKind.Utc);
        SeedBaselineGraph(ctx, lotExpiry, 50, "ORD-001");
        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.CreateAsync(It.IsAny<CreatePaymentLinkRequest>()))
            .ReturnsAsync(OkLinkResponse());
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object);
        var result = await sut.CreatePaymentLinkAsync(CustomerId, OrderId, ValidReturn, ValidCancel);
        Assert.False(string.IsNullOrWhiteSpace(result.CheckoutUrl));
        Assert.Equal(OrderId, result.OrderId);
        var tx = await ctx.Transactions.SingleAsync();
        Assert.Equal(PaymentState.Pending, tx.PaymentStatus);
        Assert.Equal("pl_test_1", tx.PayOSPaymentLinkId);
        Assert.Contains("pay.dev", tx.CheckoutUrl ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Column02_LongOrderCode_DescriptionTrimmedTo25Chars()
    {
        using var ctx = CreateContext();
        var longCode = new string('X', 28) + "-SUF";
        SeedBaselineGraph(ctx, DateTime.UtcNow.AddDays(2), 40, longCode);
        string? capturedDesc = null;
        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.CreateAsync(It.IsAny<CreatePaymentLinkRequest>()))
            .Callback<CreatePaymentLinkRequest>(r => capturedDesc = r.Description)
            .ReturnsAsync(OkLinkResponse());
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object);
        await sut.CreatePaymentLinkAsync(CustomerId, OrderId, ValidReturn, ValidCancel);
        Assert.NotNull(capturedDesc);
        Assert.Equal(25, capturedDesc!.Length);
        Assert.Equal(longCode.Trim()[..25], capturedDesc);
    }

    [Fact]
    public async Task Column03_AfterCutoff_SameDayExpiryLot_ThrowsInvalidOperation()
    {
        var utcNow = DateTimeOffset.Parse("2026-05-14T14:35:00Z");
        var lotExpiry = DateTime.SpecifyKind(DateTime.Parse("2026-05-14T16:00:00", CultureInfo.InvariantCulture), DateTimeKind.Utc);
        using var ctx = CreateContext();
        SeedBaselineGraph(ctx, lotExpiry, 50, "CUT-01");

        var lotFromDb = await ctx.StockLots.AsNoTracking().SingleAsync();
        var expiryForPolicy = lotFromDb.ExpiryDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(lotFromDb.ExpiryDate, DateTimeKind.Utc)
            : lotFromDb.ExpiryDate.ToUniversalTime();
        Assert.True(DailyExpiryOrderingPolicy.IsOrderCutoffReached(utcNow.UtcDateTime));
        Assert.True(DailyExpiryOrderingPolicy.IsExpiringInVietnamToday(expiryForPolicy, utcNow.UtcDateTime));

        var pay = new Mock<IPayOsPaymentLinkClient>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object, new FixedUtcTimeProvider(utcNow));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreatePaymentLinkAsync(CustomerId, OrderId, ValidReturn, ValidCancel));
        Assert.Contains("21:00", ex.Message, StringComparison.Ordinal);
        pay.Verify(p => p.CreateAsync(It.IsAny<CreatePaymentLinkRequest>()), Times.Never());
    }

    [Fact]
    public async Task Column04_OrderNotFound_ThrowsKeyNotFound()
    {
        using var ctx = CreateContext();
        var pay = new Mock<IPayOsPaymentLinkClient>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object);
        var missing = Guid.Parse("99999999-9999-9999-9999-999999999999");
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.CreatePaymentLinkAsync(CustomerId, missing, ValidReturn, ValidCancel));
        pay.Verify(p => p.CreateAsync(It.IsAny<CreatePaymentLinkRequest>()), Times.Never());
    }

    [Fact]
    public async Task Column05_WrongUser_ThrowsUnauthorizedAccess()
    {
        using var ctx = CreateContext();
        SeedBaselineGraph(ctx, DateTime.UtcNow.AddDays(2), 50, "U-1");
        var pay = new Mock<IPayOsPaymentLinkClient>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.CreatePaymentLinkAsync(OtherUserId, OrderId, ValidReturn, ValidCancel));
        pay.Verify(p => p.CreateAsync(It.IsAny<CreatePaymentLinkRequest>()), Times.Never());
    }

    public enum InvalidOrderKind { NotPending, NoItems, EnsureQty }

    [Theory]
    [InlineData(InvalidOrderKind.NotPending)]
    [InlineData(InvalidOrderKind.NoItems)]
    [InlineData(InvalidOrderKind.EnsureQty)]
    public async Task Column06_InvalidOrderStateOrInventory_ThrowsInvalidOperation(InvalidOrderKind kind)
    {
        using var ctx = CreateContext();
        var lotExpiry = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(4), DateTimeKind.Utc);
        SeedBaselineGraph(ctx, lotExpiry, kind == InvalidOrderKind.EnsureQty ? 0m : 50m, "INV-1");
        var order = await ctx.Orders.FirstAsync(o => o.OrderId == OrderId);
        if (kind == InvalidOrderKind.NotPending)
            order.Status = OrderState.Paid;
        if (kind == InvalidOrderKind.NoItems)
        {
            var items = await ctx.OrderItems.Where(i => i.OrderId == OrderId).ToListAsync();
            ctx.OrderItems.RemoveRange(items);
        }
        await ctx.SaveChangesAsync();
        var pay = new Mock<IPayOsPaymentLinkClient>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreatePaymentLinkAsync(CustomerId, OrderId, ValidReturn, ValidCancel));
        Assert.NotEmpty(ex.Message);
        pay.Verify(p => p.CreateAsync(It.IsAny<CreatePaymentLinkRequest>()), Times.Never());
    }

    [Fact]
    public async Task Column07_FinalAmountRoundsBelowMinimum_ThrowsInvalidOperation()
    {
        using var ctx = CreateContext();
        SeedBaselineGraph(ctx, DateTime.UtcNow.AddDays(2), 50, "LOW-1");
        var order = await ctx.Orders.FirstAsync(o => o.OrderId == OrderId);
        order.FinalAmount = 0.4m;
        order.TotalAmount = 0.4m;
        await ctx.SaveChangesAsync();
        var pay = new Mock<IPayOsPaymentLinkClient>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreatePaymentLinkAsync(CustomerId, OrderId, ValidReturn, ValidCancel));
        Assert.Contains("at least", ex.Message, StringComparison.OrdinalIgnoreCase);
        pay.Verify(p => p.CreateAsync(It.IsAny<CreatePaymentLinkRequest>()), Times.Never());
    }

    [Theory]
    [InlineData(null, "https://x.test/c")]
    [InlineData("https://x.test/r", null)]
    [InlineData(" ", "https://x.test/c")]
    public async Task Column08_MissingUrls_ThrowsInvalidOperation(string? ret, string? can)
    {
        using var ctx = CreateContext();
        SeedBaselineGraph(ctx, DateTime.UtcNow.AddDays(2), 50, "URL-1");
        var pay = new Mock<IPayOsPaymentLinkClient>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreatePaymentLinkAsync(CustomerId, OrderId, ret, can));
        pay.Verify(p => p.CreateAsync(It.IsAny<CreatePaymentLinkRequest>()), Times.Never());
    }

    [Fact]
    public async Task Column09_PayOsThrows_MarksTransactionFailed_LogsError_Rethrows()
    {
        using var ctx = CreateContext();
        SeedBaselineGraph(ctx, DateTime.UtcNow.AddDays(2), 50, "FAIL-1");
        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.CreateAsync(It.IsAny<CreatePaymentLinkRequest>()))
            .ThrowsAsync(new InvalidOperationException("simulated PayOS outage"));
        var log = new Mock<ILogger<PaymentService>>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object, logger: log.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreatePaymentLinkAsync(CustomerId, OrderId, ValidReturn, ValidCancel));
        var tx = await ctx.Transactions.SingleAsync();
        Assert.Equal(PaymentState.Failed, tx.PaymentStatus);
        Assert.NotNull(tx.UpdatedAt);
        log.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
