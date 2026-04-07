using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class OrderFulfillmentAggregatorTests
{
    [Fact]
    public void ApplyAggregatedOrderStatus_AllPackagingPending_KeepsPaid()
    {
        var order = new Order { Status = OrderState.Paid };
        var items = new[]
        {
            new OrderItem { PackagingStatus = PackagingState.Pending }
        };
        OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, items);
        Assert.Equal(OrderState.Paid, order.Status);
    }

    [Fact]
    public void ApplyAggregatedOrderStatus_AllPackaged_AllDeliveryPending_ShiftsToReadyToShip()
    {
        var order = new Order { Status = OrderState.Paid };
        var items = new[]
        {
            new OrderItem { PackagingStatus = PackagingState.Completed, DeliveryStatus = DeliveryState.ReadyToShip }
        };
        OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, items);
        Assert.Equal(OrderState.ReadyToShip, order.Status);
    }

    [Fact]
    public void ApplyAggregatedOrderStatus_AllPackaged_AllDeliveryTerminal_CompletesOrder()
    {
        var order = new Order { Status = OrderState.Paid };
        var items = new[]
        {
            new OrderItem { PackagingStatus = PackagingState.Completed, DeliveryStatus = DeliveryState.Completed }
        };
        OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, items);
        Assert.Equal(OrderState.Completed, order.Status);
    }

    [Fact]
    public void ApplyAggregatedOrderStatus_MixedTerminalAndInTransit_KeepsReadyToShip()
    {
        var order = new Order { Status = OrderState.ReadyToShip };
        var items = new[]
        {
            new OrderItem { PackagingStatus = PackagingState.Completed, DeliveryStatus = DeliveryState.Completed },
            new OrderItem { PackagingStatus = PackagingState.Completed, DeliveryStatus = DeliveryState.InTransit }
        };

        OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, items);

        Assert.Equal(OrderState.ReadyToShip, order.Status);
    }

    [Fact]
    public void ApplyAggregatedOrderStatus_HasDeliveredWaitConfirm_SetsDeliveredWaitConfirm()
    {
        var order = new Order { Status = OrderState.ReadyToShip };
        var items = new[]
        {
            new OrderItem { PackagingStatus = PackagingState.Completed, DeliveryStatus = DeliveryState.DeliveredWaitConfirm },
            new OrderItem { PackagingStatus = PackagingState.Completed, DeliveryStatus = DeliveryState.InTransit }
        };

        OrderFulfillmentAggregator.ApplyAggregatedOrderStatus(order, items);

        Assert.Equal(OrderState.DeliveredWaitConfirm, order.Status);
    }

    [Fact]
    public void SyncOrderDeliveryGroupPointer_SingleGroup_SetsOrderPointer()
    {
        var gid = Guid.NewGuid();
        var order = new Order();
        var items = new[]
        {
            new OrderItem { PackagingStatus = PackagingState.Completed, DeliveryStatus = DeliveryState.ReadyToShip, DeliveryGroupId = gid }
        };
        OrderFulfillmentAggregator.SyncOrderDeliveryGroupPointer(order, items);
        Assert.Equal(gid, order.DeliveryGroupId);
    }

    [Fact]
    public void SyncOrderDeliveryGroupPointer_MixedGroups_ClearsOrderPointer()
    {
        var order = new Order { DeliveryGroupId = Guid.NewGuid() };
        var items = new[]
        {
            new OrderItem { PackagingStatus = PackagingState.Completed, DeliveryStatus = DeliveryState.ReadyToShip, DeliveryGroupId = Guid.NewGuid() },
            new OrderItem { PackagingStatus = PackagingState.Completed, DeliveryStatus = DeliveryState.ReadyToShip, DeliveryGroupId = Guid.NewGuid() }
        };
        OrderFulfillmentAggregator.SyncOrderDeliveryGroupPointer(order, items);
        Assert.Null(order.DeliveryGroupId);
    }
}
