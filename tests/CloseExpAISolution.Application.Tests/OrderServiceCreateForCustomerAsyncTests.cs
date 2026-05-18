using System.Linq.Expressions;
using AutoMapper;
using CloseExpAISolution.Application.Configuration;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Policies;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Repositories.Interface;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN020 — UTCID01–UTCID10 per <c>.github/instructions/create-for-customer-async-test-sheet.md</c>
/// (<see cref="OrderService.CreateForCustomerAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~OrderServiceCreateForCustomerAsyncTests"</c>
/// </summary>
public sealed class OrderServiceCreateForCustomerAsyncTests
{
    private static OrderService CreateSut(
        IUnitOfWork uow,
        IMapper mapper,
        IPromotionService promotionService,
        IPromotionUsageService promotionUsageService,
        IOrderNotificationPublisher notifier)
    {
        var unitConv = UnitConversionTestDoubles.PassiveIdentity();
        return new OrderService(
            uow,
            mapper,
            promotionService,
            promotionUsageService,
            Options.Create(new PickupSearchOptions()),
            notifier,
            new OrderItemUnitConverter(uow, unitConv),
            new OrderStockQuantityHelper(uow, unitConv));
    }

    private static User ActiveUser(Guid userId) => new()
    {
        UserId = userId,
        Status = UserState.Active,
        FullName = "A",
        Email = "a@test.local",
        Phone = "0",
        PasswordHash = "x",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static StockLot BuildStockLot(
        Guid lotId,
        ProductState status,
        decimal warehouseQty,
        DateTime expiryUtc)
    {
        var now = DateTime.UtcNow;
        return new StockLot
        {
            LotId = lotId,
            ProductId = Guid.NewGuid(),
            UnitId = Guid.NewGuid(),
            ExpiryDate = expiryUtc,
            ManufactureDate = now.AddDays(-2),
            Quantity = warehouseQty,
            OriginalUnitPrice = 10m,
            SuggestedUnitPrice = 10m,
            FinalUnitPrice = 10m,
            Weight = 1m,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static CreateOwnOrderRequestDto PickupOrderRequest(
        Guid collectionId,
        Guid lotId,
        int quantity,
        decimal unitPrice,
        Guid timeSlotId)
    {
        return new CreateOwnOrderRequestDto
        {
            TimeSlotId = timeSlotId,
            CollectionId = collectionId,
            DeliveryType = DeliveryMethod.Pickup,
            AddressId = null,
            DeliveryFee = 0m,
            OrderItems =
            [
                new CreateOrderItemDto { LotId = lotId, Quantity = quantity, UnitPrice = unitPrice }
            ]
        };
    }

    private static CreateOwnOrderRequestDto DeliveryOrderRequest(
        Guid addressId,
        Guid lotId,
        int quantity,
        decimal unitPrice,
        Guid timeSlotId)
    {
        return new CreateOwnOrderRequestDto
        {
            TimeSlotId = timeSlotId,
            CollectionId = null,
            DeliveryType = DeliveryMethod.Delivery,
            AddressId = addressId,
            DeliveryFee = 15000m,
            OrderItems =
            [
                new CreateOrderItemDto { LotId = lotId, Quantity = quantity, UnitPrice = unitPrice }
            ]
        };
    }

    private static Mock<IUnitOfWork> CreateUowWithUser(User? user)
    {
        var users = user != null ? new List<User> { user } : new List<User>();
        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((Expression<Func<User, bool>> pred) => users.FirstOrDefault(pred.Compile()));

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);
        return uow;
    }

    private static void SetupSystemUsageFee(Mock<IUnitOfWork> uow, decimal feeVnd = 0m)
    {
        var cfg = new SystemConfig
        {
            ConfigKey = SystemConfigKeys.OrderSystemUsageFeeVnd,
            ConfigValue = feeVnd.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        var configRepo = new Mock<IGenericRepository<SystemConfig>>();
        configRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SystemConfig, bool>>>()))
            .ReturnsAsync((Expression<Func<SystemConfig, bool>> pred) =>
                pred.Compile()(cfg) ? cfg : null);
        uow.Setup(x => x.Repository<SystemConfig>()).Returns(configRepo.Object);
    }

    private static void SetupStockLots(Mock<IUnitOfWork> uow, IReadOnlyList<StockLot> lots)
    {
        var list = lots.ToList();
        var lotRepo = new Mock<IGenericRepository<StockLot>>();
        lotRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .ReturnsAsync((Expression<Func<StockLot, bool>> pred) => list.Where(pred.Compile()).ToList());
        uow.Setup(x => x.Repository<StockLot>()).Returns(lotRepo.Object);
    }

    private static void SetupProducts(Mock<IUnitOfWork> uow, IReadOnlyList<Product> products)
    {
        var list = products.ToList();
        var productRepo = new Mock<IGenericRepository<Product>>();
        productRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync((Expression<Func<Product, bool>> pred) => list.Where(pred.Compile()).ToList());
        uow.Setup(x => x.Repository<Product>()).Returns(productRepo.Object);
    }

    private static void SetupOrderPersistence(Mock<IUnitOfWork> uow, Mock<IOrderRepository> orderRepo, Order?[] capture)
    {
        orderRepo
            .Setup(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns<Order, CancellationToken>((ord, _) =>
            {
                capture[0] = ord;
                return Task.FromResult(ord);
            });

        orderRepo
            .Setup(o => o.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capture[0]);

        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);

        var logRepo = new Mock<IGenericRepository<OrderStatusLog>>();
        logRepo
            .Setup(r => r.AddAsync(It.IsAny<OrderStatusLog>()))
            .ReturnsAsync((OrderStatusLog l) => l);
        uow.Setup(x => x.Repository<OrderStatusLog>()).Returns(logRepo.Object);

        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static IMapper CreatePassThroughMapper()
    {
        var mock = new Mock<IMapper>();
        mock.Setup(m => m.Map<OrderResponseDto>(It.IsAny<Order>()))
            .Returns((Order o) => new OrderResponseDto
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                UserId = o.UserId,
                TimeSlotId = o.TimeSlotId,
                CollectionId = o.CollectionId,
                DeliveryType = o.DeliveryType,
                TotalAmount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                FinalAmount = o.FinalAmount,
                DeliveryFee = o.DeliveryFee,
                SystemUsageFeeAmount = o.SystemUsageFeeAmount,
                Status = o.Status.ToString(),
                OrderDate = o.OrderDate,
                AddressId = o.AddressId,
                PromotionId = o.PromotionId,
                DeliveryGroupId = o.DeliveryGroupId,
                DeliveryNote = o.DeliveryNote,
                CancelDeadline = o.CancelDeadline,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            });
        return mock.Object;
    }

    [Fact]
    public async Task UTCID01_UserNotFound_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var uow = CreateUowWithUser(null);
        SetupSystemUsageFee(uow);
        var mapper = CreatePassThroughMapper();
        var promo = new Mock<IPromotionService>().Object;
        var promoUsage = new Mock<IPromotionUsageService>().Object;
        var notifier = new Mock<IOrderNotificationPublisher>().Object;
        var sut = CreateSut(uow.Object, mapper, promo, promoUsage, notifier);

        var req = PickupOrderRequest(Guid.NewGuid(), Guid.NewGuid(), 1, 10m, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateForCustomerAsync(userId, req, CancellationToken.None));

        Assert.Contains("Không tìm thấy tài khoản", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID02_UserInactive_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var inactive = ActiveUser(userId);
        inactive.Status = UserState.Locked;
        var uow = CreateUowWithUser(inactive);
        SetupSystemUsageFee(uow);
        var mapper = CreatePassThroughMapper();
        var promo = new Mock<IPromotionService>().Object;
        var promoUsage = new Mock<IPromotionUsageService>().Object;
        var notifier = new Mock<IOrderNotificationPublisher>().Object;
        var sut = CreateSut(uow.Object, mapper, promo, promoUsage, notifier);

        var req = PickupOrderRequest(Guid.NewGuid(), Guid.NewGuid(), 1, 10m, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateForCustomerAsync(userId, req, CancellationToken.None));

        Assert.Contains("hoạt động", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID03_InvalidDeliveryType_ThrowsArgumentException()
    {
        var userId = Guid.NewGuid();
        var uow = CreateUowWithUser(ActiveUser(userId));
        SetupSystemUsageFee(uow);
        var mapper = CreatePassThroughMapper();
        var promo = new Mock<IPromotionService>().Object;
        var promoUsage = new Mock<IPromotionUsageService>().Object;
        var notifier = new Mock<IOrderNotificationPublisher>().Object;
        var sut = CreateSut(uow.Object, mapper, promo, promoUsage, notifier);

        var lotId = Guid.NewGuid();
        SetupStockLots(uow, [BuildStockLot(lotId, ProductState.Published, 100m, DateTime.UtcNow.AddDays(1))]);

        var req = PickupOrderRequest(Guid.NewGuid(), lotId, 1, 10m, Guid.NewGuid());
        req.DeliveryType = "INVALID_MODE_XYZ";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateForCustomerAsync(userId, req, CancellationToken.None));

        Assert.Equal("raw", ex.ParamName);
    }

    [Fact]
    public async Task UTCID04_PickupMissingCollection_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var uow = CreateUowWithUser(ActiveUser(userId));
        SetupSystemUsageFee(uow);
        var mapper = CreatePassThroughMapper();
        var promo = new Mock<IPromotionService>().Object;
        var promoUsage = new Mock<IPromotionUsageService>().Object;
        var notifier = new Mock<IOrderNotificationPublisher>().Object;
        var sut = CreateSut(uow.Object, mapper, promo, promoUsage, notifier);

        var lotId = Guid.NewGuid();
        SetupStockLots(uow, [BuildStockLot(lotId, ProductState.Published, 100m, DateTime.UtcNow.AddDays(1))]);

        var req = PickupOrderRequest(Guid.NewGuid(), lotId, 1, 10m, Guid.NewGuid());
        req.CollectionId = null;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateForCustomerAsync(userId, req, CancellationToken.None));

        Assert.Contains("collectionId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID05_DeliveryMissingAddress_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var uow = CreateUowWithUser(ActiveUser(userId));
        SetupSystemUsageFee(uow);
        var mapper = CreatePassThroughMapper();
        var promo = new Mock<IPromotionService>().Object;
        var promoUsage = new Mock<IPromotionUsageService>().Object;
        var notifier = new Mock<IOrderNotificationPublisher>().Object;
        var sut = CreateSut(uow.Object, mapper, promo, promoUsage, notifier);

        var lotId = Guid.NewGuid();
        SetupStockLots(uow, [BuildStockLot(lotId, ProductState.Published, 100m, DateTime.UtcNow.AddDays(1))]);

        var req = DeliveryOrderRequest(Guid.NewGuid(), lotId, 1, 10m, Guid.NewGuid());
        req.AddressId = null;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateForCustomerAsync(userId, req, CancellationToken.None));

        Assert.Contains("addressId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID06_EmptyOrderItems_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var uow = CreateUowWithUser(ActiveUser(userId));
        SetupSystemUsageFee(uow);
        var mapper = CreatePassThroughMapper();
        var promo = new Mock<IPromotionService>().Object;
        var promoUsage = new Mock<IPromotionUsageService>().Object;
        var notifier = new Mock<IOrderNotificationPublisher>().Object;
        var sut = CreateSut(uow.Object, mapper, promo, promoUsage, notifier);

        SetupStockLots(uow, []);

        var req = new CreateOwnOrderRequestDto
        {
            TimeSlotId = Guid.NewGuid(),
            CollectionId = Guid.NewGuid(),
            DeliveryType = DeliveryMethod.Pickup,
            DeliveryFee = 0m,
            OrderItems = []
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateForCustomerAsync(userId, req, CancellationToken.None));

        Assert.Contains("ít nhất một", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    public enum LotGuardCase
    {
        MissingLot,
        NotPublished
    }

    [Theory]
    [InlineData(LotGuardCase.MissingLot)]
    [InlineData(LotGuardCase.NotPublished)]
    public async Task UTCID07_StockLotGuardrails_ThrowsInvalidOperationException(LotGuardCase c)
    {
        var userId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        var uow = CreateUowWithUser(ActiveUser(userId));
        SetupSystemUsageFee(uow);
        var mapper = CreatePassThroughMapper();
        var promo = new Mock<IPromotionService>().Object;
        var promoUsage = new Mock<IPromotionUsageService>().Object;
        var notifier = new Mock<IOrderNotificationPublisher>().Object;
        var sut = CreateSut(uow.Object, mapper, promo, promoUsage, notifier);

        if (c == LotGuardCase.MissingLot)
            SetupStockLots(uow, []);
        else
            SetupStockLots(uow, [BuildStockLot(lotId, ProductState.Draft, 100m, DateTime.UtcNow.AddDays(1))]);

        var req = PickupOrderRequest(Guid.NewGuid(), lotId, 1, 10m, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateForCustomerAsync(userId, req, CancellationToken.None));

        if (c == LotGuardCase.MissingLot)
            Assert.Contains("Không tìm thấy StockLot", ex.Message, StringComparison.Ordinal);
        else
            Assert.Contains("Published", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    public enum StockQtyCase
    {
        Insufficient,
        Expired
    }

    [Theory]
    [InlineData(StockQtyCase.Insufficient)]
    [InlineData(StockQtyCase.Expired)]
    public async Task UTCID08_QuantityOrExpiryGuard_ThrowsInvalidOperationException(StockQtyCase c)
    {
        var userId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        var uow = CreateUowWithUser(ActiveUser(userId));
        SetupSystemUsageFee(uow);
        var mapper = CreatePassThroughMapper();
        var promo = new Mock<IPromotionService>().Object;
        var promoUsage = new Mock<IPromotionUsageService>().Object;
        var notifier = new Mock<IOrderNotificationPublisher>().Object;
        var sut = CreateSut(uow.Object, mapper, promo, promoUsage, notifier);

        StockLot lot;
        int requestQty = 3;
        if (c == StockQtyCase.Insufficient)
        {
            lot = BuildStockLot(lotId, ProductState.Published, 1m, DateTime.UtcNow.AddDays(3));
        }
        else
        {
            lot = BuildStockLot(lotId, ProductState.Published, 100m, DateTime.UtcNow.AddMinutes(-30));
            requestQty = 1;
        }

        SetupStockLots(uow, [lot]);

        var req = PickupOrderRequest(Guid.NewGuid(), lotId, requestQty, 10m, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateForCustomerAsync(userId, req, CancellationToken.None));

        if (c == StockQtyCase.Insufficient)
            Assert.Contains("không đủ", ex.Message, StringComparison.OrdinalIgnoreCase);
        else
            Assert.Contains("hết hạn", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UTCID09_CutoffSameVnDay_EnforcesPolicy()
    {
        var now = new DateTime(2026, 5, 14, 14, 35, 0, DateTimeKind.Utc);
        var expiry = new DateTime(2026, 5, 14, 15, 45, 0, DateTimeKind.Utc);

        Assert.True(DailyExpiryOrderingPolicy.IsOrderCutoffReached(now));
        Assert.True(DailyExpiryOrderingPolicy.IsExpiringInVietnamToday(expiry, now));
        const string expectedFragment = "21:00";
        Assert.Contains(
            expectedFragment,
            "Sau 21:00, không thể đặt lô hàng có hạn sử dụng trong ngày.",
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task UTCID10_HappyPath_CreatesOrderAndPublishes()
    {
        var userId = Guid.NewGuid();
        var timeSlotId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var lotId = Guid.NewGuid();

        var uow = CreateUowWithUser(ActiveUser(userId));
        SetupSystemUsageFee(uow, 500m);
        var lot = BuildStockLot(lotId, ProductState.Published, 50m, DateTime.UtcNow.AddDays(2));
        SetupStockLots(uow, [lot]);
        SetupProducts(uow,
        [
            new Product
            {
                ProductId = lot.ProductId,
                UnitId = lot.UnitId,
                SupermarketId = Guid.NewGuid(),
                Name = "t",
                Barcode = "",
                Sku = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        ]);

        var orderRepo = new Mock<IOrderRepository>();
        var captured = new Order?[1];
        SetupOrderPersistence(uow, orderRepo, captured);

        var mapper = CreatePassThroughMapper();
        var promo = new Mock<IPromotionService>().Object;
        var promoUsage = new Mock<IPromotionUsageService>().Object;
        var notifier = new Mock<IOrderNotificationPublisher>();
        var sut = CreateSut(uow.Object, mapper, promo, promoUsage, notifier.Object);

        var req = PickupOrderRequest(collectionId, lotId, 2, 10m, timeSlotId);

        var dto = await sut.CreateForCustomerAsync(userId, req, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, dto.OrderId);
        Assert.False(string.IsNullOrWhiteSpace(dto.OrderCode));
        Assert.Equal(userId, dto.UserId);
        Assert.Equal(20m, dto.TotalAmount);

        notifier.Verify(
            x => x.PublishOrderPlacedAsync(dto.OrderId, userId, dto.OrderCode, It.IsAny<CancellationToken>()),
            Times.Once);

        orderRepo.Verify(
            o => o.AddAsync(It.Is<Order>(ord => ord.UserId == userId && ord.OrderItems.Count == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
