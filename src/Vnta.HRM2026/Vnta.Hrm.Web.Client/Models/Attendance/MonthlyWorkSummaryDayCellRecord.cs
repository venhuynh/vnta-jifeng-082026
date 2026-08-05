using System.Globalization;
namespace Vnta.Hrm.Web.Client.Models;

// View model ô ngày; các property display chỉ chuẩn hóa cho render, không quyết định kết quả tính công.
public sealed class MonthlyWorkSummaryDayCellRecord
{
    public Guid Id { get; init; }
    public DateOnly WorkDate { get; init; }
    public string DayType { get; init; } = string.Empty;
    public string? ShiftCode { get; init; }
    public string? ShiftShortName { get; init; }
    public string? ShiftName { get; init; }
    public string? ShiftColorHex { get; init; }
    public string? CheckInAt { get; init; }
    public string? CheckOutAt { get; init; }
    public int LateMinutes { get; init; }
    public int EarlyLeaveMinutes { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
    public int OvertimeMinutes { get; init; }
    public int OvertimeMinutes15 { get; init; }
    public int OvertimeMinutes20 { get; init; }
    public int OvertimeMinutes30 { get; init; }
    public DateTime ComputedAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }

    public string DayTypeDisplay => DayType switch
    {
        "regular" => AttendanceWorkCalendarDayTypes.Regular,
        AttendanceWorkCalendarDayTypes.Regular => AttendanceWorkCalendarDayTypes.Regular,
        AttendanceWorkCalendarDayTypes.DayOff => AttendanceWorkCalendarDayTypes.DayOff,
        AttendanceWorkCalendarDayTypes.Holiday => AttendanceWorkCalendarDayTypes.Holiday,
        _ => string.IsNullOrWhiteSpace(DayType) ? "--" : DayType
    };

    public string ShiftDisplay => BuildDisplay(ShiftCode, ShiftName);

    public string ShiftShortDisplay => NormalizeDisplayValue(ShiftShortName)
        ?? NormalizeDisplayValue(ShiftCode)
        ?? NormalizeDisplayValue(ShiftName)
        ?? "--";

    public string? CheckInDisplay => FormatTimeValue(CheckInAt);
    public string? CheckOutDisplay => FormatTimeValue(CheckOutAt);
    public string CheckInOutDisplay => BuildDisplay(CheckInAt, CheckOutAt, isTimeRange: true);
    public int LateEarlyTotalMinutes => Math.Max(0, LateMinutes) + Math.Max(0, EarlyLeaveMinutes);

    // Giữ định dạng giờ tại một chỗ để cell, tooltip và metadata không hiển thị lệch nhau.
    private static string BuildDisplay(string? first, string? second, bool isTimeRange = false)
    {
        var values = new[] { first, second }
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => isTimeRange ? FormatTimeValue(value) ?? value!.Trim() : value!.Trim())
            .ToArray();

        return values.Length == 0 ? "--" : string.Join(" - ", values);
    }

    private static string? FormatTimeValue(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim();
        return TimeOnly.TryParseExact(
            trimmedValue,
            ["HH:mm:ss", "HH:mm"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedTime)
            ? parsedTime.ToString("HH:mm", CultureInfo.InvariantCulture)
            : trimmedValue;
    }

    private static string? NormalizeDisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
