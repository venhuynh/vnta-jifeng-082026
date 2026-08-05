namespace Vnta.Hrm.Application.CaKip.LichLamViec;

public enum AttendanceWorkCalendarDayType : short
{
    Regular = 0,
    DayOff = 1,
    Holiday = 2
}

// Quy tắc mặc định chỉ là fallback hiển thị; calendar cấu hình vẫn được ưu tiên khi có record cho ngày đó.
public static class AttendanceWorkCalendarDayTypes
{
    public const string Regular = "Ngày thường";
    public const string DayOff = "Ngày nghỉ";
    public const string Holiday = "Ngày lễ";

    public static readonly IReadOnlyList<AttendanceWorkCalendarDayType> All =
    [
        AttendanceWorkCalendarDayType.Regular,
        AttendanceWorkCalendarDayType.DayOff,
        AttendanceWorkCalendarDayType.Holiday
    ];

    public static readonly IReadOnlyList<AttendanceWorkCalendarDayType> SpecialDays =
    [
        AttendanceWorkCalendarDayType.DayOff,
        AttendanceWorkCalendarDayType.Holiday
    ];

    public static bool IsSpecialDay(AttendanceWorkCalendarDayType dayType) =>
        SpecialDays.Contains(dayType);

    // Chủ nhật được tô như ngày nghỉ ngay cả khi calendar chưa tải được, để màn có degraded state nhất quán.
    public static AttendanceWorkCalendarDayType ResolveDefaultDayType(DateOnly workDate) =>
        workDate.DayOfWeek == DayOfWeek.Sunday
            ? AttendanceWorkCalendarDayType.DayOff
            : AttendanceWorkCalendarDayType.Regular;

    public static string GetDisplayName(AttendanceWorkCalendarDayType dayType) => dayType switch
    {
        AttendanceWorkCalendarDayType.Regular => Regular,
        AttendanceWorkCalendarDayType.DayOff => DayOff,
        AttendanceWorkCalendarDayType.Holiday => Holiday,
        _ => Regular
    };

    public static string GetShortDisplayName(AttendanceWorkCalendarDayType dayType) => dayType switch
    {
        AttendanceWorkCalendarDayType.DayOff => "Nghỉ",
        AttendanceWorkCalendarDayType.Holiday => "Lễ",
        _ => string.Empty
    };
}
