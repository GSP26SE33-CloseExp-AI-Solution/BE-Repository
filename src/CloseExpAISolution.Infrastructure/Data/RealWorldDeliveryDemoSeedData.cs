using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Infrastructure.Data;

internal static class RealWorldDeliveryDemoSeedData
{
    internal static readonly Guid DeliveryDemoGroupId = Guid.Parse("dddd2001-0001-0001-0001-000000000001");

    private static readonly Guid VendorUserId1 = Guid.Parse("ffffffff-0000-0000-0000-000000000000");
    private static readonly Guid VendorUserId2 = Guid.Parse("11111111-1111-1111-0000-000000000001");
    private static readonly Guid DeliveryStaffUserId1 = Guid.Parse("99999999-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CustomerAddressVendor1Id = Guid.Parse("eeee0001-0001-0001-0001-000000000001");
    private static readonly Guid CustomerAddressVendor2Id = Guid.Parse("eeee0002-0002-0002-0002-000000000002");
    private static readonly Guid TimeSlotAfternoonId = Guid.Parse("cccc0002-0002-0002-0002-000000000002");

    private static readonly Guid OrderReadyId = Guid.Parse("eeee2001-0001-0001-0001-000000000001");
    private static readonly Guid OrderPickedUpId = Guid.Parse("eeee2002-0002-0002-0002-000000000002");
    private static readonly Guid OrderTransit1Id = Guid.Parse("eeee2003-0003-0003-0003-000000000003");
    private static readonly Guid OrderTransit2Id = Guid.Parse("eeee2004-0004-0004-0004-000000000004");
    private static readonly Guid OrderWaitConfirmId = Guid.Parse("eeee2005-0005-0005-0005-000000000005");
    private static readonly Guid OrderDone1Id = Guid.Parse("eeee2011-0011-0011-0011-000000000011");
    private static readonly Guid OrderDone2Id = Guid.Parse("eeee2012-0012-0012-0012-000000000012");
    private static readonly Guid OrderDone3Id = Guid.Parse("eeee2013-0013-0013-0013-000000000013");

    private static readonly Guid StaffUserId1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly Guid[] OrderItemIds =
    [
        Guid.Parse("ffff2001-0001-0001-0001-000000000001"),
        Guid.Parse("ffff2002-0002-0002-0002-000000000002"),
        Guid.Parse("ffff2003-0003-0003-0003-000000000003"),
        Guid.Parse("ffff2004-0004-0004-0004-000000000004"),
        Guid.Parse("ffff2005-0005-0005-0005-000000000005"),
        Guid.Parse("ffff2011-0011-0011-0011-000000000011"),
        Guid.Parse("ffff2012-0012-0012-0012-000000000012"),
        Guid.Parse("ffff2013-0013-0013-0013-000000000013"),
    ];

    private static readonly Guid[] PackagingIds =
    [
        Guid.Parse("aaaa2001-0001-0001-0001-000000000001"),
        Guid.Parse("aaaa2002-0002-0002-0002-000000000002"),
        Guid.Parse("aaaa2003-0003-0003-0003-000000000003"),
        Guid.Parse("aaaa2004-0004-0004-0004-000000000004"),
        Guid.Parse("aaaa2005-0005-0005-0005-000000000005"),
        Guid.Parse("aaaa2011-0011-0011-0011-000000000011"),
        Guid.Parse("aaaa2012-0012-0012-0012-000000000012"),
        Guid.Parse("aaaa2013-0013-0013-0013-000000000013"),
    ];

    private static readonly Guid[] DeliveryLogIds =
    [
        Guid.Parse("bbbb2002-0002-0002-0002-000000000002"),
        Guid.Parse("bbbb2003-0003-0003-0003-000000000003"),
        Guid.Parse("bbbb2004-0004-0004-0004-000000000004"),
        Guid.Parse("bbbb2005-0005-0005-0005-000000000005"),
        Guid.Parse("bbbb2011-0011-0011-0011-000000000011"),
        Guid.Parse("bbbb2012-0012-0012-0012-000000000012"),
        Guid.Parse("bbbb2013-0013-0013-0013-000000000013"),
    ];

    internal static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Orders.AnyAsync(o => o.OrderId == OrderReadyId))
            return;

        var catalogProductIds = RealWorldCatalogSeedData.Products.Select(p => p.ProductId).ToHashSet();
        var lots = await context.StockLots
            .Where(l => catalogProductIds.Contains(l.ProductId) && l.Status == ProductState.Published)
            .OrderBy(l => l.ProductId)
            .ThenBy(l => l.LotId)
            .Take(24)
            .ToListAsync();

        if (lots.Count < 8)
            return;

        var now = DateTime.UtcNow;
        var deliveryDate = now.Date;

        var group = new DeliveryGroup
        {
            DeliveryGroupId = DeliveryDemoGroupId,
            GroupCode = "DEL-DEMO-GROUP-01",
            DeliveryStaffId = DeliveryStaffUserId1,
            TimeSlotId = TimeSlotAfternoonId,
            DeliveryType = DeliveryMethod.Delivery,
            DeliveryArea = "DELIVERY",
            CenterLatitude = 10.7769m,
            CenterLongitude = 106.7009m,
            Status = DeliveryGroupState.InTransit,
            TotalOrders = 8,
            Notes = "Seed đơn giao hàng demo — nhiều trạng thái",
            DeliveryDate = deliveryDate,
            CreatedAt = now.AddHours(-4),
            UpdatedAt = now.AddMinutes(-15)
        };

        var scenarios = new DeliveryOrderScenario[]
        {
            new(
                OrderReadyId,
                "DEL-DEMO-READY-001",
                VendorUserId1,
                CustomerAddressVendor1Id,
                OrderState.ReadyToShip,
                DeliveryState.ReadyToShip,
                0,
                95_000m,
                now.AddHours(-3)),
            new(
                OrderPickedUpId,
                "DEL-DEMO-PICKUP-001",
                VendorUserId2,
                CustomerAddressVendor2Id,
                OrderState.ReadyToShip,
                DeliveryState.PickedUp,
                1,
                128_000m,
                now.AddHours(-2.5)),
            new(
                OrderTransit1Id,
                "DEL-DEMO-TRANSIT-001",
                VendorUserId1,
                CustomerAddressVendor2Id,
                OrderState.ReadyToShip,
                DeliveryState.InTransit,
                2,
                156_000m,
                now.AddHours(-2)),
            new(
                OrderTransit2Id,
                "DEL-DEMO-TRANSIT-002",
                VendorUserId2,
                CustomerAddressVendor1Id,
                OrderState.ReadyToShip,
                DeliveryState.InTransit,
                3,
                112_000m,
                now.AddHours(-1.8)),
            new(
                OrderWaitConfirmId,
                "DEL-DEMO-WAIT-001",
                VendorUserId1,
                CustomerAddressVendor1Id,
                OrderState.DeliveredWaitConfirm,
                DeliveryState.DeliveredWaitConfirm,
                4,
                189_000m,
                now.AddHours(-1.2)),
            new(
                OrderDone1Id,
                "DEL-DEMO-DONE-001",
                VendorUserId2,
                CustomerAddressVendor2Id,
                OrderState.Completed,
                DeliveryState.Completed,
                5,
                142_000m,
                now.AddHours(-5)),
            new(
                OrderDone2Id,
                "DEL-DEMO-DONE-002",
                VendorUserId1,
                CustomerAddressVendor1Id,
                OrderState.Completed,
                DeliveryState.Completed,
                6,
                98_000m,
                now.AddHours(-4.5)),
            new(
                OrderDone3Id,
                "DEL-DEMO-DONE-003",
                VendorUserId2,
                CustomerAddressVendor2Id,
                OrderState.Completed,
                DeliveryState.Completed,
                7,
                175_000m,
                now.AddHours(-4))
        };

        var orders = new List<Order>();
        var items = new List<OrderItem>();
        var packagingRecords = new List<OrderPackaging>();
        var deliveryLogs = new List<DeliveryLog>();

        for (var index = 0; index < scenarios.Length; index++)
        {
            var scenario = scenarios[index];
            var lot = lots[scenario.LotIndex % lots.Count];
            var unitPrice = lot.FinalUnitPrice is > 0 ? lot.FinalUnitPrice.Value : lot.SuggestedUnitPrice;
            if (unitPrice <= 0)
                unitPrice = 50_000m;

            var packagedAt = scenario.OrderDate.AddMinutes(45);
            var deliveredAt = scenario.DeliveryStatus is DeliveryState.Completed or DeliveryState.DeliveredWaitConfirm
                ? scenario.OrderDate.AddHours(1.5)
                : (DateTime?)null;

            orders.Add(new Order
            {
                OrderId = scenario.OrderId,
                OrderCode = scenario.OrderCode,
                UserId = scenario.UserId,
                TimeSlotId = TimeSlotAfternoonId,
                AddressId = scenario.AddressId,
                DeliveryType = DeliveryMethod.Delivery,
                TotalAmount = scenario.TotalAmount,
                DiscountAmount = 0,
                FinalAmount = scenario.TotalAmount + 10_000m,
                DeliveryFee = 10_000m,
                SystemUsageFeeAmount = 0,
                Status = scenario.OrderStatus,
                OrderDate = scenario.OrderDate,
                DeliveryGroupId = DeliveryDemoGroupId,
                DeliveryNote = $"Seed giao hàng — {scenario.DeliveryStatus}",
                CreatedAt = scenario.OrderDate,
                UpdatedAt = now.AddMinutes(-10)
            });

            var itemId = OrderItemIds[index];

            items.Add(new OrderItem
            {
                OrderItemId = itemId,
                OrderId = scenario.OrderId,
                LotId = lot.LotId,
                PurchaseUnitId = lot.UnitId,
                Quantity = 2,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * 2,
                PackagingStatus = PackagingState.Completed,
                DeliveryStatus = scenario.DeliveryStatus,
                DeliveryGroupId = DeliveryDemoGroupId,
                PackagedAt = packagedAt,
                DeliveredAt = deliveredAt
            });

            packagingRecords.Add(new OrderPackaging
            {
                PackagingId = PackagingIds[index],
                OrderId = scenario.OrderId,
                UserId = StaffUserId1,
                Status = PackagingState.Completed,
                PackagedAt = packagedAt
            });

            if (scenario.DeliveryStatus is DeliveryState.PickedUp or DeliveryState.InTransit
                or DeliveryState.DeliveredWaitConfirm or DeliveryState.Completed)
            {
                deliveryLogs.Add(new DeliveryLog
                {
                    DeliveryId = DeliveryLogIds[index - 1],
                    OrderId = scenario.OrderId,
                    OrderItemId = itemId,
                    UserId = DeliveryStaffUserId1,
                    Status = scenario.DeliveryStatus,
                    DeliveredAt = deliveredAt,
                    DeliveryLatitude = 10.7769m,
                    DeliveryLongitude = 106.7009m
                });
            }
        }

        await context.DeliveryGroups.AddAsync(group);
        await context.Orders.AddRangeAsync(orders);
        await context.OrderItems.AddRangeAsync(items);
        await context.PackagingRecords.AddRangeAsync(packagingRecords);
        await context.DeliveryLogs.AddRangeAsync(deliveryLogs);
        await context.SaveChangesAsync();
    }

    private sealed record DeliveryOrderScenario(
        Guid OrderId,
        string OrderCode,
        Guid UserId,
        Guid AddressId,
        OrderState OrderStatus,
        DeliveryState DeliveryStatus,
        int LotIndex,
        decimal TotalAmount,
        DateTime OrderDate);
}
