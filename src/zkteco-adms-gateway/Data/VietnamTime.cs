namespace Vnta.AttendanceGateway.Data;

internal static class VietnamTime
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public static TimeSpan VietnamOffset => VietnamTimeZone.BaseUtcOffset;

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTimeZone);

    public static DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    public static DateTimeOffset ToUtcForDatabase(DateTimeOffset value)
        => value.ToUniversalTime();

    public static DateTimeOffset FromDatabase(DateTimeOffset value)
        => TimeZoneInfo.ConvertTime(value, VietnamTimeZone);

    public static DateTime ToVietnamLocalTimestamp(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            return value;
        }

        var utcValue = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        var vietnamValue = TimeZoneInfo.ConvertTimeFromUtc(utcValue, VietnamTimeZone);
        return DateTime.SpecifyKind(vietnamValue, DateTimeKind.Unspecified);
    }
}
