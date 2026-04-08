using CloseExpAISolution.Application.Services.Fulfillment;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class RefundAmountCalculatorTests
{
    [Theory]
    [InlineData(100000, 0, 100000)]
    [InlineData(100000, 25000, 75000)]
    [InlineData(100000, 100000, 0)]
    [InlineData(100000, 120000, 0)]
    public void ComputeRefundable_ReturnsExpected(decimal paidAmount, decimal alreadyRefunded, decimal expected)
    {
        Assert.Equal(expected, RefundAmountCalculator.ComputeRefundable(paidAmount, alreadyRefunded));
    }
}

