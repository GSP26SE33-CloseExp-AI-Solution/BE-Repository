namespace CloseExpAISolution.Application.Policies;

public static class DailyExpiryOrderingPolicy
{
    private static readonly TimeZoneInfo VietnamTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private static readonly TimeSpan DefaultCutoffTime = new(21, 0, 0);

    public static DateTime GetVietnamNow(DateTime utcNow)
        => TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(utcNow), VietnamTimeZone);

    public static bool IsOrderCutoffReached(DateTime utcNow, TimeSpan? cutoffTime = null)
    {
        var nowVn = GetVietnamNow(utcNow);
        var cutoff = cutoffTime ?? DefaultCutoffTime;
        return nowVn.TimeOfDay >= cutoff;
    }

    public static (DateTime StartUtc, DateTime EndUtc) GetVietnamDateRangeUtc(DateTime utcNow)
    {
        var nowVn = GetVietnamNow(utcNow);
        var startLocal = nowVn.Date;
        var endLocal = startLocal.AddDays(1);

        return (
            TimeZoneInfo.ConvertTimeToUtc(startLocal, VietnamTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(endLocal, VietnamTimeZone));
    }

    public static bool IsExpiringInVietnamToday(DateTime expiryUtc, DateTime utcNow)
    {
        var (startUtc, endUtc) = GetVietnamDateRangeUtc(utcNow);
        var normalizedExpiry = EnsureUtc(expiryUtc);
        return normalizedExpiry >= startUtc && normalizedExpiry < endUtc;
    }

    public static bool IsLotBlockedForOrdering(DateTime expiryUtc, DateTime utcNow)
    {
        if (!IsOrderCutoffReached(utcNow))
            return false;

        return IsExpiringInVietnamToday(expiryUtc, utcNow);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
