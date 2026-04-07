using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Services.Fulfillment;

/// <summary>
/// Derives <see cref="Order.Status"/> from the set of <see cref="OrderItem"/> packaging/delivery states (item-level fulfillment).
/// </summary>
public static class OrderFulfillmentAggregator
{
    /// <summary>
    /// Recomputes and assigns <paramref name="order"/>.Status from <paramref name="items"/>.
    /// Does not persist; caller updates the entity and SaveChanges.
    /// </summary>
    public static void ApplyAggregatedOrderStatus(Order order, IReadOnlyList<OrderItem> items)
    {
        if (items.Count == 0)
            return;

        if (order.Status is OrderState.Canceled or OrderState.Refunded)
            return;

        var packagingOpen = items.Any(i =>
            i.PackagingStatus is not PackagingState.Completed and not PackagingState.Failed);

        if (packagingOpen)
        {
            if (order.Status is OrderState.Paid or OrderState.ReadyToShip)
                order.Status = OrderState.Paid;
            return;
        }

        if (items.All(i => i.PackagingStatus == PackagingState.Failed))
        {
            order.Status = OrderState.Failed;
            return;
        }

        var shipped = items.Where(i => i.PackagingStatus == PackagingState.Completed).ToList();
        if (shipped.Count == 0)
        {
            order.Status = OrderState.Failed;
            return;
        }

        // Delivery terminal: Completed or Failed (delivery) for every shipped line
        static bool IsDeliveryDone(OrderItem i) =>
            i.DeliveryStatus is DeliveryState.Completed or DeliveryState.Failed;

        if (shipped.All(IsDeliveryDone))
        {
            order.Status = OrderState.Completed;
            return;
        }

        var anyWaitConfirm = shipped.Any(i => i.DeliveryStatus == DeliveryState.DeliveredWaitConfirm);
        if (anyWaitConfirm)
        {
            order.Status = OrderState.DeliveredWaitConfirm;
            return;
        }

        // Still in shipper pipeline (ReadyToShip / PickedUp / InTransit) or not yet assigned
        order.Status = OrderState.ReadyToShip;
    }

    /// <summary>
    /// After packaging completes for an item, sets <see cref="OrderItem.DeliveryStatus"/> to ReadyToShip when appropriate.
    /// </summary>
    public static void MarkItemReadyToShip(OrderItem item)
    {
        if (item.PackagingStatus != PackagingState.Completed)
            return;
        if (item.DeliveryStatus is DeliveryState.Failed)
            return;
        item.DeliveryStatus = DeliveryState.ReadyToShip;
    }

    /// <summary>
    /// Syncs legacy <see cref="Order.DeliveryGroupId"/> when every non-failed packaged item shares the same group; otherwise clears it.
    /// </summary>
    public static void SyncOrderDeliveryGroupPointer(Order order, IReadOnlyList<OrderItem> items)
    {
        var shipped = items.Where(i => i.PackagingStatus == PackagingState.Completed).ToList();
        if (shipped.Count == 0)
        {
            order.DeliveryGroupId = null;
            return;
        }

        var ids = shipped
            .Where(i => i.DeliveryStatus != DeliveryState.Failed)
            .Select(i => i.DeliveryGroupId)
            .Distinct()
            .ToList();

        order.DeliveryGroupId = ids.Count == 1 ? ids[0] : null;
    }
}
