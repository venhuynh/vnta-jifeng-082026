using System.Globalization;
using System.ComponentModel.DataAnnotations;
namespace Vnta.Hrm.Web.Client.Models;

public sealed class AttendanceWorkdaySummaryRecord
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    [StringLength(50)]
    public string? EmployeeCode { get; set; }

    [StringLength(200)]
    public string? EmployeeName { get; set; }

    [StringLength(200)]
    public string? DepartmentName { get; set; }

    [StringLength(200)]
    public string? PositionName { get; set; }

    public DateOnly WorkDate { get; set; }

    [Required]
    public string DayType { get; set; } = string.Empty;

    public Guid? ShiftId { get; set; }

    [StringLength(50)]
    public string? ShiftCode { get; set; }

    [StringLength(50)]
    public string? ShiftShortName { get; set; }

    [StringLength(200)]
    public string? ShiftName { get; set; }

    [StringLength(7)]
    public string? ShiftColorHex { get; set; }

    [StringLength(20)]
    public string? ScheduledStartAt { get; set; }

    [StringLength(20)]
    public string? ScheduledEndAt { get; set; }

    [StringLength(20)]
    public string? CheckInAt { get; set; }

    [StringLength(20)]
    public string? CheckOutAt { get; set; }

    public int LateMinutes { get; set; }

    public int EarlyLeaveMinutes { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    public bool IsLocked { get; set; }

    public int OvertimeMinutes { get; set; }

    public int OvertimeMinutes15 { get; set; }

    public int OvertimeMinutes20 { get; set; }

    public int OvertimeMinutes30 { get; set; }

    [StringLength(20)]
    public string? CheckInForOT15 { get; set; }

    public bool IsRegisterForOT { get; set; }

    public bool RequireDocument { get; set; }

    public string? Note { get; set; }

    public DateTime ComputedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string EmployeeDisplay
    {
        get
        {
            var parts = new[] { EmployeeCode, EmployeeName }
                .Where(static part => !string.IsNullOrWhiteSpace(part))
                .Select(static part => part!.Trim())
                .ToArray();

            return parts.Length == 0 ? "--" : string.Join(" - ", parts);
        }
    }

    public string DayTypeDisplay => DayType switch
    {
        "regular" => AttendanceWorkCalendarDayTypes.Regular,
        AttendanceWorkCalendarDayTypes.Regular => AttendanceWorkCalendarDayTypes.Regular,
        AttendanceWorkCalendarDayTypes.DayOff => AttendanceWorkCalendarDayTypes.DayOff,
        AttendanceWorkCalendarDayTypes.Holiday => AttendanceWorkCalendarDayTypes.Holiday,
        _ => string.IsNullOrWhiteSpace(DayType) ? "--" : DayType
    };

    public string ShiftDisplay
    {
        get
        {
            var parts = new[] { ShiftCode, ShiftName }
                .Where(static part => !string.IsNullOrWhiteSpace(part))
                .Select(static part => part!.Trim())
                .ToArray();

            return parts.Length == 0 ? "--" : string.Join(" - ", parts);
        }
    }

    public string ShiftShortDisplay =>
        NormalizeDisplayValue(ShiftShortName)
        ?? NormalizeDisplayValue(ShiftCode)
        ?? NormalizeDisplayValue(ShiftName)
        ?? "--";

    public string ScheduleDisplay => BuildTimeRange(ScheduledStartAt, ScheduledEndAt);

    public string? CheckInDisplay => FormatTimeValue(CheckInAt);

    public string? CheckOutDisplay => FormatTimeValue(CheckOutAt);

    public string CheckInOutDisplay => BuildTimeRange(CheckInAt, CheckOutAt);

    public int LateEarlyTotalMinutes => Math.Max(0, LateMinutes) + Math.Max(0, EarlyLeaveMinutes);

    private static string BuildTimeRange(string? start, string? end)
    {
        var parts = new[] { start, end }
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => FormatTimeValue(value) ?? value!.Trim())
            .ToArray();

        return parts.Length == 0 ? "--" : string.Join(" - ", parts);
    }

    private static string? FormatTimeValue(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim();
        var formats = new[] { "HH:mm:ss", "HH:mm" };

        return TimeOnly.TryParseExact(
            trimmedValue,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedTime)
            ? parsedTime.ToString("HH:mm", CultureInfo.InvariantCulture)
            : trimmedValue;
    }

    private static string? NormalizeDisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
