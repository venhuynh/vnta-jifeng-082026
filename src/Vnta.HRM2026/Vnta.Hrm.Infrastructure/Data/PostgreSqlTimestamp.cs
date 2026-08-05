namespace Vnta.Hrm.Infrastructure.Data;

/// <summary>
/// Normalizes values written to PostgreSQL <c>timestamp without time zone</c> columns.
/// PostgreSQL persists microsecond precision, while <see cref="DateTime"/> carries ticks.
/// </summary>
internal static class PostgreSqlTimestamp
{
    private const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1_000;

    public static DateTime ToTimestampWithoutTimeZone(DateTime value) =>
        new(value.Ticks - value.Ticks % TicksPerMicrosecond, DateTimeKind.Unspecified);

    public static DateTime? ToTimestampWithoutTimeZone(DateTime? value) =>
        value is { } timestamp
            ? ToTimestampWithoutTimeZone(timestamp)
            : null;
}
