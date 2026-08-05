namespace Vnta.Hrm.Application.CaKip.LichLamViec;

/// <summary>
/// Contract read-only của một ngày cấu hình; không lộ row Infrastructure cho component.
/// </summary>
public sealed record AttendanceWorkCalendarDayDto(
    Guid Id,
    DateOnly WorkDate,
    AttendanceWorkCalendarDayType DayType,
    string? Name,
    string? Note,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
