using System.Linq.Expressions;
using AutoMapper;
using CloseExpAISolution.Application.Configuration;
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
/// FN022 — UTCID01–UTCID10 per <c>.github/instructions/update-order-status-async-test-sheet.md</c>
/// (<see cref="OrderService.UpdateStatusAsync(Guid, OrderState, string?, CancellationToken)"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~OrderServiceUpdateStatusAsyncTests"</c>
/// </summary>
public sealed class OrderServiceUpdateStatusAsyncTests
{
    private static OrderService CreateSut(IUnitOfWork uow, IOrderNotificationPublisher notifier)
    {
        var unitConv = UnitConversionTestDoubles.PassiveIdentity();
        return new OrderService(
            uow,
            Mock.Of<IMapper>(),
            Mock.Of<IPromotionService>(),
            Mock.Of<IPromotionUsageService>(),
            Options.Create(new PickupSearchOptions()),
            notifier,
            new OrderItemUnitConverter(uow, unitConv),
            new OrderStockQuantityHelper(uow, unitConv));
    }

    private static OrderService CreateSut(IUnitOfWork uow) => CreateSut(uow, Mock.Of<IOrderNotificationPublisher>());

    private static Order MinimalOrder(
        Guid orderId,
        OrderState status,
        DateTime? cancelDeadline = null,
        Guid? deliveryGroupId = null)
    {
        var now = DateTime.UtcNow;
        return new Order
        {
            OrderId = orderId,
            OrderCode = "T-UTC",
            UserId = Guid.NewGuid(),
            TimeSlotId = Guid.NewGuid(),
            CollectionId = null,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = 100m,
            DiscountAmount = 0m,
            FinalAmount = 100m,
            Status = status,
            OrderDate = now.Date,
            AddressId = null,
            PromotionId = null,
            DeliveryGroupId = deliveryGroupId,
            DeliveryFee = 0m,
            SystemUsageFeeAmount = 0m,
            CancelDeadline = cancelDeadline,
            CreatedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-1)
        };
    }

    private static Mock<IOrderRepository> AttachOrderRepo(
        Mock<IUnitOfWork> uow,
        Order order)
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo
            .Setup(o => o.GetByOrderIdAsync(order.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        orderRepo.Setup(o => o.Update(It.IsAny<Order>())).Verifiable();

        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1).Verifiable();
        return orderRepo;
    }

    private static void SetupStatusLog(Mock<IUnitOfWork> uow)
    {
        var logRepo = new Mock<IGenericRepository<OrderStatusLog>>();
        logRepo
            .Setup(r => r.AddAsync(It.IsAny<OrderStatusLog>()))
            .ReturnsAsync((OrderStatusLog l) => l);
        uow.Setup(x => x.Repository<OrderStatusLog>()).Returns(logRepo.Object);
    }

    private static void SetupSystemConfigs(Mock<IUnitOfWork> uow, IReadOnlyList<SystemConfig> configs)
    {
        var list = configs.ToList();
        var configRepo = new Mock<IGenericRepository<SystemConfig>>();
        configRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SystemConfig, bool>>>()))
            .ReturnsAsync((Expression<Func<SystemConfig, bool>> pred) =>
                list.FirstOrDefault(pred.Compile()));
        uow.Setup(x => x.Repository<SystemConfig>()).Returns(configRepo.Object);
    }

    private static void SetupOrderItems(Mock<IUnitOfWork> uow, IReadOnlyList<OrderItem> items)
    {
        var list = items.ToList();
        var itemRepo = new Mock<IGenericRepository<OrderItem>>();
        itemRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<OrderItem, bool>>>()))
            .ReturnsAsync((Expression<Func<OrderItem, bool>> pred) => list.Where(pred.Compile()).ToList());
        uow.Setup(x => x.Repository<OrderItem>()).Returns(itemRepo.Object);
    }

    private static void SetupStockLots(Mock<IUnitOfWork> uow, IReadOnlyList<StockLot> lots)
    {
        var list = lots.ToList();
        var lotRepo = new Mock<IGenericRepository<StockLot>>();
        lotRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .ReturnsAsync((Expression<Func<StockLot, bool>> pred) => list.Where(pred.Compile()).ToList());
        lotRepo.Setup(r => r.Update(It.IsAny<StockLot>())).Verifiable();
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

    private static void SetupDeliveryGroups(Mock<IUnitOfWork> uow, IReadOnlyList<DeliveryGroup> groups)
    {
        var list = groups.ToList();
        var dgRepo = new Mock<IGenericRepository<DeliveryGroup>>();
        dgRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<DeliveryGroup, bool>>>()))
            .ReturnsAsync((Expression<Func<DeliveryGroup, bool>> pred) =>
                list.FirstOrDefault(pred.Compile()));
        dgRepo.Setup(r => r.Update(It.IsAny<DeliveryGroup>())).Verifiable();
        uow.Setup(x => x.Repository<DeliveryGroup>()).Returns(dgRepo.Object);
    }

    /// <summary>UTCID01 — Missing order (<see cref="KeyNotFoundException"/>).</summary>
    [Fact]
    public async Task UTCID01_OrderNotFound_ThrowsKeyNotFoundException()
    {
        var orderId = Guid.NewGuid();
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(o => o.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);
        SetupStatusLog(uow);

        var sut = CreateSut(uow.Object);
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.UpdateStatusAsync(orderId, OrderState.Paid, CancellationToken.None));

        Assert.Contains(orderId.ToString(), ex.Message, StringComparison.OrdinalIgnoreCase);

        orderRepo.Verify(o => o.Update(It.IsAny<Order>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID02 — Same status: early exit (no <c>Update</c> / <c>SaveChanges</c> / publisher).</summary>
    [Fact]
    public async Task UTCID02_SameOldAndNew_NoPersistenceOrPublish()
    {
        var orderId = Guid.NewGuid();
        var order = MinimalOrder(orderId, OrderState.Pending);

        var uow = new Mock<IUnitOfWork>();
        var orderRepo = AttachOrderRepo(uow, order);
        SetupStatusLog(uow);

        var notifier = new Mock<IOrderNotificationPublisher>();
        notifier
            .Setup(n => n.PublishOrderStatusChangedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<OrderState>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(uow.Object, notifier.Object);

        await sut.UpdateStatusAsync(orderId, OrderState.Pending, statusNote: null, CancellationToken.None);

        orderRepo.Verify(o => o.Update(It.IsAny<Order>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        notifier.Verify(
            n => n.PublishOrderStatusChangedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<OrderState>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>UTCID03 — Cancel with blank <c>statusNote</c>.</summary>
    [Fact]
    public async Task UTCID03_CanceledWithoutReason_ThrowsInvalidOperationException()
    {
        var orderId = Guid.NewGuid();
        var order = MinimalOrder(orderId, OrderState.Pending);

        var uow = new Mock<IUnitOfWork>();
        var orderRepo = AttachOrderRepo(uow, order);
        SetupStatusLog(uow);

        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateStatusAsync(orderId, OrderState.Canceled, " \t\r\n", CancellationToken.None));

        Assert.Contains("lý do", ex.Message, StringComparison.OrdinalIgnoreCase);

        orderRepo.Verify(o => o.Update(It.IsAny<Order>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID04 — Transition to Paid sets <see cref="Order.CancelDeadline"/> from <see cref="SystemConfigKeys.OrderCancelWindowMinutesAfterPaid"/>.</summary>
    [Fact]
    public async Task UTCID04_ToPaid_WithNoDeadline_LoadsCancelWindow_ConfiguresDeadline()
    {
        var orderId = Guid.NewGuid();
        var order = MinimalOrder(orderId, OrderState.Pending);

        var uow = new Mock<IUnitOfWork>();
        var orderRepo = AttachOrderRepo(uow, order);
        SetupStatusLog(uow);
        SetupSystemConfigs(uow,
        [
            new SystemConfig
            {
                ConfigKey = SystemConfigKeys.OrderCancelWindowMinutesAfterPaid,
                ConfigValue = "45"
            }
        ]);

        var notifier = new Mock<IOrderNotificationPublisher>();
        notifier
            .Setup(n => n.PublishOrderStatusChangedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<OrderState>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(uow.Object, notifier.Object);

        await sut.UpdateStatusAsync(orderId, OrderState.Paid, CancellationToken.None);

        Assert.True(order.CancelDeadline.HasValue);
        Assert.True(order.CancelDeadline.Value >= DateTime.UtcNow.AddMinutes(44));

        notifier.Verify(
            n => n.PublishOrderStatusChangedAsync(
                order.OrderId,
                order.UserId,
                order.OrderCode,
                OrderState.Paid,
                It.IsAny<CancellationToken>()),
            Times.Once);

        orderRepo.Verify(o => o.Update(It.IsAny<Order>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(OrderState.Paid, order.Status);
    }

    /// <summary>UTCID05 — Paid → Canceled but <see cref="Order.CancelDeadline"/> missing.</summary>
    [Fact]
    public async Task UTCID05_PaidCancel_MissingDeadline_ThrowsInvalidOperationException()
    {
        var orderId = Guid.NewGuid();
        var order = MinimalOrder(orderId, OrderState.Paid, cancelDeadline: null);

        var uow = new Mock<IUnitOfWork>();
        var orderRepo = AttachOrderRepo(uow, order);
        SetupStatusLog(uow);

        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateStatusAsync(orderId, OrderState.Canceled, "customer request", CancellationToken.None));

        Assert.Contains("CancelDeadline", ex.Message, StringComparison.OrdinalIgnoreCase);

        orderRepo.Verify(o => o.Update(It.IsAny<Order>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID06 — Paid → Canceled after cancel window passed.</summary>
    [Fact]
    public async Task UTCID06_PaidCancel_AfterDeadline_ThrowsInvalidOperationException()
    {
        var orderId = Guid.NewGuid();
        var order = MinimalOrder(
            orderId,
            OrderState.Paid,
            cancelDeadline: DateTime.UtcNow.AddHours(-24));

        var uow = new Mock<IUnitOfWork>();
        var orderRepo = AttachOrderRepo(uow, order);
        SetupStatusLog(uow);

        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateStatusAsync(orderId, OrderState.Canceled, "late cancel attempt", CancellationToken.None));

        Assert.Contains("quá thời gian", ex.Message, StringComparison.OrdinalIgnoreCase);

        orderRepo.Verify(o => o.Update(It.IsAny<Order>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID07 — Cancel from forbidden <c>OrderState</c> (non Pending/Paid).</summary>
    [Fact]
    public async Task UTCID07_CompletedToCanceled_ThrowsInvalidOperationException()
    {
        var orderId = Guid.NewGuid();
        var order = MinimalOrder(orderId, OrderState.Completed);

        var uow = new Mock<IUnitOfWork>();
        var orderRepo = AttachOrderRepo(uow, order);
        SetupStatusLog(uow);

        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateStatusAsync(orderId, OrderState.Canceled, "no longer wants", CancellationToken.None));

        Assert.Contains("Không thể hủy", ex.Message, StringComparison.OrdinalIgnoreCase);

        orderRepo.Verify(o => o.Update(It.IsAny<Order>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID08 — Pending → Canceled succeeds; no stock restore branch for non-Paid old status.</summary>
    [Fact]
    public async Task UTCID08_PendingToCanceled_Succeeds_NoStockRestoreNeeded()
    {
        var orderId = Guid.NewGuid();
        var order = MinimalOrder(orderId, OrderState.Pending);

        var uow = new Mock<IUnitOfWork>();
        var orderRepo = AttachOrderRepo(uow, order);
        SetupStatusLog(uow);

        SetupOrderItems(uow, Array.Empty<OrderItem>());
        SetupStockLots(uow, Array.Empty<StockLot>());

        var notifier = new Mock<IOrderNotificationPublisher>();
        notifier
            .Setup(n => n.PublishOrderStatusChangedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<OrderState>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(uow.Object, notifier.Object);

        await sut.UpdateStatusAsync(orderId, OrderState.Canceled, "changed mind", CancellationToken.None);

        Assert.Equal(OrderState.Canceled, order.Status);
        orderRepo.Verify(o => o.Update(order), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        notifier.Verify(
            n => n.PublishOrderStatusChangedAsync(
                order.OrderId,
                order.UserId,
                order.OrderCode,
                OrderState.Canceled,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>UTCID09 — Paid → Canceled restores <see cref="StockLot"/> quantities.</summary>
    [Fact]
    public async Task UTCID09_PaidToCanceled_RestoresStockForLots()
    {
        var orderId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        var deadline = DateTime.UtcNow.AddDays(7);

        var order = MinimalOrder(orderId, OrderState.Paid, cancelDeadline: deadline);

        var now = DateTime.UtcNow;
        var lotBefore = new StockLot
        {
            LotId = lotId,
            ProductId = Guid.NewGuid(),
            UnitId = Guid.NewGuid(),
            ExpiryDate = now.AddMonths(3),
            ManufactureDate = now.AddMonths(-3),
            Quantity = 3m,
            OriginalUnitPrice = 5m,
            SuggestedUnitPrice = 5m,
            FinalUnitPrice = 5m,
            Weight = 1m,
            Status = ProductState.Verified,
            CreatedAt = now,
            UpdatedAt = now
        };

        var items = new List<OrderItem>
        {
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = orderId,
                LotId = lotId,
                Quantity = 2,
                UnitPrice = 5m,
                TotalPrice = 10m,
                PackagingStatus = PackagingState.Pending
            }
        };

        var uow = new Mock<IUnitOfWork>();
        var orderRepo = AttachOrderRepo(uow, order);
        SetupStatusLog(uow);
        SetupOrderItems(uow, items);

        var lotCopy = CloneLot(lotBefore);
        SetupStockLots(uow, new[] { lotCopy });
        SetupProducts(uow,
        [
            new Product
            {
                ProductId = lotBefore.ProductId,
                UnitId = lotBefore.UnitId,
                SupermarketId = Guid.NewGuid(),
                Name = "t",
                Barcode = "",
                Sku = "",
                CreatedAt = now,
                UpdatedAt = now
            }
        ]);

        var notifier = new Mock<IOrderNotificationPublisher>();
        notifier
            .Setup(n => n.PublishOrderStatusChangedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<OrderState>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(uow.Object, notifier.Object);

        await sut.UpdateStatusAsync(orderId, OrderState.Canceled, "refund workflow", CancellationToken.None);

        Assert.Equal(5m, lotCopy.Quantity); // was 3, +2 restored
        Assert.Equal(OrderState.Canceled, order.Status);
        orderRepo.Verify(o => o.Update(It.IsAny<Order>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID10 — ReadyToShip → Refunded restores stock and detaches <see cref="Order.DeliveryGroupId"/>.</summary>
    [Fact]
    public async Task UTCID10_ReadyToShipToRefunded_RestocksAndDetachesGroup()
    {
        var orderId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var lotId = Guid.NewGuid();

        var order = MinimalOrder(orderId, OrderState.ReadyToShip, deliveryGroupId: groupId);

        var now = DateTime.UtcNow;
        var lots = new List<StockLot>
        {
            new()
            {
                LotId = lotId,
                ProductId = Guid.NewGuid(),
                UnitId = Guid.NewGuid(),
                ExpiryDate = now.AddMonths(2),
                ManufactureDate = now.AddMonths(-2),
                Quantity = 1m,
                OriginalUnitPrice = 4m,
                SuggestedUnitPrice = 4m,
                FinalUnitPrice = 4m,
                Weight = 1m,
                Status = ProductState.Verified,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var items = new List<OrderItem>
        {
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = orderId,
                LotId = lotId,
                Quantity = 4,
                UnitPrice = 4m,
                TotalPrice = 16m,
                PackagingStatus = PackagingState.Completed
            }
        };

        var dg = new DeliveryGroup
        {
            DeliveryGroupId = groupId,
            GroupCode = "DG-X",
            TimeSlotId = order.TimeSlotId,
            DeliveryType = DeliveryMethod.Pickup,
            DeliveryArea = "A",
            Status = DeliveryGroupState.Pending,
            TotalOrders = 3,
            DeliveryDate = order.OrderDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        var uow = new Mock<IUnitOfWork>();
        var orderRepo = AttachOrderRepo(uow, order);
        SetupStatusLog(uow);
        SetupOrderItems(uow, items);
        SetupStockLots(uow, lots);
        SetupProducts(uow,
        [
            new Product
            {
                ProductId = lots[0].ProductId,
                UnitId = lots[0].UnitId,
                SupermarketId = Guid.NewGuid(),
                Name = "t",
                Barcode = "",
                Sku = "",
                CreatedAt = now,
                UpdatedAt = now
            }
        ]);
        SetupDeliveryGroups(uow, new[] { dg });

        var notifier = new Mock<IOrderNotificationPublisher>();
        notifier
            .Setup(n => n.PublishOrderStatusChangedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<OrderState>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(uow.Object, notifier.Object);

        await sut.UpdateStatusAsync(orderId, OrderState.Refunded, "quality issue after pack", CancellationToken.None);

        Assert.Null(order.DeliveryGroupId);
        Assert.Equal(OrderState.Refunded, order.Status);
        Assert.Equal(5m, lots[0].Quantity); // 1 + 4
        Assert.Equal(2, dg.TotalOrders); // decremented once

        uow.Verify(x => x.Repository<DeliveryGroup>(), Times.AtLeastOnce);
        notifier.Verify(
            n => n.PublishOrderStatusChangedAsync(
                order.OrderId,
                order.UserId,
                order.OrderCode,
                OrderState.Refunded,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static StockLot CloneLot(StockLot source)
    {
        var now = DateTime.UtcNow;
        return new StockLot
        {
            LotId = source.LotId,
            ProductId = source.ProductId,
            UnitId = source.UnitId,
            ExpiryDate = source.ExpiryDate,
            ManufactureDate = source.ManufactureDate,
            Quantity = source.Quantity,
            OriginalUnitPrice = source.OriginalUnitPrice,
            SuggestedUnitPrice = source.SuggestedUnitPrice,
            FinalUnitPrice = source.FinalUnitPrice,
            Weight = source.Weight,
            Status = source.Status,
            CreatedAt = now,
            UpdatedAt = source.UpdatedAt
        };
    }
}
