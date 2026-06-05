using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Domain.Entities;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class PromotionLineAllocationTests
{
    [Fact]
    public void GetRefundableLineAmount_UsesDiscountedShare_NotListPrice()
    {
        var item1Id = Guid.NewGuid();
        var item2Id = Guid.NewGuid();

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            TotalAmount = 100_000,
            DiscountAmount = 20_000,
            OrderItems =
            [
                new OrderItem { OrderItemId = item1Id, TotalPrice = 60_000 },
                new OrderItem { OrderItemId = item2Id, TotalPrice = 40_000 }
            ]
        };

        var paid1 = PromotionLineAllocation.GetRefundableLineAmount(order, order.OrderItems.First());
        var paid2 = PromotionLineAllocation.GetRefundableLineAmount(order, order.OrderItems.Last());

        Assert.Equal(48_000, paid1);
        Assert.Equal(32_000, paid2);
        Assert.Equal(80_000, paid1 + paid2);
    }
}
