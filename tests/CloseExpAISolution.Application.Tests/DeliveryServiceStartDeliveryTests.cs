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
/// FN027 — Columns 01–10 of <c>.github/instructions/start-delivery-async-test-sheet.md</c>
/// (<see cref="DeliveryService.StartDeliveryAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~DeliveryServiceStartDeliveryTests"</c>
/// </summary>
public sealed class DeliveryServiceStartDeliveryTests
{
    private static readonly Guid TimeSlotId = Guid.Parse("cccc6001-0001-0001-0001-000000000001");
    private static readonly Guid CollectionId = Guid.Parse("dddd6001-0001-0001-0001-000000000001");
    private static readonly Guid VendorId = Guid.Parse("ffffffff-6000-0000-0000-000000000000");
    private static readonly Guid DeliveryAId = Guid.Parse("bbbbbbbb-6005-bbbb-bbbb-bbbbbbbbbbb5");
    private static readonly Guid DeliveryBId = Guid.Parse("bbbbbbbb-6006-bbbb-bbbb-bbbbbbbbbbb6");
    private static readonly Guid UnitId = Guid.Parse("aaaa6001-0001-0001-0001-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("ccca6001-0001-0001-0001-000000000001");
    private static readonly Guid SupermarketId = Guid.Parse("11111116-1111-1111-1111-111111111111");
    private static readonly Guid ProductId = Guid.Parse("bbbb6001-0001-0001-0001-000000000001");
    private static readonly Guid LotId = Guid.Parse("dddd6003-0003-0003-0003-000000000003");
    private static readonly Guid GroupId = Guid.Parse("eeee6001-6001-6001-6001-000000000001");
    private static readonly Guid OrderId = Guid.Parse("eeee6002-6002-6002-6002-000000000002");
    private static readonly Guid OrderItemId = Guid.Parse("eeee6003-6003-6003-6003-000000000003");

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

    private static void VerifyNoInTransitNotifications(Mock<IOrderNotificationPublisher> notifier)
    {
        notifier.Verify(
            n => n.PublishDeliveryStatusChildAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                DeliveryState.InTransit,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }

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
                Email = "a-st@ship.test",
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
                Email = "b-st@ship.test",
                Phone = "2",
                PasswordHash = "x",
                RoleId = (int)RoleUser.DeliveryStaff,
                Status = UserState.Active,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                UserId = VendorId,
                FullName = "Customer",
                Email = "c-st@test.local",
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

    private static void AddOrderWithItem(ApplicationDbContext ctx, Guid deliveryGroupId)
    {
        var now = DateTime.UtcNow;
        ctx.Orders.Add(new Order
        {
            OrderId = OrderId,
            OrderCode = "ORD-ST-01",
            UserId = VendorId,
            TimeSlotId = TimeSlotId,
            CollectionId = CollectionId,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = 100000,
            DiscountAmount = 0,
            FinalAmount = 100000,
            DeliveryFee = 0,
            SystemUsageFeeAmount = 0,
            Status = OrderState.ReadyToShip,
            OrderDate = now,
            DeliveryGroupId = deliveryGroupId,
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
            PackagingStatus = PackagingState.Completed,
            DeliveryStatus = DeliveryState.ReadyToShip,
            DeliveryGroupId = deliveryGroupId
        });

        ctx.SaveChanges();
    }

    private static DeliveryGroup CreateGroup(DeliveryGroupState status, Guid? assignedStaffId, string? notes = null)
    {
        var now = DateTime.UtcNow;
        return new DeliveryGroup
        {
            DeliveryGroupId = GroupId,
            GroupCode = "GRP-ST",
            SupermarketId = null,
            DeliveryStaffId = assignedStaffId,
            TimeSlotId = TimeSlotId,
            DeliveryType = DeliveryMethod.Delivery,
            DeliveryArea = "Q1",
            CenterLatitude = 10.77m,
            CenterLongitude = 106.70m,
            Status = status,
            TotalOrders = 1,
            Notes = notes,
            DeliveryDate = now.Date,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    [Fact]
    public async Task TC01_NullNotes_AssignedToInTransit_PublishesInTransit()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(DeliveryGroupState.Assigned, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            await sut.StartDeliveryAsync(
                GroupId,
                DeliveryAId,
                new StartDeliveryRequestDto { Notes = null },
                CancellationToken.None);

            var g = await ctx.DeliveryGroups.AsNoTracking().FirstAsync(x => x.DeliveryGroupId == GroupId);
            Assert.Equal(DeliveryGroupState.InTransit, g.Status);

            var oi = await ctx.OrderItems.AsNoTracking().FirstAsync(x => x.OrderItemId == OrderItemId);
            Assert.Equal(DeliveryState.InTransit, oi.DeliveryStatus);

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    OrderId,
                    VendorId,
                    "ORD-ST-01",
                    DeliveryState.InTransit,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            VerifyInformationLogContains(logger, "starting delivery for group", Times.Once());
            VerifyInformationLogContains(logger, "now InTransit", Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC02_NotesContent_WhenNotesEmpty_ReplacesStoredAsProvided()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(DeliveryGroupState.Assigned, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            await sut.StartDeliveryAsync(
                GroupId,
                DeliveryAId,
                new StartDeliveryRequestDto { Notes = "Xuất phát depot 1" },
                CancellationToken.None);

            var g = await ctx.DeliveryGroups.FirstAsync(x => x.DeliveryGroupId == GroupId);
            Assert.Equal("Xuất phát depot 1", g.Notes);

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    DeliveryState.InTransit,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            VerifyInformationLogContains(logger, "starting delivery for group", Times.Once());
            VerifyInformationLogContains(logger, "now InTransit", Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC03_WhitespaceOnlyNotes_StoredUntrimmed_ServiceDoesNotNormalize()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(DeliveryGroupState.Assigned, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            await sut.StartDeliveryAsync(
                GroupId,
                DeliveryAId,
                new StartDeliveryRequestDto { Notes = "    " },
                CancellationToken.None);

            var g = await ctx.DeliveryGroups.FirstAsync(x => x.DeliveryGroupId == GroupId);
            Assert.Equal("    ", g.Notes);
            Assert.Equal(DeliveryGroupState.InTransit, g.Status);

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    DeliveryState.InTransit,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            VerifyInformationLogContains(logger, "starting delivery for group", Times.Once());
            VerifyInformationLogContains(logger, "now InTransit", Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC04_AlreadyInTransit_Idempotent_NoPublish_SecondSuccessLogAbsent()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(DeliveryGroupState.InTransit, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            var itemBefore = await ctx.OrderItems.AsNoTracking().FirstAsync(x => x.OrderItemId == OrderItemId);
            notifier.Invocations.Clear();

            await sut.StartDeliveryAsync(
                GroupId,
                DeliveryAId,
                new StartDeliveryRequestDto { Notes = null },
                CancellationToken.None);

            VerifyNoInTransitNotifications(notifier);
            var itemAfter = await ctx.OrderItems.AsNoTracking().FirstAsync(x => x.OrderItemId == OrderItemId);
            Assert.Equal(itemBefore.DeliveryStatus, itemAfter.DeliveryStatus);

            VerifyInformationLogContains(logger, "starting delivery for group", Times.Once());
            VerifyInformationLogContains(logger, "now InTransit", Times.Never());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC05_GroupNotFound_KeyNotFound()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            var missing = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                sut.StartDeliveryAsync(missing, DeliveryAId, new StartDeliveryRequestDto(), CancellationToken.None));

            Assert.Equal("Không tìm thấy nhóm giao hàng.", ex.Message);

            VerifyInformationLogContains(logger, "starting delivery for group", Times.Once());
            VerifyInformationLogContains(logger, "now InTransit", Times.Never());
            VerifyNoInTransitNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC06_WrongShipper_Unauthorized_NoPublish()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(DeliveryGroupState.Assigned, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sut.StartDeliveryAsync(GroupId, DeliveryBId, new StartDeliveryRequestDto { Notes = null }, CancellationToken.None));

            Assert.Contains("phân công", ex.Message, StringComparison.Ordinal);

            VerifyInformationLogContains(logger, "starting delivery for group", Times.Once());
            VerifyInformationLogContains(logger, "now InTransit", Times.Never());
            VerifyNoInTransitNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC07_PendingGroup_InvalidOperation_CallersStaffMatches_GroupRow()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(DeliveryGroupState.Pending, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.StartDeliveryAsync(GroupId, DeliveryAId, new StartDeliveryRequestDto(), CancellationToken.None));

            Assert.Contains("Đã nhận", ex.Message, StringComparison.Ordinal);

            VerifyInformationLogContains(logger, "starting delivery for group", Times.Once());
            VerifyInformationLogContains(logger, "now InTransit", Times.Never());
            VerifyNoInTransitNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC08_CompletedGroup_InvalidOperation()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(DeliveryGroupState.Completed, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.StartDeliveryAsync(GroupId, DeliveryAId, new StartDeliveryRequestDto(), CancellationToken.None));

            VerifyInformationLogContains(logger, "starting delivery for group", Times.Once());
            VerifyInformationLogContains(logger, "now InTransit", Times.Never());
            VerifyNoInTransitNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC09_Notes_AppendWhenGroupAlreadyHasNotes()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(DeliveryGroupState.Assigned, DeliveryAId, notes: "Lộ trình A"));
            AddOrderWithItem(ctx, GroupId);

            await sut.StartDeliveryAsync(
                GroupId,
                DeliveryAId,
                new StartDeliveryRequestDto { Notes = "Xe tải 01" },
                CancellationToken.None);

            var g = await ctx.DeliveryGroups.FirstAsync(x => x.DeliveryGroupId == GroupId);
            Assert.Equal("Lộ trình A | Xe tải 01", g.Notes);
            Assert.Equal(DeliveryGroupState.InTransit, g.Status);

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    DeliveryState.InTransit,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            VerifyInformationLogContains(logger, "starting delivery for group", Times.Once());
            VerifyInformationLogContains(logger, "now InTransit", Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC10_DraftGroup_InvalidOperation_StaffMatchesRow()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroup(DeliveryGroupState.Draft, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.StartDeliveryAsync(GroupId, DeliveryAId, new StartDeliveryRequestDto(), CancellationToken.None));

            VerifyInformationLogContains(logger, "starting delivery for group", Times.Once());
            VerifyInformationLogContains(logger, "now InTransit", Times.Never());
            VerifyNoInTransitNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }
}
