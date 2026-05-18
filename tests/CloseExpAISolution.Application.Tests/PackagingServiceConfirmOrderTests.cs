using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services;
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
/// Tự động hóa các biến thể UTCID01–UTCID06 (và quyền) cho <see cref="PackagingService.ConfirmOrderAsync"/>.
/// Chạy: dotnet test --filter "FullyQualifiedName~PackagingServiceConfirmOrderTests"
/// </summary>
public sealed class PackagingServiceConfirmOrderTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly PackagingService _sut;

    private static readonly Guid PackagingStaffId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid VendorUserId = Guid.Parse("ffffffff-0000-0000-0000-000000000000");
    private static readonly Guid TimeSlotId = Guid.Parse("cccc0001-0001-0001-0001-000000000001");
    private static readonly Guid UnitId = Guid.Parse("aaaa0001-0001-0001-0001-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("ccca0001-0001-0001-0001-000000000001");
    private static readonly Guid SupermarketId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductId = Guid.Parse("bbbb0001-0001-0001-0001-000000000001");
    private static readonly Guid LotId1 = Guid.Parse("dddd0001-0001-0001-0001-000000000001");
    private static readonly Guid LotId2 = Guid.Parse("dddd0002-0002-0002-0002-000000000002");
    private static readonly Guid OrderId = Guid.Parse("ffff0006-0006-0006-0006-000000000006");
    private static readonly Guid OrderItemId1 = Guid.Parse("ffff1001-0001-0001-0001-000000000001");
    private static readonly Guid OrderItemId2 = Guid.Parse("ffff1002-0002-0002-0002-000000000002");
    private static readonly Guid ForeignOrderItemId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public PackagingServiceConfirmOrderTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        SeedBaseline();

        var unitOfWork = new UnitOfWork(_context);
        var unitConversion = new UnitConversionRateService(unitOfWork);
        var stockQtyHelper = new OrderStockQuantityHelper(unitOfWork, unitConversion);
        var logger = new Mock<ILogger<PackagingService>>();

        var schedulerFactory = new Mock<ISchedulerFactory>();
        var scheduler = new Mock<IScheduler>();
        schedulerFactory
            .Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduler.Object);

        var refund = new Mock<IRefundService>();
        var notifications = new Mock<IOrderNotificationPublisher>();

        _sut = new PackagingService(unitOfWork, logger.Object, schedulerFactory.Object, refund.Object, notifications.Object, stockQtyHelper);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private void SeedBaseline()
    {
        var now = DateTime.UtcNow;

        _context.Roles.AddRange(
            new Role { RoleId = (int)RoleUser.PackagingStaff, RoleName = "PackagingStaff" },
            new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });

        _context.Users.AddRange(
            new User
            {
                UserId = PackagingStaffId,
                FullName = "NV đóng gói",
                Email = "pack@test.local",
                Phone = "0900000001",
                PasswordHash = "x",
                RoleId = (int)RoleUser.PackagingStaff,
                Status = UserState.Active,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                UserId = VendorUserId,
                FullName = "Vendor",
                Email = "vendor@test.local",
                Phone = "0900000002",
                PasswordHash = "x",
                RoleId = (int)RoleUser.Vendor,
                Status = UserState.Active,
                CreatedAt = now,
                UpdatedAt = now
            });

        _context.DeliveryTimeSlots.Add(new DeliveryTimeSlot
        {
            DeliveryTimeSlotId = TimeSlotId,
            StartTime = TimeSpan.FromHours(19),
            EndTime = TimeSpan.FromHours(20.5)
        });

        _context.UnitOfMeasures.Add(new UnitOfMeasure
        {
            UnitId = UnitId,
            Name = "Kg",
            Symbol = "kg",
            Type = "Weight",
            CreatedAt = now,
            UpdatedAt = now
        });

        _context.Categories.Add(new Category
        {
            CategoryId = CategoryId,
            Name = "Test",
            IsFreshFood = false,
            IsActive = true
        });

        _context.Supermarkets.Add(new Supermarket
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

        _context.Products.Add(new Product
        {
            ProductId = ProductId,
            CategoryId = CategoryId,
            SupermarketId = SupermarketId,
            UnitId = UnitId,
            Name = "Sản phẩm test",
            Barcode = "b1",
            Sku = "s1",
            Status = ProductState.Verified,
            CreatedBy = "test",
            CreatedAt = now,
            UpdatedAt = now,
            IsFeatured = false
        });

        _context.StockLots.AddRange(
            new StockLot
            {
                LotId = LotId1,
                ProductId = ProductId,
                UnitId = UnitId,
                ExpiryDate = now.AddDays(7),
                ManufactureDate = now.AddDays(-1),
                Quantity = 100,
                OriginalUnitPrice = 1000,
                SuggestedUnitPrice = 1000,
                Weight = 1,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now
            },
            new StockLot
            {
                LotId = LotId2,
                ProductId = ProductId,
                UnitId = UnitId,
                ExpiryDate = now.AddDays(7),
                ManufactureDate = now.AddDays(-1),
                Quantity = 100,
                OriginalUnitPrice = 1000,
                SuggestedUnitPrice = 1000,
                Weight = 1,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now
            });

        _context.Orders.Add(new Order
        {
            OrderId = OrderId,
            OrderCode = "PKG-CONFIRM-TEST",
            UserId = VendorUserId,
            TimeSlotId = TimeSlotId,
            CollectionId = null,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = 100000,
            DiscountAmount = 0,
            FinalAmount = 100000,
            DeliveryFee = 0,
            SystemUsageFeeAmount = 0,
            Status = OrderState.Paid,
            OrderDate = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        _context.OrderItems.AddRange(
            new OrderItem
            {
                OrderItemId = OrderItemId1,
                OrderId = OrderId,
                LotId = LotId1,
                Quantity = 1,
                UnitPrice = 50000,
                TotalPrice = 50000,
                PackagingStatus = PackagingState.Pending
            },
            new OrderItem
            {
                OrderItemId = OrderItemId2,
                OrderId = OrderId,
                LotId = LotId2,
                Quantity = 1,
                UnitPrice = 50000,
                TotalPrice = 50000,
                PackagingStatus = PackagingState.Pending
            });

        _context.SaveChanges();
    }

    [Fact]
    public async Task UTCID01_Confirm_AllLines_When_OrderItemIds_NullOrEmpty_ReturnsDetail()
    {
        var dtoEmpty = new ConfirmPackagingOrderRequestDto { OrderItemIds = Array.Empty<Guid>() };
        var r1 = await _sut.ConfirmOrderAsync(OrderId, PackagingStaffId, dtoEmpty, CancellationToken.None);
        Assert.Equal(OrderId, r1.OrderId);
        Assert.Equal(2, r1.Items.Count());

        var dtoNull = new ConfirmPackagingOrderRequestDto { OrderItemIds = null };
        var r2 = await _sut.ConfirmOrderAsync(OrderId, PackagingStaffId, dtoNull, CancellationToken.None);
        Assert.Equal(2, r2.Items.Count());
    }

    [Fact]
    public async Task UTCID02_OrderNotFound_Throws_KeyNotFoundException()
    {
        var missing = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.ConfirmOrderAsync(missing, PackagingStaffId, new ConfirmPackagingOrderRequestDto(), CancellationToken.None));

        Assert.Equal("Không tìm thấy đơn hàng.", ex.Message);
    }

    [Fact]
    public async Task UTCID03_ValidSubset_Confirms_Requested_Lines_Only()
    {
        var item2Row = await _context.OrderItems.FirstAsync(i => i.OrderItemId == OrderItemId2);
        item2Row.PackagingStatus = PackagingState.Packaging;
        await _context.SaveChangesAsync();

        await _sut.ConfirmOrderAsync(
            OrderId,
            PackagingStaffId,
            new ConfirmPackagingOrderRequestDto { OrderItemIds = new[] { OrderItemId1 } },
            CancellationToken.None);

        var item1 = await _context.OrderItems.FirstAsync(i => i.OrderItemId == OrderItemId1);
        var item2 = await _context.OrderItems.FirstAsync(i => i.OrderItemId == OrderItemId2);
        Assert.Equal(PackagingState.Pending, item1.PackagingStatus);
        Assert.Equal(PackagingState.Packaging, item2.PackagingStatus);
    }

    [Fact]
    public async Task UTCID04_DuplicateOrderItemIds_Throws_InvalidOperationException()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ConfirmOrderAsync(
                OrderId,
                PackagingStaffId,
                new ConfirmPackagingOrderRequestDto { OrderItemIds = new[] { OrderItemId1, OrderItemId1 } },
                CancellationToken.None));

        Assert.Equal("Có mã OrderItem bị trùng.", ex.Message);
    }

    [Fact]
    public async Task UTCID05_OrderItemNotInOrder_Throws_InvalidOperationException()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ConfirmOrderAsync(
                OrderId,
                PackagingStaffId,
                new ConfirmPackagingOrderRequestDto
                {
                    OrderItemIds = new[] { OrderItemId1, ForeignOrderItemId }
                },
                CancellationToken.None));

        Assert.Equal("Có mã OrderItem không thuộc đơn hàng.", ex.Message);
    }

    [Fact]
    public async Task UTCID06_TerminalLine_Throws_InvalidOperationException()
    {
        var terminalOrder = Guid.Parse("ffff0007-0007-0007-0007-000000000007");
        var terminalItem = Guid.Parse("ffff1003-0003-0003-0003-000000000003");
        var now = DateTime.UtcNow;

        _context.Orders.Add(new Order
        {
            OrderId = terminalOrder,
            OrderCode = "PKG-TERM",
            UserId = VendorUserId,
            TimeSlotId = TimeSlotId,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = 90000,
            DiscountAmount = 0,
            FinalAmount = 90000,
            DeliveryFee = 0,
            SystemUsageFeeAmount = 0,
            Status = OrderState.Paid,
            OrderDate = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        _context.OrderItems.Add(new OrderItem
        {
            OrderItemId = terminalItem,
            OrderId = terminalOrder,
            LotId = LotId1,
            Quantity = 1,
            UnitPrice = 90000,
            TotalPrice = 90000,
            PackagingStatus = PackagingState.Completed,
            PackagedAt = now
        });
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ConfirmOrderAsync(
                terminalOrder,
                PackagingStaffId,
                new ConfirmPackagingOrderRequestDto { OrderItemIds = new[] { terminalItem } },
                CancellationToken.None));

        Assert.Equal("Không thể xác nhận dòng hàng đã hoàn tất hoặc đã thất bại.", ex.Message);
    }

    [Fact]
    public async Task NonPackagingRole_Throws_UnauthorizedAccessException()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ConfirmOrderAsync(OrderId, VendorUserId, new ConfirmPackagingOrderRequestDto(), CancellationToken.None));

        Assert.Contains("quyền", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PackagingStaffUserMissing_Throws_KeyNotFoundException()
    {
        var ghost = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.ConfirmOrderAsync(OrderId, ghost, new ConfirmPackagingOrderRequestDto(), CancellationToken.None));

        Assert.Equal("Không tìm thấy nhân viên đóng gói.", ex.Message);
    }
}
