using AutoMapper;
using CloseExpAISolution.Application.Configuration;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Repositories.Interface;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN021 — UTCID01–UTCID05 per <c>.github/instructions/get-order-details-async-test-sheet.md</c>
/// (<see cref="OrderService.GetByIdWithDetailsAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~OrderServiceGetByIdWithDetailsAsyncTests"</c>
/// </summary>
public sealed class OrderServiceGetByIdWithDetailsAsyncTests
{
    private static OrderService CreateSut(IUnitOfWork uow, IMapper mapper)
    {
        var unitConv = UnitConversionTestDoubles.PassiveIdentity();
        return new OrderService(
            uow,
            mapper,
            Mock.Of<IPromotionService>(),
            Mock.Of<IPromotionUsageService>(),
            Options.Create(new PickupSearchOptions()),
            Mock.Of<IOrderNotificationPublisher>(),
            new OrderItemUnitConverter(uow, unitConv),
            new OrderStockQuantityHelper(uow, unitConv));
    }

    [Fact]
    public async Task UTCID01_RepoNull_ReturnsNull_MapNotCalled()
    {
        var orderId = Guid.NewGuid();
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo
            .Setup(o => o.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);

        var mapper = new Mock<IMapper>();
        var sut = CreateSut(uow.Object, mapper.Object);

        var result = await sut.GetByIdWithDetailsAsync(orderId, CancellationToken.None);

        Assert.Null(result);
        mapper.Verify(m => m.Map<OrderResponseDto>(It.IsAny<Order>()), Times.Never);
        orderRepo.Verify(o => o.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UTCID02_RepoReturnsOrder_ReturnsDtoWithMatchingIds()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entity = new Order
        {
            OrderId = orderId,
            OrderCode = "ORD-X",
            UserId = userId,
            DeliveryType = DeliveryMethod.Pickup,
            TimeSlotId = Guid.NewGuid(),
            TotalAmount = 10m,
            OrderDate = DateTime.UtcNow,
            Status = OrderState.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo
            .Setup(o => o.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);

        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<OrderResponseDto>(It.IsAny<Order>()))
            .Returns((Order o) => new OrderResponseDto
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                UserId = o.UserId,
                DeliveryType = o.DeliveryType,
                TimeSlotId = o.TimeSlotId,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString()
            });

        var sut = CreateSut(uow.Object, mapper.Object);

        var dto = await sut.GetByIdWithDetailsAsync(orderId, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(orderId, dto.OrderId);
        Assert.Equal("ORD-X", dto.OrderCode);
        Assert.Equal(userId, dto.UserId);
    }

    [Fact]
    public async Task UTCID03_ForwardsCancellationTokenToRepository()
    {
        var orderId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        CancellationToken? captured = null;
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo
            .Setup(o => o.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, ct) => captured = ct)
            .ReturnsAsync((Order?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);

        var mapper = new Mock<IMapper>();
        var sut = CreateSut(uow.Object, mapper.Object);

        await sut.GetByIdWithDetailsAsync(orderId, token);

        Assert.True(captured.HasValue);
        Assert.Equal(token, captured.Value);
    }

    [Fact]
    public async Task UTCID04_MapInvokedExactlyOnce_WhenOrderExists()
    {
        var orderId = Guid.NewGuid();
        var entity = new Order
        {
            OrderId = orderId,
            OrderCode = "ORD-1",
            UserId = Guid.NewGuid(),
            DeliveryType = DeliveryMethod.Pickup,
            TimeSlotId = Guid.NewGuid(),
            TotalAmount = 1m,
            OrderDate = DateTime.UtcNow,
            Status = OrderState.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo
            .Setup(o => o.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);

        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<OrderResponseDto>(It.IsAny<Order>()))
            .Returns(new OrderResponseDto { OrderId = orderId, OrderCode = "ORD-1" });

        var sut = CreateSut(uow.Object, mapper.Object);

        _ = await sut.GetByIdWithDetailsAsync(orderId, CancellationToken.None);

        mapper.Verify(m => m.Map<OrderResponseDto>(It.Is<Order>(o => o.OrderId == orderId)), Times.Once);
    }

    [Fact]
    public async Task UTCID05_OrderWithItems_MapsOrderItemsPerProfile()
    {
        var orderId = Guid.NewGuid();
        var item1 = Guid.NewGuid();
        var item2 = Guid.NewGuid();
        var lot1 = Guid.NewGuid();
        var lot2 = Guid.NewGuid();

        var entity = new Order
        {
            OrderId = orderId,
            OrderCode = "ORD-M",
            UserId = Guid.NewGuid(),
            DeliveryType = DeliveryMethod.Pickup,
            TimeSlotId = Guid.NewGuid(),
            TotalAmount = 30m,
            OrderDate = DateTime.UtcNow,
            Status = OrderState.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderItems =
            [
                new OrderItem
                {
                    OrderItemId = item1,
                    OrderId = orderId,
                    LotId = lot1,
                    Quantity = 1,
                    UnitPrice = 10m,
                    TotalPrice = 10m
                },
                new OrderItem
                {
                    OrderItemId = item2,
                    OrderId = orderId,
                    LotId = lot2,
                    Quantity = 2,
                    UnitPrice = 10m,
                    TotalPrice = 20m
                }
            ]
        };

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo
            .Setup(o => o.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);

        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<OrderResponseDto>(It.IsAny<Order>()))
            .Returns((Order o) =>
            {
                var dto = new OrderResponseDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    UserId = o.UserId,
                    DeliveryType = o.DeliveryType,
                    TimeSlotId = o.TimeSlotId,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status.ToString()
                };
                foreach (var i in o.OrderItems)
                {
                    dto.OrderItems.Add(new OrderItemResponseDto
                    {
                        OrderItemId = i.OrderItemId,
                        OrderId = o.OrderId,
                        LotId = i.LotId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.TotalPrice,
                        PackagingStatus = i.PackagingStatus.ToString()
                    });
                }

                return dto;
            });

        var sut = CreateSut(uow.Object, mapper.Object);

        var dto = await sut.GetByIdWithDetailsAsync(orderId, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(2, dto!.OrderItems.Count);
        Assert.Contains(dto.OrderItems, x => x.OrderItemId == item1 && x.LotId == lot1 && x.Quantity == 1);
        Assert.Contains(dto.OrderItems, x => x.OrderItemId == item2 && x.LotId == lot2 && x.Quantity == 2);
    }
}
