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
/// FN026 — Implements columns 01–10 of <c>.github/instructions/accept-delivery-group-async-test-sheet.md</c>
/// (<see cref="DeliveryService.AcceptDeliveryGroupAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~DeliveryServiceAcceptDeliveryGroupTests"</c>
/// </summary>
public sealed class DeliveryServiceAcceptDeliveryGroupTests
{
    private static readonly Guid TimeSlotId = Guid.Parse("cccc0001-0001-0001-0001-000000000001");
    private static readonly Guid CollectionId = Guid.Parse("dddd0001-0001-0001-0001-000000000001");
    private static readonly Guid VendorId = Guid.Parse("ffffffff-0000-0000-0000-000000000000");
    private static readonly Guid DeliveryAId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5");
    private static readonly Guid DeliveryBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6");
    private static readonly Guid PackagingUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
    private static readonly Guid UnitId = Guid.Parse("aaaa0001-0001-0001-0001-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("ccca0001-0001-0001-0001-000000000001");
    private static readonly Guid SupermarketId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductId = Guid.Parse("bbbb0001-0001-0001-0001-000000000001");
    private static readonly Guid LotId = Guid.Parse("dddd0003-0003-0003-0003-000000000003");
    private static readonly Guid GroupId = Guid.Parse("eeee5001-5001-5001-5001-000000000001");
    private static readonly Guid OrderId = Guid.Parse("eeee5002-5002-5002-5002-000000000002");
    private static readonly Guid OrderItemId = Guid.Parse("eeee5003-5003-5003-5003-000000000003");

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

    /// <summary>Test sheet matrix — Log (<see cref="ILogger{T}"/>): informational line contains substring.</summary>
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

    private static void VerifyNoPickedUpNotifications(Mock<IOrderNotificationPublisher> notifier)
    {
        notifier.Verify(
            n => n.PublishDeliveryStatusChildAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                DeliveryState.PickedUp,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }

    /// <summary>Nền: Role, user (shipper A/B, vendor, packaging), slot, điểm lấy, unit/cat/SM/product/lot.</summary>
    private static void SeedCommonGraph(ApplicationDbContext ctx)
    {
        var now = DateTime.UtcNow;
        ctx.Roles.AddRange(
            new Role { RoleId = (int)RoleUser.PackagingStaff, RoleName = "PackagingStaff" },
            new Role { RoleId = (int)RoleUser.DeliveryStaff, RoleName = "DeliveryStaff" },
            new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });

        ctx.Users.AddRange(
            new User
            {
                UserId = DeliveryAId,
                FullName = "Shipper A",
                Email = "a@ship.test",
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
                Email = "b@ship.test",
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
                FullName = "Vendor",
                Email = "v@test.local",
                Phone = "3",
                PasswordHash = "x",
                RoleId = (int)RoleUser.Vendor,
                Status = UserState.Active,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                UserId = PackagingUserId,
                FullName = "Packaging",
                Email = "p@test.local",
                Phone = "4",
                PasswordHash = "x",
                RoleId = (int)RoleUser.PackagingStaff,
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
            OrderCode = "ORD-ACC-01",
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

    private static DeliveryGroup CreateGroupTemplate(DeliveryGroupState status, Guid? assignedStaffId, string? notes = null)
    {
        var now = DateTime.UtcNow;
        return new DeliveryGroup
        {
            DeliveryGroupId = GroupId,
            GroupCode = "GRP-ACC",
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
    public async Task TC01_NullNotes_Success_Assigned_And_PublishPickedUp()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroupTemplate(DeliveryGroupState.Pending, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);
            await sut.AcceptDeliveryGroupAsync(GroupId, DeliveryAId, new AcceptDeliveryGroupRequestDto { Notes = null }, CancellationToken.None);

            var g = await ctx.DeliveryGroups.FirstAsync(x => x.DeliveryGroupId == GroupId);
            Assert.Equal(DeliveryGroupState.Assigned, g.Status);

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    OrderId,
                    VendorId,
                    "ORD-ACC-01",
                    DeliveryState.PickedUp,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            VerifyInformationLogContains(logger, "accepting delivery group", Times.Once());
            VerifyInformationLogContains(logger, "accepted by staff", Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC02_NotesWithContent_AppendedToEmptyGroupNotes()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroupTemplate(DeliveryGroupState.Pending, DeliveryAId, notes: null));
            AddOrderWithItem(ctx, GroupId);

            await sut.AcceptDeliveryGroupAsync(
                GroupId,
                DeliveryAId,
                new AcceptDeliveryGroupRequestDto { Notes = "  Đã xác nhận nhận  " },
                CancellationToken.None);

            var g = await ctx.DeliveryGroups.FirstAsync(x => x.DeliveryGroupId == GroupId);
            Assert.Equal("Đã xác nhận nhận", g.Notes);
            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    DeliveryState.PickedUp,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            VerifyInformationLogContains(logger, "accepting delivery group", Times.Once());
            VerifyInformationLogContains(logger, "accepted by staff", Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC03_WhitespaceOnlyNotes_DoesNotAlterGroupNotes()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroupTemplate(DeliveryGroupState.Pending, DeliveryAId, notes: null));
            AddOrderWithItem(ctx, GroupId);

            await sut.AcceptDeliveryGroupAsync(
                GroupId,
                DeliveryAId,
                new AcceptDeliveryGroupRequestDto { Notes = "    " },
                CancellationToken.None);

            var g = await ctx.DeliveryGroups.FirstAsync(x => x.DeliveryGroupId == GroupId);
            Assert.Null(g.Notes);

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    DeliveryState.PickedUp,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            VerifyInformationLogContains(logger, "accepting delivery group", Times.Once());
            VerifyInformationLogContains(logger, "accepted by staff", Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC04_GroupNotFound_Throws_KeyNotFound()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            var missing = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                sut.AcceptDeliveryGroupAsync(missing, DeliveryAId, new AcceptDeliveryGroupRequestDto(), CancellationToken.None));

            Assert.Equal("Không tìm thấy nhóm giao hàng.", ex.Message);

            VerifyInformationLogContains(logger, "accepting delivery group", Times.Once());
            VerifyInformationLogContains(logger, "accepted by staff", Times.Never());
            VerifyNoPickedUpNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC05_DeliveryUserNotFound_Throws_KeyNotFound()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroupTemplate(DeliveryGroupState.Pending, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            var ghost = Guid.Parse("77777777-7777-7777-7777-777777777777");
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                sut.AcceptDeliveryGroupAsync(GroupId, ghost, new AcceptDeliveryGroupRequestDto(), CancellationToken.None));

            Assert.Equal("Không tìm thấy nhân viên giao hàng.", ex.Message);

            VerifyInformationLogContains(logger, "accepting delivery group", Times.Once());
            VerifyInformationLogContains(logger, "accepted by staff", Times.Never());
            VerifyNoPickedUpNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC06_WrongRole_Throws_Unauthorized()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroupTemplate(DeliveryGroupState.Pending, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sut.AcceptDeliveryGroupAsync(GroupId, PackagingUserId, new AcceptDeliveryGroupRequestDto(), CancellationToken.None));

            Assert.Contains("quyền", ex.Message, StringComparison.Ordinal);

            VerifyInformationLogContains(logger, "accepting delivery group", Times.Once());
            VerifyInformationLogContains(logger, "accepted by staff", Times.Never());
            VerifyNoPickedUpNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC07_GroupNotPending_Throws_InvalidOperation()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroupTemplate(DeliveryGroupState.Assigned, DeliveryAId));
            AddOrderWithItem(ctx, GroupId);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.AcceptDeliveryGroupAsync(GroupId, DeliveryAId, new AcceptDeliveryGroupRequestDto(), CancellationToken.None));

            Assert.Contains("không ở trạng thái chờ shipper xác nhận nhận", ex.Message, StringComparison.Ordinal);

            VerifyInformationLogContains(logger, "accepting delivery group", Times.Once());
            VerifyInformationLogContains(logger, "accepted by staff", Times.Never());
            VerifyNoPickedUpNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC08_NoAssignedStaffOnGroup_Throws_InvalidOperation()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroupTemplate(DeliveryGroupState.Pending, assignedStaffId: null));
            AddOrderWithItem(ctx, GroupId);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.AcceptDeliveryGroupAsync(GroupId, DeliveryAId, new AcceptDeliveryGroupRequestDto(), CancellationToken.None));

            Assert.Contains("chưa được admin gán shipper", ex.Message, StringComparison.Ordinal);

            VerifyInformationLogContains(logger, "accepting delivery group", Times.Once());
            VerifyInformationLogContains(logger, "accepted by staff", Times.Never());
            VerifyNoPickedUpNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC09_WrongAssignedShipper_Throws_Unauthorized()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroupTemplate(DeliveryGroupState.Pending, DeliveryBId));
            AddOrderWithItem(ctx, GroupId);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sut.AcceptDeliveryGroupAsync(GroupId, DeliveryAId, new AcceptDeliveryGroupRequestDto(), CancellationToken.None));

            VerifyInformationLogContains(logger, "accepting delivery group", Times.Once());
            VerifyInformationLogContains(logger, "accepted by staff", Times.Never());
            VerifyNoPickedUpNotifications(notifier);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task TC10_NotesAppendWhenGroupAlreadyHasNotes()
    {
        var (conn, ctx, sut, notifier, logger) = CreateSut();
        try
        {
            SeedCommonGraph(ctx);
            ctx.DeliveryGroups.Add(CreateGroupTemplate(DeliveryGroupState.Pending, DeliveryAId, notes: "Ghi cũ"));
            AddOrderWithItem(ctx, GroupId);

            await sut.AcceptDeliveryGroupAsync(
                GroupId,
                DeliveryAId,
                new AcceptDeliveryGroupRequestDto { Notes = "Ghi mới" },
                CancellationToken.None);

            var g = await ctx.DeliveryGroups.FirstAsync(x => x.DeliveryGroupId == GroupId);
            Assert.Equal("Ghi cũ | Ghi mới", g.Notes);

            notifier.Verify(
                n => n.PublishDeliveryStatusChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    DeliveryState.PickedUp,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            VerifyInformationLogContains(logger, "accepting delivery group", Times.Once());
            VerifyInformationLogContains(logger, "accepted by staff", Times.Once());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }
}
