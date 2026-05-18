using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Payment;
using CloseExpAISolution.Application.Services;
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
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN030 — Columns 01–10 of <c>.github/instructions/confirm-payment-async-test-sheet.md</c>
/// (<see cref="PaymentService.ConfirmPaymentAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~PaymentServiceConfirmPaymentTests"</c>
/// </summary>
public sealed class PaymentServiceConfirmPaymentTests : IDisposable
{
    private readonly SqliteConnection _conn;

    private static readonly Guid TimeSlotId = Guid.Parse("aaaaaaaa-8001-0001-0001-000000000001");
    private static readonly Guid CollectionId = Guid.Parse("bbbbbbbb-8001-0001-0001-000000000001");
    private static readonly Guid CustomerId = Guid.Parse("cccccccc-8001-0001-0001-000000000001");
    private static readonly Guid UnitId = Guid.Parse("eeeeeeee-8001-0001-0001-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("ffffffff-8001-0001-0001-000000000001");
    private static readonly Guid SupermarketId = Guid.Parse("11111111-8001-1111-1111-111111111111");
    private static readonly Guid ProductId = Guid.Parse("22222222-8001-2222-2222-222222222222");
    private static readonly Guid LotId = Guid.Parse("33333333-8001-3333-3333-333333333333");
    private static readonly Guid OrderId = Guid.Parse("44444444-8001-4444-4444-444444444444");
    private static readonly Guid OrderItemId = Guid.Parse("55555555-8001-5555-5555-555555555555");
    private static readonly Guid TransactionId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    /// <summary>Matches seeded <see cref="Transaction.PayOSOrderCode"/>.</summary>
    private const long PayOsOrderCode = 880_011_002_993L;

    public PaymentServiceConfirmPaymentTests()
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

    /// <summary>Graph + cancel window config + pending transaction referencing <paramref name="payOsCode"/>.</summary>
    private static void SeedForConfirm(ApplicationDbContext ctx, decimal stockQuantity, PaymentState txState, long payOsCode)
    {
        var now = DateTime.UtcNow;
        var lotExpiry = DateTime.SpecifyKind(now.AddDays(5), DateTimeKind.Utc);

        ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
        ctx.Users.Add(new User
        {
            UserId = CustomerId,
            FullName = "Buyer",
            Email = "buyer-confirmpay.test@local",
            Phone = "1",
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
            ExpiryDate = lotExpiry,
            ManufactureDate = now.AddDays(-2),
            Quantity = stockQuantity,
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
            OrderCode = "CONFIRM-TC",
            UserId = CustomerId,
            TimeSlotId = TimeSlotId,
            CollectionId = CollectionId,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = 100_000,
            DiscountAmount = 0,
            FinalAmount = 100_000,
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
            UnitPrice = 100_000,
            TotalPrice = 100_000,
            PackagingStatus = PackagingState.Pending
        });

        ctx.SystemConfigs.Add(new SystemConfig
        {
            ConfigKey = SystemConfigKeys.OrderCancelWindowMinutesAfterPaid,
            ConfigValue = "60",
            UpdatedAt = now
        });

        ctx.Transactions.Add(new Transaction
        {
            TransactionId = TransactionId,
            OrderId = OrderId,
            Amount = 100_000,
            PaymentMethod = "PayOS",
            PaymentStatus = txState,
            CreatedAt = now,
            UpdatedAt = txState == PaymentState.Paid ? now : null,
            PayOSOrderCode = payOsCode
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
        ILogger<PaymentService>? logger = null,
        StackExchange.Redis.IConnectionMultiplexer? redis = null)
    {
        var services = new ServiceCollection();
        if (redis != null)
            services.AddSingleton(redis);
        var unitConversion = new UnitConversionRateService(uow);
        services.AddSingleton(new OrderStockQuantityHelper(uow, unitConversion));
        var sp = services.BuildServiceProvider();
        return new PaymentService(
            uow,
            Options.Create(ValidPayOsSettings),
            logger ?? Mock.Of<ILogger<PaymentService>>(),
            sp,
            time ?? TimeProvider.System,
            payLinks);
    }

    private static PaymentLink PaidLinkStub(long amount, long paid) =>
        new()
        {
            Status = PaymentLinkStatus.Paid,
            Amount = amount,
            AmountPaid = paid
        };

    private static PaymentLink PendingFullAmountStub(long amount) =>
        new()
        {
            Status = PaymentLinkStatus.Pending,
            Amount = amount,
            AmountPaid = amount
        };

    [Fact]
    public async Task Column01_Baseline_PaidPayOS_PendingTxn_OrderPaid_TransactionPaid_ReducesStock_RedirectClearCartRedis()
    {
        using var ctx = CreateContext();
        SeedForConfirm(ctx, stockQuantity: 50, PaymentState.Pending, PayOsOrderCode);
        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.GetAsync(PayOsOrderCode))
            .ReturnsAsync(PaidLinkStub(100_000, 100_000));

        var cartKey = (StackExchange.Redis.RedisKey)$"cart:{CustomerId:D}";
        var dbRedis = new Mock<StackExchange.Redis.IDatabase>();
        dbRedis.Setup(d => d.KeyDeleteAsync(cartKey, It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync(true);

        var muxRedis = new Mock<StackExchange.Redis.IConnectionMultiplexer>();
        muxRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(dbRedis.Object);

        var sut = CreateSut(new UnitOfWork(ctx), pay.Object, redis: muxRedis.Object);
        var result = await sut.ConfirmPaymentAsync(PayOsOrderCode);

        Assert.True(result.Success);
        Assert.Equal(PaymentConfirmErrorCode.None, result.ErrorCode);

        var lot = await ctx.StockLots.AsNoTracking().SingleAsync(l => l.LotId == LotId);
        Assert.Equal(49m, lot.Quantity);

        var tx = await ctx.Transactions.AsNoTracking().SingleAsync(t => t.TransactionId == TransactionId);
        Assert.Equal(PaymentState.Paid, tx.PaymentStatus);

        var order = await ctx.Orders.AsNoTracking().SingleAsync(o => o.OrderId == OrderId);
        Assert.Equal(OrderState.Paid, order.Status);
        Assert.NotNull(order.CancelDeadline);

        muxRedis.Verify(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()), Times.AtLeastOnce());
        dbRedis.Verify(d => d.KeyDeleteAsync(cartKey, It.IsAny<StackExchange.Redis.CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task Column02_Idempotent_LocalTransactionAlreadyPaid_Ok_NoStockMovement()
    {
        using var ctx = CreateContext();
        SeedForConfirm(ctx, stockQuantity: 50, PaymentState.Paid, PayOsOrderCode);
        var qtyBefore = (await ctx.StockLots.AsNoTracking().SingleAsync(l => l.LotId == LotId)).Quantity;

        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.GetAsync(PayOsOrderCode))
            .ReturnsAsync(PaidLinkStub(100_000, 100_000));
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object);

        var result = await sut.ConfirmPaymentAsync(PayOsOrderCode);

        Assert.True(result.Success);
        var qtyAfter = (await ctx.StockLots.AsNoTracking().SingleAsync(l => l.LotId == LotId)).Quantity;
        Assert.Equal(qtyBefore, qtyAfter);

        pay.Verify(p => p.GetAsync(PayOsOrderCode), Times.Once);
    }

    [Fact]
    public async Task Column03_Settled_NonPaidStatus_WithFullAmount_FollowsPaidPathLikeColumn01()
    {
        using var ctx = CreateContext();
        SeedForConfirm(ctx, stockQuantity: 40, PaymentState.Pending, PayOsOrderCode);
        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.GetAsync(PayOsOrderCode))
            .ReturnsAsync(PendingFullAmountStub(100_000));

        var sut = CreateSut(new UnitOfWork(ctx), pay.Object);
        var result = await sut.ConfirmPaymentAsync(PayOsOrderCode);

        Assert.True(result.Success);
        var tx = await ctx.Transactions.SingleAsync(t => t.TransactionId == TransactionId);
        Assert.Equal(PaymentState.Paid, tx.PaymentStatus);
        Assert.Equal(OrderState.Paid, (await ctx.Orders.SingleAsync(o => o.OrderId == OrderId)).Status);
    }

    [Fact]
    public async Task Column04_AmountZero_NotSettled_NotPaidYet_ErrCodePaymentNotComplete()
    {
        using var ctx = CreateContext();
        SeedForConfirm(ctx, stockQuantity: 50, PaymentState.Pending, PayOsOrderCode);
        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.GetAsync(PayOsOrderCode))
            .ReturnsAsync(new PaymentLink { Status = PaymentLinkStatus.Pending, Amount = 0, AmountPaid = 0 });

        var log = new Mock<ILogger<PaymentService>>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object, logger: log.Object);
        var result = await sut.ConfirmPaymentAsync(PayOsOrderCode);

        Assert.False(result.Success);
        Assert.Equal(PaymentConfirmErrorCode.PaymentNotComplete, result.ErrorCode);
        Assert.False(string.IsNullOrEmpty(result.PayOsStatus));

        VerifyInformationLoggedAtLeastOnce(log);
    }

    [Fact]
    public async Task Column05_PartialAmount_NotPaidYet()
    {
        using var ctx = CreateContext();
        SeedForConfirm(ctx, stockQuantity: 50, PaymentState.Pending, PayOsOrderCode);
        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.GetAsync(PayOsOrderCode))
            .ReturnsAsync(new PaymentLink
            {
                Status = PaymentLinkStatus.Pending,
                Amount = 100_000,
                AmountPaid = 50_000
            });

        var log = new Mock<ILogger<PaymentService>>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object, logger: log.Object);
        var result = await sut.ConfirmPaymentAsync(PayOsOrderCode);

        Assert.False(result.Success);
        Assert.Equal(PaymentConfirmErrorCode.PaymentNotComplete, result.ErrorCode);
        Assert.NotNull(result.AmountPaid);
        Assert.True(result.Message.Contains("AmountPaid=", StringComparison.Ordinal));
        VerifyInformationLoggedAtLeastOnce(log);
    }

    [Theory]
    [InlineData(PaymentLinkStatus.Cancelled)]
    [InlineData(PaymentLinkStatus.Failed)]
    [InlineData(PaymentLinkStatus.Expired)]
    public async Task Column06_TerminalPayOS_LocalPending_AppliesFailedAndCanceled_Order(PaymentLinkStatus terminal)
    {
        using var ctx = CreateContext();
        SeedForConfirm(ctx, stockQuantity: 50, PaymentState.Pending, PayOsOrderCode);
        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.GetAsync(PayOsOrderCode))
            .ReturnsAsync(new PaymentLink
            {
                Status = terminal,
                Amount = 100_000,
                AmountPaid = 0
            });

        var log = new Mock<ILogger<PaymentService>>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object, logger: log.Object);
        var result = await sut.ConfirmPaymentAsync(PayOsOrderCode);

        Assert.False(result.Success);
        Assert.Equal(PaymentConfirmErrorCode.PaymentNotComplete, result.ErrorCode);

        var tx = await ctx.Transactions.SingleAsync(t => t.TransactionId == TransactionId);
        Assert.Equal(PaymentState.Failed, tx.PaymentStatus);
        Assert.Equal(OrderState.Canceled, (await ctx.Orders.SingleAsync(o => o.OrderId == OrderId)).Status);

        VerifyInformationLoggedAtLeastOnce(log);
    }

    [Fact]
    public async Task Column07_NotSettled_NoLocalTransaction_InfoLog_NoSideEffect()
    {
        const long orphanedCode = 990_077_088_099L;

        using var ctx = CreateContext();
        SeedForConfirm(ctx, stockQuantity: 50, PaymentState.Pending, PayOsOrderCode);
        ctx.Transactions.RemoveRange(ctx.Transactions);
        ctx.SaveChanges();

        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.GetAsync(orphanedCode))
            .ReturnsAsync(new PaymentLink { Status = PaymentLinkStatus.Pending, Amount = 100_000, AmountPaid = 10_000 });

        var log = new Mock<ILogger<PaymentService>>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object, logger: log.Object);

        var result = await sut.ConfirmPaymentAsync(orphanedCode);
        Assert.False(result.Success);
        Assert.Equal(PaymentConfirmErrorCode.PaymentNotComplete, result.ErrorCode);
        VerifyInformationLoggedAtLeastOnce(log);
        Assert.Empty(await ctx.Transactions.ToListAsync());
    }

    [Fact]
    public async Task Column08_GetThrows_PayOsFailure_LogWarning_WithException()
    {
        using var ctx = CreateContext();
        SeedForConfirm(ctx, stockQuantity: 50, PaymentState.Pending, PayOsOrderCode);
        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.GetAsync(PayOsOrderCode))
            .ThrowsAsync(new HttpRequestException("simulated outage"));

        var log = new Mock<ILogger<PaymentService>>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object, logger: log.Object);
        var result = await sut.ConfirmPaymentAsync(PayOsOrderCode);

        Assert.False(result.Success);
        Assert.Equal(PaymentConfirmErrorCode.PayOsUnavailable, result.ErrorCode);
        Assert.False(string.IsNullOrEmpty(result.Message));

        log.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Column09_Settled_PaidButNoTxnRow_MissingTransaction_WarningLogged()
    {
        const long missingRowCode = 770_088_099_011L;

        using var ctx = CreateContext();
        SeedForConfirm(ctx, stockQuantity: 50, PaymentState.Pending, PayOsOrderCode);
        ctx.Transactions.RemoveRange(ctx.Transactions);
        ctx.SaveChanges();

        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.GetAsync(missingRowCode)).ReturnsAsync(PaidLinkStub(100_000, 100_000));

        var log = new Mock<ILogger<PaymentService>>();
        var sut = CreateSut(new UnitOfWork(ctx), pay.Object, logger: log.Object);
        var result = await sut.ConfirmPaymentAsync(missingRowCode);

        Assert.False(result.Success);
        Assert.Equal(PaymentConfirmErrorCode.TransactionMissing, result.ErrorCode);
        VerifyWarningLoggedAtLeastOnce(log);
    }

    [Fact]
    public async Task Column10_StockInsufficientAfterPayment_OrderFailed_But_ResultOk_TransactionMarkedPaid()
    {
        using var ctx = CreateContext();
        SeedForConfirm(ctx, stockQuantity: 0m, PaymentState.Pending, PayOsOrderCode);
        var pay = new Mock<IPayOsPaymentLinkClient>();
        pay.Setup(p => p.GetAsync(PayOsOrderCode)).ReturnsAsync(PaidLinkStub(100_000, 100_000));

        var sut = CreateSut(new UnitOfWork(ctx), pay.Object);
        var result = await sut.ConfirmPaymentAsync(PayOsOrderCode);

        Assert.True(result.Success);
        Assert.Equal(OrderState.Failed, (await ctx.Orders.SingleAsync(o => o.OrderId == OrderId)).Status);
        Assert.Equal(PaymentState.Paid, (await ctx.Transactions.SingleAsync(t => t.TransactionId == TransactionId)).PaymentStatus);
    }

    private static void VerifyInformationLoggedAtLeastOnce(Mock<ILogger<PaymentService>> log) =>
        log.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

    private static void VerifyWarningLoggedAtLeastOnce(Mock<ILogger<PaymentService>> log) =>
        log.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
}
