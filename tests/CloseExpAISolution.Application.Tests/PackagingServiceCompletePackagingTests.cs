using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// Ma trận TC01–TC11 cho <see cref="PackagingService.CompletePackagingAsync"/> (theo guideline-test / bảng testcase).
/// Chạy: dotnet test --filter "FullyQualifiedName~PackagingServiceCompletePackagingTests"
/// </summary>
public sealed class PackagingServiceCompletePackagingTests
{
    private static readonly Guid Staff1Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Staff2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbc");
    private static readonly Guid VendorId = Guid.Parse("ffffffff-0000-0000-0000-000000000000");
    private static readonly Guid TimeSlotId = Guid.Parse("cccc0001-0001-0001-0001-000000000001");
    private static readonly Guid UnitId = Guid.Parse("aaaa0001-0001-0001-0001-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("ccca0001-0001-0001-0001-000000000001");
    private static readonly Guid SupermarketId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductId = Guid.Parse("bbbb0001-0001-0001-0001-000000000001");
    private static readonly Guid Lot1 = Guid.Parse("dddd0001-0001-0001-0001-000000000001");
    private static readonly Guid Lot2 = Guid.Parse("dddd0002-0002-0002-0002-000000000002");
    private static readonly Guid Item1 = Guid.Parse("ffff2001-2001-2001-2001-000000000001");
    private static readonly Guid Item2 = Guid.Parse("ffff2002-2002-2002-2002-000000000002");
    private static readonly Guid ForeignItem = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid OrderBase = Guid.Parse("eeee0001-0001-0001-0001-000000000001");

    private static (SqliteConnection conn, ApplicationDbContext ctx, PackagingService sut, Mock<IOrderNotificationPublisher> notifications, Mock<ILogger<PackagingService>> logger) CreateSut()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new ApplicationDbContext(options);
        ctx.Database.EnsureCreated();

        var uow = new UnitOfWork(ctx);
        var logger = new Mock<ILogger<PackagingService>>();
        var schedulerFactory = new Mock<ISchedulerFactory>();
        var scheduler = new Mock<IScheduler>();
        schedulerFactory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(scheduler.Object);
        var refund = new Mock<IRefundService>();
        var notifications = new Mock<IOrderNotificationPublisher>();
        notifications
            .Setup(n => n.PublishOrderThreadChildAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new PackagingService(uow, logger.Object, schedulerFactory.Object, refund.Object, notifications.Object);
        return (conn, ctx, sut, notifications, logger);
    }

    private static void SeedGraph(ApplicationDbContext ctx)
    {
        var now = DateTime.UtcNow;
        ctx.Roles.AddRange(
            new Role { RoleId = (int)RoleUser.PackagingStaff, RoleName = "PackagingStaff" },
            new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });

        ctx.Users.AddRange(
            new User
            {
                UserId = Staff1Id,
                FullName = "Pack 1",
                Email = "p1@test.local",
                Phone = "1",
                PasswordHash = "x",
                RoleId = (int)RoleUser.PackagingStaff,
                Status = UserState.Active,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                UserId = Staff2Id,
                FullName = "Pack 2",
                Email = "p2@test.local",
                Phone = "2",
                PasswordHash = "x",
                RoleId = (int)RoleUser.PackagingStaff,
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
            });

        ctx.DeliveryTimeSlots.Add(new DeliveryTimeSlot
        {
            DeliveryTimeSlotId = TimeSlotId,
            StartTime = TimeSpan.FromHours(19),
            EndTime = TimeSpan.FromHours(20.5)
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
            Name = "Cat",
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

        ctx.StockLots.AddRange(
            new StockLot
            {
                LotId = Lot1,
                ProductId = ProductId,
                UnitId = UnitId,
                ExpiryDate = now.AddDays(7),
                ManufactureDate = now.AddDays(-1),
                Quantity = 100,
                OriginalUnitPrice = 1,
                SuggestedUnitPrice = 1,
                Weight = 1,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now
            },
            new StockLot
            {
                LotId = Lot2,
                ProductId = ProductId,
                UnitId = UnitId,
                ExpiryDate = now.AddDays(7),
                ManufactureDate = now.AddDays(-1),
                Quantity = 100,
                OriginalUnitPrice = 1,
                SuggestedUnitPrice = 1,
                Weight = 1,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now
            });

        ctx.SaveChanges();
    }

    private static Order AddOrder(
        ApplicationDbContext ctx,
        Guid orderId,
        OrderState status,
        bool withItems)
    {
        var now = DateTime.UtcNow;
        var order = new Order
        {
            OrderId = orderId,
            OrderCode = "ORD-TC",
            UserId = VendorId,
            TimeSlotId = TimeSlotId,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = 100000,
            DiscountAmount = 0,
            FinalAmount = 100000,
            DeliveryFee = 0,
            SystemUsageFeeAmount = 0,
            Status = status,
            OrderDate = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        ctx.Orders.Add(order);

        if (withItems)
        {
            ctx.OrderItems.AddRange(
                new OrderItem
                {
                    OrderItemId = Item1,
                    OrderId = orderId,
                    LotId = Lot1,
                    Quantity = 1,
                    UnitPrice = 50000,
                    TotalPrice = 50000,
                    PackagingStatus = PackagingState.Pending
                },
                new OrderItem
                {
                    OrderItemId = Item2,
                    OrderId = orderId,
                    LotId = Lot2,
                    Quantity = 1,
                    UnitPrice = 50000,
                    TotalPrice = 50000,
                    PackagingStatus = PackagingState.Pending
                });
        }

        ctx.SaveChanges();
        return order;
    }

    private static void AddPackagingRecord(
        ApplicationDbContext ctx,
        Guid orderId,
        Guid orderItemId,
        Guid ownerUserId,
        PackagingState status)
    {
        ctx.PackagingRecords.Add(new OrderPackaging
        {
            PackagingId = Guid.NewGuid(),
            OrderId = orderId,
            OrderItemId = orderItemId,
            UserId = ownerUserId,
            Status = status,
            PackagedAt = null
        });
        ctx.SaveChanges();
    }

    /// <summary>
    /// TC01: Paid, OrderItemIds null, 2 dòng Pending, NV hiện tại — hoàn tất tất cả, đơn ReadyToShip.
    /// </summary>
    [Fact]
    public async Task TC01_NullOrderItemIds_AllPending_Completes_And_Sets_ReadyToShip()
    {
        var (conn, ctx, sut, notif, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.Paid, withItems: true);
            AddPackagingRecord(ctx, OrderBase, Item1, Staff1Id, PackagingState.Pending);
            AddPackagingRecord(ctx, OrderBase, Item2, Staff1Id, PackagingState.Pending);

            await sut.CompletePackagingAsync(
                OrderBase,
                Staff1Id,
                new CompletePackagingOrderRequestDto { OrderItemIds = null, Notes = "n" },
                CancellationToken.None);

            var order = await ctx.Orders.FirstAsync(o => o.OrderId == OrderBase);
            Assert.Equal(OrderState.ReadyToShip, order.Status);
            var items = await ctx.OrderItems.Where(i => i.OrderId == OrderBase).ToListAsync();
            Assert.All(items, i => Assert.Equal(PackagingState.Completed, i.PackagingStatus));

            notif.Verify(
                n => n.PublishOrderThreadChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    "Đơn hàng sẵn sàng giao",
                    It.IsAny<string>(),
                    NotificationType.OrderUpdate,
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// TC02: Paid, subset 1 dòng (Packaging), dòng còn Pending — có thông báo cập nhật đóng gói một phần.
    /// </summary>
    [Fact]
    public async Task TC02_ValidSubset_PartialLines_Notifies_PartialPackaging()
    {
        var (conn, ctx, sut, notif, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.Paid, withItems: true);
            var row1 = await ctx.OrderItems.FirstAsync(i => i.OrderItemId == Item1);
            row1.PackagingStatus = PackagingState.Packaging;
            await ctx.SaveChangesAsync();

            AddPackagingRecord(ctx, OrderBase, Item1, Staff1Id, PackagingState.Packaging);
            AddPackagingRecord(ctx, OrderBase, Item2, Staff1Id, PackagingState.Pending);

            await sut.CompletePackagingAsync(
                OrderBase,
                Staff1Id,
                new CompletePackagingOrderRequestDto { OrderItemIds = new[] { Item1 } },
                CancellationToken.None);

            notif.Verify(
                n => n.PublishOrderThreadChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    "Cập nhật đóng gói",
                    It.IsAny<string>(),
                    NotificationType.OrderUpdate,
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// TC03: ReadyToShip, mọi dòng target đã Completed — idempotent, không gọi publish trong luồng hoàn tất.
    /// </summary>
    [Fact]
    public async Task TC03_ReadyToShip_AllTargetsCompleted_Idempotent_NoPublish()
    {
        var (conn, ctx, sut, notif, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.ReadyToShip, withItems: true);
            foreach (var oi in ctx.OrderItems.Where(i => i.OrderId == OrderBase).ToList())
            {
                oi.PackagingStatus = PackagingState.Completed;
                oi.PackagedAt = DateTime.UtcNow;
            }

            await ctx.SaveChangesAsync();

            AddPackagingRecord(ctx, OrderBase, Item1, Staff1Id, PackagingState.Completed);
            AddPackagingRecord(ctx, OrderBase, Item2, Staff1Id, PackagingState.Completed);

            notif.Invocations.Clear();

            await sut.CompletePackagingAsync(
                OrderBase,
                Staff1Id,
                new CompletePackagingOrderRequestDto { OrderItemIds = new[] { Item1, Item2 } },
                CancellationToken.None);

            notif.Verify(
                n => n.PublishOrderThreadChildAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<CancellationToken>()),
                Times.Never());
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// TC04: Trạng thái đơn không phải Paid — "Chỉ đóng gói khi đơn ở Paid".
    /// </summary>
    [Fact]
    public async Task TC04_OrderNotPaid_Throws_Chỉ_đóng_gói_khi_đơn_ở_Paid()
    {
        var (conn, ctx, sut, _, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.Pending, withItems: true);
            AddPackagingRecord(ctx, OrderBase, Item1, Staff1Id, PackagingState.Pending);
            AddPackagingRecord(ctx, OrderBase, Item2, Staff1Id, PackagingState.Pending);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CompletePackagingAsync(
                    OrderBase,
                    Staff1Id,
                    new CompletePackagingOrderRequestDto(),
                    CancellationToken.None));

            Assert.Contains("Chỉ đóng gói khi đơn ở Paid", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// TC05: Dòng target Failed — "phải ở trạng thái đã xác nhận hoặc đang thu gom".
    /// </summary>
    [Fact]
    public async Task TC05_TargetFailed_Throws_StateMessage()
    {
        var (conn, ctx, sut, _, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.Paid, withItems: true);
            var failed = await ctx.OrderItems.FirstAsync(i => i.OrderItemId == Item1);
            failed.PackagingStatus = PackagingState.Failed;
            await ctx.SaveChangesAsync();
            AddPackagingRecord(ctx, OrderBase, Item1, Staff1Id, PackagingState.Failed);
            AddPackagingRecord(ctx, OrderBase, Item2, Staff1Id, PackagingState.Pending);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CompletePackagingAsync(OrderBase, Staff1Id, new CompletePackagingOrderRequestDto { OrderItemIds = new[] { Item1 } }, CancellationToken.None));

            Assert.Contains("phải ở trạng thái đã xác nhận hoặc đang thu gom", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// TC06: OrderPackaging do NV khác sở hữu — UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task TC06_WrongRecordOwner_Throws_Unauthorized()
    {
        var (conn, ctx, sut, _, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.Paid, withItems: true);
            AddPackagingRecord(ctx, OrderBase, Item1, Staff2Id, PackagingState.Pending);
            AddPackagingRecord(ctx, OrderBase, Item2, Staff1Id, PackagingState.Pending);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sut.CompletePackagingAsync(OrderBase, Staff1Id, new CompletePackagingOrderRequestDto { OrderItemIds = new[] { Item1 } }, CancellationToken.None));
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// TC07: Trùng OrderItemId trong request.
    /// </summary>
    [Fact]
    public async Task TC07_DuplicateOrderItemIds_Throws()
    {
        var (conn, ctx, sut, _, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.Paid, withItems: true);
            AddPackagingRecord(ctx, OrderBase, Item1, Staff1Id, PackagingState.Pending);
            AddPackagingRecord(ctx, OrderBase, Item2, Staff1Id, PackagingState.Pending);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CompletePackagingAsync(
                    OrderBase,
                    Staff1Id,
                    new CompletePackagingOrderRequestDto { OrderItemIds = new[] { Item1, Item1 } },
                    CancellationToken.None));

            Assert.Equal("Có mã OrderItem bị trùng.", ex.Message);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// TC08: OrderItem không thuộc đơn.
    /// </summary>
    [Fact]
    public async Task TC08_OrderItemNotInOrder_Throws()
    {
        var (conn, ctx, sut, _, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.Paid, withItems: true);
            AddPackagingRecord(ctx, OrderBase, Item1, Staff1Id, PackagingState.Pending);
            AddPackagingRecord(ctx, OrderBase, Item2, Staff1Id, PackagingState.Pending);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CompletePackagingAsync(
                    OrderBase,
                    Staff1Id,
                    new CompletePackagingOrderRequestDto { OrderItemIds = new[] { Item1, ForeignItem } },
                    CancellationToken.None));

            Assert.Equal("Có mã OrderItem không thuộc đơn hàng.", ex.Message);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// TC09: Đơn không có dòng hàng.
    /// </summary>
    [Fact]
    public async Task TC09_OrderHasNoItems_Throws()
    {
        var (conn, ctx, sut, _, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.Paid, withItems: false);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CompletePackagingAsync(OrderBase, Staff1Id, new CompletePackagingOrderRequestDto(), CancellationToken.None));

            Assert.Equal("Đơn hàng không có dòng hàng.", ex.Message);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// TC10: OrderItemIds = mảng rỗng — cùng hành vi null (toàn bộ dòng).
    /// </summary>
    [Fact]
    public async Task TC10_EmptyOrderItemIds_Completes_All_Lines_Like_Null()
    {
        var (conn, ctx, sut, _, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.Paid, withItems: true);
            AddPackagingRecord(ctx, OrderBase, Item1, Staff1Id, PackagingState.Pending);
            AddPackagingRecord(ctx, OrderBase, Item2, Staff1Id, PackagingState.Pending);

            await sut.CompletePackagingAsync(
                OrderBase,
                Staff1Id,
                new CompletePackagingOrderRequestDto { OrderItemIds = Array.Empty<Guid>() },
                CancellationToken.None);

            var order = await ctx.Orders.FirstAsync(o => o.OrderId == OrderBase);
            Assert.Equal(OrderState.ReadyToShip, order.Status);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// TC11: ReadyToShip nhưng target chưa tất cả Completed (một dòng vẫn Packaging) — không idempotent, chặn vì không Paid.
    /// </summary>
    [Fact]
    public async Task TC11_ReadyToShip_IncompleteTargets_Throws_PaidGuard()
    {
        var (conn, ctx, sut, _, _) = CreateSut();
        try
        {
            SeedGraph(ctx);
            AddOrder(ctx, OrderBase, OrderState.ReadyToShip, withItems: true);
            var i1 = await ctx.OrderItems.FirstAsync(i => i.OrderItemId == Item1);
            var i2 = await ctx.OrderItems.FirstAsync(i => i.OrderItemId == Item2);
            i1.PackagingStatus = PackagingState.Completed;
            i1.PackagedAt = DateTime.UtcNow;
            i2.PackagingStatus = PackagingState.Packaging;
            await ctx.SaveChangesAsync();

            AddPackagingRecord(ctx, OrderBase, Item1, Staff1Id, PackagingState.Completed);
            AddPackagingRecord(ctx, OrderBase, Item2, Staff1Id, PackagingState.Packaging);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CompletePackagingAsync(OrderBase, Staff1Id, new CompletePackagingOrderRequestDto(), CancellationToken.None));

            Assert.Contains("Chỉ đóng gói khi đơn ở Paid", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            ctx.Dispose();
            conn.Dispose();
        }
    }
}
