using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Fulfillment;

public static class PromotionLineAllocation
{
    public static IReadOnlyDictionary<Guid, decimal> ComputePaidLineAmounts(Order order)
    {
        var items = order.OrderItems.ToList();
        var result = new Dictionary<Guid, decimal>();
        if (items.Count == 0)
            return result;

        var discount = order.DiscountAmount > 0 && order.TotalAmount > 0
            ? Math.Min(order.DiscountAmount, order.TotalAmount)
            : 0m;

        if (discount <= 0)
        {
            foreach (var item in items)
                result[item.OrderItemId] = item.TotalPrice;
            return result;
        }

        var remainingDiscount = discount;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (i == items.Count - 1)
            {
                result[item.OrderItemId] = Math.Max(0, item.TotalPrice - remainingDiscount);
                continue;
            }

            var share = Math.Round(discount * (item.TotalPrice / order.TotalAmount), 0, MidpointRounding.AwayFromZero);
            share = Math.Min(share, item.TotalPrice);
            share = Math.Min(share, remainingDiscount);
            result[item.OrderItemId] = Math.Max(0, item.TotalPrice - share);
            remainingDiscount -= share;
        }

        return result;
    }

    public static decimal GetRefundableLineAmount(Order order, OrderItem item)
    {
        var paidByLine = ComputePaidLineAmounts(order);
        return paidByLine.TryGetValue(item.OrderItemId, out var paid)
            ? paid
            : item.TotalPrice;
    }

    public static decimal SumRefundableLineAmounts(Order order, IEnumerable<OrderItem> items)
    {
        return items.Sum(i => GetRefundableLineAmount(order, i));
    }
}
