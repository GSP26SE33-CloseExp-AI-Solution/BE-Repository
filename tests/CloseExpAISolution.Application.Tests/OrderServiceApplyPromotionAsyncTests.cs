using System.Linq.Expressions;
using AutoMapper;
using CloseExpAISolution.Application;
using CloseExpAISolution.Application.Configuration;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
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
/// FN023 — UTCID01–UTCID10 per <c>.github/instructions/apply-promotion-async-test-sheet.md</c>
/// (<see cref="OrderService.ApplyPromotionAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~OrderServiceApplyPromotionAsyncTests"</c>
/// </summary>
public sealed class OrderServiceApplyPromotionAsyncTests
{
    private static OrderService CreateSut(
        IUnitOfWork uow,
        IMapper mapper,
        IPromotionService promotionService,
        IPromotionUsageService promotionUsageService)
    {
        var unitConv = UnitConversionTestDoubles.PassiveIdentity();
        return new OrderService(
            uow,
            mapper,
            promotionService,
            promotionUsageService,
            Options.Create(new PickupSearchOptions()),
            Mock.Of<IOrderNotificationPublisher>(),
            new OrderItemUnitConverter(uow, unitConv),
            new OrderStockQuantityHelper(uow, unitConv));
    }

    private static Order BuildOrder(Guid orderId, Guid ownerUserId, OrderState status, decimal totalAmount, decimal deliveryFee = 5000m)
    {
        var now = DateTime.UtcNow;
        return new Order
        {
            OrderId = orderId,
            OrderCode = "FN023-T",
            UserId = ownerUserId,
            TimeSlotId = Guid.NewGuid(),
            CollectionId = null,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = totalAmount,
            DiscountAmount = 0m,
            FinalAmount = totalAmount + deliveryFee,
            Status = status,
            OrderDate = now.Date,
            AddressId = null,
            PromotionId = null,
            DeliveryFee = deliveryFee,
            SystemUsageFeeAmount = 0m,
            DeliveryNote = null,
            CancelDeadline = null,
            DeliveryGroupId = null,
            CreatedAt = now.AddMinutes(-60),
            UpdatedAt = now.AddMinutes(-50)
        };
    }

    private static IMapper CreateMapper()
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

    private static void AttachOrderDetailsRepo(Mock<IUnitOfWork> uow, Order order)
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo
            .Setup(o => o.GetByIdWithDetailsAsync(order.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orderRepo.Setup(o => o.Update(It.IsAny<Order>())).Verifiable();
        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static void AttachSystemFees(Mock<IUnitOfWork> uow, decimal systemUsageFeeVnd)
    {
        var configs = new List<SystemConfig>
        {
            new()
            {
                ConfigKey = SystemConfigKeys.OrderSystemUsageFeeVnd,
                ConfigValue = systemUsageFeeVnd.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }
        };
        var configRepo = new Mock<IGenericRepository<SystemConfig>>();
        configRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SystemConfig, bool>>>()))
            .ReturnsAsync((Expression<Func<SystemConfig, bool>> pred) =>
                configs.FirstOrDefault(pred.Compile()));
        uow.Setup(x => x.Repository<SystemConfig>()).Returns(configRepo.Object);
    }

    private static void AttachEmptySystemConfigs(Mock<IUnitOfWork> uow)
    {
        var configRepo = new Mock<IGenericRepository<SystemConfig>>();
        configRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SystemConfig, bool>>>()))
            .ReturnsAsync((Expression<Func<SystemConfig, bool>> _) => null);
        uow.Setup(x => x.Repository<SystemConfig>()).Returns(configRepo.Object);
    }

    [Fact]
    public async Task UTCID01_OrderNotFound_ThrowsKeyNotFound_NoValidateOrPersist()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(o => o.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);

        var promo = new Mock<IPromotionService>();

        var sut = CreateSut(uow.Object, CreateMapper(), promo.Object, Mock.Of<IPromotionUsageService>());

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ApplyPromotionAsync(orderId, userId, new ApplyPromotionToOrderRequestDto(), CancellationToken.None));

        Assert.Contains(orderId.ToString(), ex.Message, StringComparison.OrdinalIgnoreCase);

        promo.Verify(p => p.ValidatePromotionAsync(
            It.IsAny<Guid>(), It.IsAny<ValidatePromotionRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID02_OtherCustomerOrder_ThrowsInvalidOperation_NoPromotionCall()
    {
        var orderId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var caller = Guid.NewGuid();
        var order = BuildOrder(orderId, owner, OrderState.Pending, 100_000m);

        var uow = new Mock<IUnitOfWork>();
        AttachOrderDetailsRepo(uow, order);

        var promo = new Mock<IPromotionService>();
        var promoUsage = new Mock<IPromotionUsageService>();

        var sut = CreateSut(uow.Object, CreateMapper(), promo.Object, promoUsage.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ApplyPromotionAsync(orderId, caller, new ApplyPromotionToOrderRequestDto(), CancellationToken.None));

        Assert.Contains("quyền", ex.Message, StringComparison.OrdinalIgnoreCase);

        promo.Verify(p => p.ValidatePromotionAsync(
            It.IsAny<Guid>(), It.IsAny<ValidatePromotionRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        promoUsage.Verify(p => p.RecordUsageAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID03_PromotionInvalid_Throws_InvalidatesMessage_FromValidator()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var promoId = Guid.NewGuid();
        var order = BuildOrder(orderId, userId, OrderState.Pending, 250_000m);

        var uow = new Mock<IUnitOfWork>();
        AttachOrderDetailsRepo(uow, order);
        AttachSystemFees(uow, 2000m);

        var promo = new Mock<IPromotionService>();
        promo
            .Setup(p => p.ValidatePromotionAsync(userId, It.IsAny<ValidatePromotionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromotionValidationResultDto
            {
                IsValid = false,
                Message = "Mã không còn hiệu lực.",
                PromotionId = null,
                DiscountAmount = 0m
            });

        var sut = CreateSut(uow.Object, CreateMapper(), promo.Object, Mock.Of<IPromotionUsageService>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ApplyPromotionAsync(orderId, userId,
                new ApplyPromotionToOrderRequestDto { PromotionId = promoId },
                CancellationToken.None));

        Assert.Equal("Mã không còn hiệu lực.", ex.Message);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID04_ValidFlagButPromotionIdAbsent_ThrowsMessage()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var order = BuildOrder(orderId, userId, OrderState.Pending, 80_000m);

        var uow = new Mock<IUnitOfWork>();
        AttachOrderDetailsRepo(uow, order);
        AttachSystemFees(uow, 1000m);

        var promo = new Mock<IPromotionService>();
        promo
            .Setup(p => p.ValidatePromotionAsync(userId, It.IsAny<ValidatePromotionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromotionValidationResultDto
            {
                IsValid = true,
                PromotionId = null,
                DiscountAmount = 999m,
                Message = "ambiguous server state"
            });

        var sut = CreateSut(uow.Object, CreateMapper(), promo.Object, Mock.Of<IPromotionUsageService>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ApplyPromotionAsync(orderId, userId, new ApplyPromotionToOrderRequestDto(), CancellationToken.None));

        Assert.Contains("ambiguous", ex.Message, StringComparison.OrdinalIgnoreCase);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID05_PendingOrder_ValidPromotion_PersistsAmounts_NoRecordUsage()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var promotionIdResolved = Guid.NewGuid();
        const decimal sysFee = 3000m;
        const decimal discount = 15_000m;
        var order = BuildOrder(orderId, userId, OrderState.Pending, 100_000m, deliveryFee: 12_000m);

        var uow = new Mock<IUnitOfWork>();
        AttachOrderDetailsRepo(uow, order);
        AttachSystemFees(uow, sysFee);

        var promo = new Mock<IPromotionService>();
        promo
            .Setup(p => p.ValidatePromotionAsync(userId, It.IsAny<ValidatePromotionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromotionValidationResultDto
            {
                IsValid = true,
                PromotionId = promotionIdResolved,
                DiscountAmount = discount,
                Message = string.Empty,
                OriginalAmount = order.TotalAmount,
                FinalAmount = order.TotalAmount - discount
            });

        var promoUsage = new Mock<IPromotionUsageService>();

        var sut = CreateSut(uow.Object, CreateMapper(), promo.Object, promoUsage.Object);

        var dto = await sut.ApplyPromotionAsync(orderId, userId,
            new ApplyPromotionToOrderRequestDto { PromotionId = promotionIdResolved },
            CancellationToken.None);

        Assert.Equal(promotionIdResolved, dto.PromotionId);
        Assert.Equal(order.PromotionId, promotionIdResolved);
        Assert.Equal(sysFee, order.SystemUsageFeeAmount);
        var expectedFinal = OrderTotalsHelper.ComputeFinalAmount(order.TotalAmount, discount, order.DeliveryFee, sysFee);
        Assert.Equal(expectedFinal, order.FinalAmount);
        Assert.Equal(expectedFinal, dto.FinalAmount);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        promoUsage.Verify(p => p.RecordUsageAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID06_PaidOrder_WithDiscount_TriggersSingleRecordUsageCall()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var promotionIdResolved = Guid.NewGuid();
        const decimal discount = 5555m;
        var order = BuildOrder(orderId, userId, OrderState.Paid, 555_555m);

        var uow = new Mock<IUnitOfWork>();
        AttachOrderDetailsRepo(uow, order);
        AttachSystemFees(uow, 0m);

        var promo = new Mock<IPromotionService>();
        promo
            .Setup(p => p.ValidatePromotionAsync(userId, It.IsAny<ValidatePromotionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromotionValidationResultDto
            {
                IsValid = true,
                PromotionId = promotionIdResolved,
                DiscountAmount = discount,
                Message = string.Empty,
                OriginalAmount = order.TotalAmount,
                FinalAmount = order.TotalAmount - discount
            });

        var promoUsage = new Mock<IPromotionUsageService>();
        promoUsage
            .Setup(p => p.RecordUsageAsync(
                promotionIdResolved, userId, orderId, discount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromotionUsageDto());

        var sut = CreateSut(uow.Object, CreateMapper(), promo.Object, promoUsage.Object);

        await sut.ApplyPromotionAsync(orderId, userId,
            new ApplyPromotionToOrderRequestDto { PromotionCode = "PROMO-X" }, CancellationToken.None);

        promoUsage.Verify(p => p.RecordUsageAsync(promotionIdResolved, userId, orderId, discount, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UTCID07_MissingUsageFeeSystemConfig_SaveNotCalled_InvalidOperation()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var promoId = Guid.NewGuid();
        var order = BuildOrder(orderId, userId, OrderState.Pending, 10_000m);

        var uow = new Mock<IUnitOfWork>();
        AttachOrderDetailsRepo(uow, order);
        AttachEmptySystemConfigs(uow);

        var promo = new Mock<IPromotionService>();
        promo
            .Setup(p => p.ValidatePromotionAsync(userId, It.IsAny<ValidatePromotionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromotionValidationResultDto
            {
                IsValid = true,
                PromotionId = promoId,
                DiscountAmount = 100m,
                Message = string.Empty,
                OriginalAmount = order.TotalAmount,
                FinalAmount = order.TotalAmount - 100m
            });

        var sut = CreateSut(uow.Object, CreateMapper(), promo.Object, Mock.Of<IPromotionUsageService>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ApplyPromotionAsync(orderId, userId,
                new ApplyPromotionToOrderRequestDto { PromotionId = promoId },
                CancellationToken.None));

        Assert.Contains(SystemConfigKeys.OrderSystemUsageFeeVnd, ex.Message, StringComparison.OrdinalIgnoreCase);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID08_AppliesViaPromotionCode_ResolvesPromotionId()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var promoIdReturned = Guid.NewGuid();
        var order = BuildOrder(orderId, userId, OrderState.Pending, 44_444m);

        var uow = new Mock<IUnitOfWork>();
        AttachOrderDetailsRepo(uow, order);
        AttachSystemFees(uow, 500m);

        var promo = new Mock<IPromotionService>();
        ValidatePromotionRequestDto? captured = null;
        promo
            .Setup(p => p.ValidatePromotionAsync(userId, It.IsAny<ValidatePromotionRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, ValidatePromotionRequestDto, CancellationToken>((_, req, _) => captured = req)
            .ReturnsAsync(new PromotionValidationResultDto
            {
                IsValid = true,
                PromotionId = promoIdReturned,
                DiscountAmount = 4000m,
                Message = string.Empty,
                OriginalAmount = order.TotalAmount,
                FinalAmount = order.TotalAmount - 4000m
            });

        var sut = CreateSut(uow.Object, CreateMapper(), promo.Object, Mock.Of<IPromotionUsageService>());

        await sut.ApplyPromotionAsync(orderId, userId,
            new ApplyPromotionToOrderRequestDto { PromotionCode = "CODE-ONLY", PromotionId = null }, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Null(captured.PromotionId);
        Assert.Equal("CODE-ONLY", captured.PromotionCode);
        Assert.Equal(order.TotalAmount, captured.TotalAmount);
        Assert.Equal(promoIdReturned, order.PromotionId);
    }

    [Fact]
    public async Task UTCID09_ForwardsCancellationTokenThroughDependencies()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var promoId = Guid.NewGuid();
        var order = BuildOrder(orderId, userId, OrderState.Pending, 20_000m);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var capturedGetById = new List<CancellationToken>();
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo
            .Setup(o => o.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, ct) => capturedGetById.Add(ct))
            .ReturnsAsync(order);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct =>
            {
                Assert.Equal(token, ct);
            })
            .ReturnsAsync(1);

        AttachSystemFees(uow, 0m);

        var promo = new Mock<IPromotionService>();
        CancellationToken? capturedPromoCt = null;
        promo
            .Setup(p => p.ValidatePromotionAsync(userId, It.IsAny<ValidatePromotionRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, ValidatePromotionRequestDto, CancellationToken>((_, _, ct) => capturedPromoCt = ct)
            .ReturnsAsync(new PromotionValidationResultDto
            {
                IsValid = true,
                PromotionId = promoId,
                DiscountAmount = 999m,
                Message = string.Empty,
                OriginalAmount = order.TotalAmount,
                FinalAmount = order.TotalAmount - 999m
            });

        var sut = CreateSut(uow.Object, CreateMapper(), promo.Object, Mock.Of<IPromotionUsageService>());

        await sut.ApplyPromotionAsync(orderId, userId,
            new ApplyPromotionToOrderRequestDto { PromotionId = promoId }, token);

        Assert.Equal(2, capturedGetById.Count);
        Assert.All(capturedGetById, ct => Assert.Equal(token, ct));
        Assert.True(capturedPromoCt.HasValue && capturedPromoCt.Value == token);
    }

    [Fact]
    public async Task UTCID10_PaidOrder_ZeroDiscount_SavesButSkipsPromotionUsageLedger()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var promoId = Guid.NewGuid();
        var order = BuildOrder(orderId, userId, OrderState.Paid, 99_000m);

        var uow = new Mock<IUnitOfWork>();
        AttachOrderDetailsRepo(uow, order);
        AttachSystemFees(uow, 100m);

        var promo = new Mock<IPromotionService>();
        promo
            .Setup(p => p.ValidatePromotionAsync(userId, It.IsAny<ValidatePromotionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromotionValidationResultDto
            {
                IsValid = true,
                PromotionId = promoId,
                DiscountAmount = 0m,
                Message = string.Empty,
                OriginalAmount = order.TotalAmount,
                FinalAmount = order.TotalAmount
            });

        var promoUsage = new Mock<IPromotionUsageService>();

        var sut = CreateSut(uow.Object, CreateMapper(), promo.Object, promoUsage.Object);

        await sut.ApplyPromotionAsync(orderId, userId,
            new ApplyPromotionToOrderRequestDto { PromotionId = promoId }, CancellationToken.None);

        Assert.Equal(promoId, order.PromotionId);
        Assert.Equal(0m, order.DiscountAmount);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        promoUsage.Verify(p => p.RecordUsageAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
