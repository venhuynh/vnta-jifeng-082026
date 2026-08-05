namespace Vnta.Hrm.Application.CaKip.LichLamViec;

/// <summary>
/// Snapshot theo năm để UI cache và dựng các cột ngày mà không cần biết schema calendar.
/// </summary>
public sealed record AttendanceWorkCalendarYearDto(
    int Year,
    IReadOnlyList<AttendanceWorkCalendarDayDto> Days);
