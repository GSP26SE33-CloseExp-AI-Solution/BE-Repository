using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Application.Services.Routing;
using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN028 — Columns 01–12 of <c>.github/instructions/confirm-delivery-async-test-sheet.md</c>
/// (<see cref="DeliveryService.ConfirmDeliveryAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~DeliveryServiceConfirmDeliveryTests"</c>
/// </summary>
public sealed class DeliveryServiceConfirmDeliveryTests
{
    private const string ValidProofHttps = "https://proof.cdn.example/order-proof.png";

    private static readonly Guid TimeSlotId = Guid.Parse("cccc7001-0001-0001-0001-000000000001");
    private static readonly Guid CollectionId = Guid.Parse("dddd7001-0001-0001-0001-000000000001");
    private static readonly Guid CustomerId = Guid.Parse("ffffffff-7000-0000-0000-000000000000");
    private static readonly Guid DeliveryAId = Guid.Parse("bbbbbbbb-7005-bbbb-bbbb-bbbbbbbbbbb5");
    private static readonly Guid DeliveryBId = Guid.Parse("bbbbbbbb-7006-bbbb-bbbb-bbbbbbbbbbb6");
    private static readonly Guid UnitId = Guid.Parse("aaaa7001-0001-0001-0001-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("ccca7001-0001-0001-0001-000000000001");
    private static readonly Guid SupermarketId = Guid.Parse("11111117-1111-1111-1111-111111111111");
    private static readonly Guid ProductId = Guid.Parse("bbbb7001-0001-0001-0001-000000000001");
    private static readonly Guid LotId = Guid.Parse("dddd7003-0003-0003-0003-000000000003");

    private static readonly Guid GroupId = Guid.Parse("eeee7001-7001-7001-7001-000000000001");

    private static readonly Guid OrderId = Guid.Parse("eeee7020-7020-7020-7020-000000000020");
    private static readonly Guid OrderItemId1 = Guid.Parse("eeee7021-7021-7021-7021-000000000021");
    private static readonly Guid OrderItemId2 = Guid.Parse("eeee7022-7022-7022-7022-000000000022");

    private static (SqliteConnection Conn, ApplicationDbContext Ctx, DeliveryService Sut, Mock<IOrderNotificationPublisher> Notifier, Mock<ILogger<DeliveryService>> Logger) CreateSut()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new ApplicationDbContext(options);
        ctx.Database.EnsureCreated();

        var uow = new UnitOfWork(ctx);
        var r2 = Mock.Of<IR2StorageService>();
        var mapbox = Mock.Of<IMapboxService>();
        var optimization = Mock.Of<IMapboxOptimizationService>();
        var logger = new Mock<ILogger<DeliveryService>>();
        var notifier = new Mock<IOrderNotificationPublisher>();
        notifier
            .Setup(n => n.PublishDeliveryStatusChildAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DeliveryState>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hybrid = new HybridRoutingStrategy(
            mapbox,
            optimization,
            new Mock<ILogger<HybridRoutingStrategy>>().Object);

        var sut = new DeliveryService(uow, r2, mapbox, logger.Object, notifier.Object, hybrid);
        return (conn, ctx, sut, notifier, logger);
    }

    private static void VerifyInformationLogContains(Mock<ILogger<DeliveryService>> logger, string substring, Times expectedInvocations)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v != null && v.ToString()!.Contains(substring, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            expectedInvocations);
    }

    private static void VerifyNoDeliveredWaitConfirm(Mock<IOrderNotificationPublisher> notifier) =>
        notifier.Verify(
            n => n.PublishDeliveryStatusChildAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                DeliveryState.DeliveredWaitConfirm,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never());

    private static void SeedCommonGraph(ApplicationDbContext ctx)
    {
        var now = DateTime.UtcNow;
        ctx.Roles.AddRange(
            new Role { RoleId = (int)RoleUser.DeliveryStaff, RoleName = "DeliveryStaff" },
            new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });

        ctx.Users.AddRange(
            new User
            {
                UserId = DeliveryAId,
                FullName = "Shipper A",
                Email = "a-cnf@ship.test",
                Phone = "1",
                PasswordHash = "x",
                RoleId = (int)RoleUser.DeliveryStaff,
                Status = UserState.Active,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                UserId = DeliveryBId,
                FullName = "Shipper B",
                Email = "b-cnf@ship.test",
                Phone = "2",
                PasswordHash = "x",
                RoleId = (int)RoleUser.DeliveryStaff,
                Status = UserState.Active,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                UserId = CustomerId,
                FullName = "Customer",
                Email = "c-cnf@test.local",
                Phone = "3",
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
            Name = "CP1",
            AddressLine = "Q1",
            Latitude = 10.77m,
            Longitude = 106.70m
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
            ContactPhone = "028",
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
            ExpiryDate = now.AddDays(5),
            ManufactureDate = now.AddDays(-1),
            Quantity = 50,
            OriginalUnitPrice = 1,
            SuggestedUnitPrice = 1,
            Weight = 1,
            Status = ProductState.Published,
            CreatedAt = now,
            UpdatedAt = now
        });

        ctx.SaveChanges();
    }

    private static DeliveryGroup CreateGroup(Guid gid, DeliveryGroupState status, Guid staffId)
    {
        var now = DateTime.UtcNow;
        return new DeliveryGroup
        {
            DeliveryGroupId = gid,
            GroupCode = "GRP-CNF-" + gid.ToString()[..4],
            SupermarketId = null,
            DeliveryStaffId = staffId,
            TimeSlotId = TimeSlotId,
            DeliveryType = DeliveryMethod.Delivery,
            DeliveryArea = "Q1",
            CenterLatitude = 10.77m,
            CenterLongitude = 106.70m,
            Status = status,
            TotalOrders = 1,
            DeliveryDate = now.Date,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Order CreateOrder(Guid orderId, string orderCode, OrderState status, Guid? deliveryGroupOnOrder)
    {
        var now = DateTime.UtcNow;
        return new Order
        {
            OrderId = orderId,
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
            Status = status,
            OrderDate = now,
            DeliveryGroupId = deliveryGroupOnOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static OrderItem CreateItem(Guid oid, Guid iid, Guid groupId, DeliveryState lineDelivery)
    {
        return new OrderItem
        {
            OrderItemId = iid,
            OrderId = oid,
            LotId = LotId,
            Quantity = 1,
            UnitPrice = 100000,
            TotalPrice = 100000,
            PackagingStatus = PackagingState.Completed,
            DeliveryStatus = lineDelivery,
            DeliveryGroupId = groupId
        };
    }

    private static ConfirmDeliveryRequestDto BuildRequest(
        string proofUrl,
        string verificationCode,
        Guid? deliveryGroupId = null,
        IReadOnlyList<Guid>? orderItemIds = null) =>
        new ConfirmDeliveryRequestDto
        {
            ProofImageUrl = proofUrl,
            VerificationCode = verificationCode,
            DeliveryGroupId = deliveryGroupId,
            OrderItemIds = orderItemIds
        };

    [Fact]
    public async Task TC01_AllEligibleInGroup_WithDeliveryGroupId_Success_LogAndPublish()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryAId));
            ctx.Orders.Add(CreateOrder(OrderId, "ORD-CN-01", OrderState.ReadyToShip, GroupId));
            ctx.OrderItems.AddRange(
                CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.InTransit),
                CreateItem(OrderId, OrderItemId2, GroupId, DeliveryState.InTransit));
            await ctx.SaveChangesAsync();

            await sut.ConfirmDeliveryAsync(
                OrderId,
                DeliveryAId,
                BuildRequest(ValidProofHttps, "ORD-CN-01", GroupId),
                CancellationToken.None);

            var items = await ctx.OrderItems.Where(i => i.OrderId == OrderId).OrderBy(i => i.OrderItemId).ToListAsync();
            Assert.All(items, i => Assert.Equal(DeliveryState.DeliveredWaitConfirm, i.DeliveryStatus));
            Assert.Equal(2, await ctx.Set<DeliveryLog>().CountAsync());

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    OrderId,
                    CustomerId,
                    "ORD-CN-01",
                    DeliveryState.DeliveredWaitConfirm,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            VerifyInformationLogContains(logger, "confirming delivery for order", Times.Once());
            VerifyInformationLogContains(logger, "delivered successfully", Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC02_Subset_OrderItemIds_DeliveryGroupIdOmitted_OnlyChosenLinesConfirmed()
    {
        var (conn, ctx, sut, notifier, _) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryAId));
            ctx.Orders.Add(CreateOrder(OrderId, "ORD-CN-02", OrderState.ReadyToShip, GroupId));
            ctx.OrderItems.AddRange(
                CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.InTransit),
                CreateItem(OrderId, OrderItemId2, GroupId, DeliveryState.InTransit));
            await ctx.SaveChangesAsync();

            await sut.ConfirmDeliveryAsync(
                OrderId,
                DeliveryAId,
                BuildRequest(ValidProofHttps, "ORD-CN-02", deliveryGroupId: null, orderItemIds: new[] { OrderItemId1 }),
                CancellationToken.None);

            var item1 = await ctx.OrderItems.AsNoTracking().FirstAsync(i => i.OrderItemId == OrderItemId1);
            var item2 = await ctx.OrderItems.AsNoTracking().FirstAsync(i => i.OrderItemId == OrderItemId2);
            Assert.Equal(DeliveryState.DeliveredWaitConfirm, item1.DeliveryStatus);
            Assert.Equal(DeliveryState.InTransit, item2.DeliveryStatus);
            Assert.Equal(1, await ctx.Set<DeliveryLog>().CountAsync());

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    DeliveryState.DeliveredWaitConfirm,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC03_ProofUrlTrimmedAsciiSpaces_AcceptsValidHttps()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryAId));
            ctx.Orders.Add(CreateOrder(OrderId, "ORD-CN-03", OrderState.ReadyToShip, GroupId));
            ctx.OrderItems.Add(CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.InTransit));
            await ctx.SaveChangesAsync();

            await sut.ConfirmDeliveryAsync(
                OrderId,
                DeliveryAId,
                BuildRequest("  https://cdn.example/path/proof.webp  ", "ORD-CN-03", GroupId),
                CancellationToken.None);

            Assert.Equal(DeliveryState.DeliveredWaitConfirm,
                await ctx.OrderItems.AsNoTracking()
                    .Where(i => i.OrderItemId == OrderItemId1)
                    .Select(i => i.DeliveryStatus)
                    .FirstAsync());

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    DeliveryState.DeliveredWaitConfirm,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());
            VerifyInformationLogContains(logger, "delivered successfully", Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC04_VerificationCode_CaseInsensitive_Matches_OrderCode()
    {
        var (conn, ctx, sut, notifier, _) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryAId));
            ctx.Orders.Add(CreateOrder(OrderId, "AbC-deF-009", OrderState.ReadyToShip, GroupId));
            ctx.OrderItems.Add(CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.InTransit));
            await ctx.SaveChangesAsync();

            await sut.ConfirmDeliveryAsync(
                OrderId,
                DeliveryAId,
                BuildRequest(ValidProofHttps, " abc-def-009 ", GroupId),
                CancellationToken.None);

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    DeliveryState.DeliveredWaitConfirm,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC05_OrderMissing_KeyNotFound_NoNotify()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            var ghost = Guid.Parse("aaaaaaaa-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                sut.ConfirmDeliveryAsync(ghost, DeliveryAId, BuildRequest(ValidProofHttps, "X"), CancellationToken.None));

            Assert.Contains("Không tìm thấy đơn hàng.", ex.Message, StringComparison.Ordinal);
            VerifyNoDeliveredWaitConfirm(notifier);
            VerifyInformationLogContains(logger, "confirming delivery for order", Times.Once());
            VerifyInformationLogContains(logger, "delivered successfully", Times.Never());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC06_StaffNotAssignedToOrderGroup_Unauthorized()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryBId));
            ctx.Orders.Add(CreateOrder(OrderId, "ORD-CN-06", OrderState.ReadyToShip, GroupId));
            ctx.OrderItems.Add(CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.InTransit));
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sut.ConfirmDeliveryAsync(
                    OrderId,
                    DeliveryAId,
                    BuildRequest(ValidProofHttps, "ORD-CN-06", GroupId),
                    CancellationToken.None));

            Assert.Contains("phân công", ex.Message, StringComparison.Ordinal);
            VerifyNoDeliveredWaitConfirm(notifier);
            VerifyInformationLogContains(logger, "delivered successfully", Times.Never());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC07_OrderWrongState_InvalidOperation_NotReadyToShip()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryAId));
            ctx.Orders.Add(CreateOrder(OrderId, "ORD-CN-07", OrderState.Paid, GroupId));
            ctx.OrderItems.Add(CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.InTransit));
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.ConfirmDeliveryAsync(
                    OrderId,
                    DeliveryAId,
                    BuildRequest(ValidProofHttps, "ORD-CN-07", GroupId),
                    CancellationToken.None));

            Assert.Contains("Đơn hàng phải ở trạng thái phù hợp", ex.Message, StringComparison.Ordinal);
            VerifyNoDeliveredWaitConfirm(notifier);
            VerifyInformationLogContains(logger, "delivered successfully", Times.Never());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC08_InvalidProofScheme_InvalidOperation()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryAId));
            ctx.Orders.Add(CreateOrder(OrderId, "ORD-CN-08", OrderState.ReadyToShip, GroupId));
            ctx.OrderItems.Add(CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.InTransit));
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.ConfirmDeliveryAsync(
                    OrderId,
                    DeliveryAId,
                    BuildRequest("ftp://files.example/evil.bin", "ORD-CN-08", GroupId),
                    CancellationToken.None));

            Assert.Contains("ProofImageUrl", ex.Message, StringComparison.Ordinal);
            VerifyNoDeliveredWaitConfirm(notifier);
            VerifyInformationLogContains(logger, "delivered successfully", Times.Never());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC09_ProofWhitespaceOnly_InvalidOperation_InvalidUrl()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryAId));
            ctx.Orders.Add(CreateOrder(OrderId, "ORD-CN-09", OrderState.ReadyToShip, GroupId));
            ctx.OrderItems.Add(CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.InTransit));
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.ConfirmDeliveryAsync(
                    OrderId,
                    DeliveryAId,
                    BuildRequest("   ", "ORD-CN-09", GroupId),
                    CancellationToken.None));

            Assert.Contains("ProofImageUrl", ex.Message, StringComparison.Ordinal);
            VerifyNoDeliveredWaitConfirm(notifier);
            VerifyInformationLogContains(logger, "delivered successfully", Times.Never());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC10_VerificationWhitespaceOnly_InvalidOperation()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryAId));
            ctx.Orders.Add(CreateOrder(OrderId, "ORD-CN-10", OrderState.ReadyToShip, GroupId));
            ctx.OrderItems.Add(CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.InTransit));
            await ctx.SaveChangesAsync();

            var dto = BuildRequest(ValidProofHttps, "   ", GroupId);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.ConfirmDeliveryAsync(OrderId, DeliveryAId, dto, CancellationToken.None));

            Assert.Contains("QR", ex.Message, StringComparison.Ordinal);
            VerifyNoDeliveredWaitConfirm(notifier);
            VerifyInformationLogContains(logger, "delivered successfully", Times.Never());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC11_VerificationMismatch_InvalidOperation_NoSideEffects()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryAId));
            ctx.Orders.Add(CreateOrder(OrderId, "ORD-CN-11", OrderState.ReadyToShip, GroupId));
            ctx.OrderItems.Add(CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.InTransit));
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.ConfirmDeliveryAsync(
                    OrderId,
                    DeliveryAId,
                    BuildRequest(ValidProofHttps, "WRONGQR", GroupId),
                    CancellationToken.None));

            Assert.Contains("khớp", ex.Message, StringComparison.Ordinal);

            VerifyNoDeliveredWaitConfirm(notifier);
            VerifyInformationLogContains(logger, "delivered successfully", Times.Never());

            Assert.Equal(
                DeliveryState.InTransit,
                await ctx.OrderItems.AsNoTracking()
                    .Where(i => i.OrderItemId == OrderItemId1)
                    .Select(i => i.DeliveryStatus)
                    .FirstAsync());
            Assert.Equal(0, await ctx.Set<DeliveryLog>().CountAsync());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC12_NoEligibleLines_AllAlreadyDeliveredWaitConfirm_InvalidOperation()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(GroupId, DeliveryGroupState.Assigned, DeliveryAId));
            ctx.Orders.Add(CreateOrder(OrderId, "ORD-CN-12", OrderState.DeliveredWaitConfirm, GroupId));
            ctx.OrderItems.Add(CreateItem(OrderId, OrderItemId1, GroupId, DeliveryState.DeliveredWaitConfirm));
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.ConfirmDeliveryAsync(
                    OrderId,
                    DeliveryAId,
                    BuildRequest(ValidProofHttps, "ORD-CN-12", GroupId),
                    CancellationToken.None));

            Assert.Contains("Không có dòng hàng nào hợp lệ", ex.Message, StringComparison.Ordinal);
            VerifyNoDeliveredWaitConfirm(notifier);
            VerifyInformationLogContains(logger, "delivered successfully", Times.Never());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }
}
