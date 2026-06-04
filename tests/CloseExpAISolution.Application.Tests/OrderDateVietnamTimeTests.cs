using CloseExpAISolution.Application.Policies;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class OrderDateVietnamTimeTests
{
    [Fact]
    public void ToUtcFromVietnamLocal_PreservesWallClockInVietnam()
    {
        var placedUtc = new DateTime(2026, 6, 3, 4, 30, 0, DateTimeKind.Utc);
        var placedVn = DailyExpiryOrderingPolicy.GetVietnamNow(placedUtc);

        var deliveryVn = new DateTime(2026, 6, 5, placedVn.Hour, placedVn.Minute, placedVn.Second);
        var orderDateUtc = DailyExpiryOrderingPolicy.ToUtcFromVietnamLocal(deliveryVn);
        var orderDateVn = DailyExpiryOrderingPolicy.GetVietnamNow(orderDateUtc);

        Assert.Equal(new DateTime(2026, 6, 5, 11, 30, 0), orderDateVn);
        Assert.Equal(11, orderDateVn.Hour);
        Assert.Equal(30, orderDateVn.Minute);
    }
}
