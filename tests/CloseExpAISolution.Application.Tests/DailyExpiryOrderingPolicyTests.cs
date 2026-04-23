using CloseExpAISolution.Application.Policies;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class DailyExpiryOrderingPolicyTests
{
    [Fact]
    public void IsOrderCutoffReached_Before21Vn_ReturnsFalse()
    {
        var utcNow = new DateTime(2026, 4, 22, 13, 59, 0, DateTimeKind.Utc); // 20:59 VN

        var actual = DailyExpiryOrderingPolicy.IsOrderCutoffReached(utcNow);

        Assert.False(actual);
    }

    [Fact]
    public void IsOrderCutoffReached_At21Vn_ReturnsTrue()
    {
        var utcNow = new DateTime(2026, 4, 22, 14, 0, 0, DateTimeKind.Utc); // 21:00 VN

        var actual = DailyExpiryOrderingPolicy.IsOrderCutoffReached(utcNow);

        Assert.True(actual);
    }

    [Fact]
    public void IsLotBlockedForOrdering_AfterCutoff_ExpiryInVietnamToday_ReturnsTrue()
    {
        var utcNow = new DateTime(2026, 4, 22, 14, 30, 0, DateTimeKind.Utc); // 21:30 VN
        var expiryUtc = new DateTime(2026, 4, 22, 16, 0, 0, DateTimeKind.Utc); // 23:00 VN same day

        var actual = DailyExpiryOrderingPolicy.IsLotBlockedForOrdering(expiryUtc, utcNow);

        Assert.True(actual);
    }

    [Fact]
    public void IsLotBlockedForOrdering_AfterCutoff_ExpiryNotInVietnamToday_ReturnsFalse()
    {
        var utcNow = new DateTime(2026, 4, 22, 14, 30, 0, DateTimeKind.Utc); // 21:30 VN
        var expiryUtc = new DateTime(2026, 4, 22, 20, 0, 0, DateTimeKind.Utc); // 03:00 VN next day

        var actual = DailyExpiryOrderingPolicy.IsLotBlockedForOrdering(expiryUtc, utcNow);

        Assert.False(actual);
    }
}
