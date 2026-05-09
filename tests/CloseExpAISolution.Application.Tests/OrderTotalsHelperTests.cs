using CloseExpAISolution.Application;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class OrderTotalsHelperTests
{
    [Fact]
    public void ComputeFinalAmount_IncludesDeliveryAndSystemFee_AfterDiscount()
    {
        var final = OrderTotalsHelper.ComputeFinalAmount(
            totalAmount: 100_000m,
            discountAmount: 10_000m,
            deliveryFee: 15_000m,
            systemUsageFeeAmount: 5_000m);

        Assert.Equal(110_000m, final);
    }

    [Fact]
    public void ComputeFinalAmount_ClampsSubtotalAtZero()
    {
        var final = OrderTotalsHelper.ComputeFinalAmount(
            totalAmount: 5_000m,
            discountAmount: 10_000m,
            deliveryFee: 0m,
            systemUsageFeeAmount: 5_000m);

        Assert.Equal(5_000m, final);
    }
}
