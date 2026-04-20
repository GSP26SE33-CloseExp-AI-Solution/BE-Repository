using CloseExpAISolution.Application.Services.Fulfillment;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class StaleReadyToShipPolicyTests
{
    private static readonly DateTime BaseRtsAtUtc = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ComputeDeadline_AddsMaxWaitMinutesToReadyToShipAt()
    {
        var deadline = StaleReadyToShipPolicy.ComputeDeadline(BaseRtsAtUtc, 75);

        Assert.Equal(BaseRtsAtUtc.AddMinutes(75), deadline);
    }

    [Theory]
    [InlineData(0, false)]   // ngay tại T0: chưa quá hạn
    [InlineData(74, false)]  // còn 1 phút
    [InlineData(75, true)]   // đúng deadline
    [InlineData(120, true)]  // đã quá hạn 45 phút
    public void IsDueForRefund_ReturnsExpected(int minutesAfterRts, bool expected)
    {
        var now = BaseRtsAtUtc.AddMinutes(minutesAfterRts);

        var actual = StaleReadyToShipPolicy.IsDueForRefund(BaseRtsAtUtc, now, maxWaitMinutes: 75);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildRefundReason_ContainsThresholdAndRtsTimestamp()
    {
        var reason = StaleReadyToShipPolicy.BuildRefundReason(BaseRtsAtUtc, 75);

        Assert.Contains("75 phút", reason);
        Assert.Contains("2026-04-20 10:00 UTC", reason);
        Assert.Contains("Sẵn sàng giao", reason);
    }
}
