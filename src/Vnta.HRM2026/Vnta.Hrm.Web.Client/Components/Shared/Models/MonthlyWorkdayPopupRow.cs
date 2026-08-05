namespace Vnta.Hrm.Web.Client.Components.Shared.Models;

/// <summary>
/// Shared display model for a single day in a monthly-work popup. It is intentionally
/// independent of every allowance feature so payroll and deduction screens can share it.
/// </summary>
public sealed record MonthlyWorkdayPopupRow(
    Guid Id,
    DateOnly WorkDate,
    string DayType,
    string ShiftShortName,
    string? ShiftColorHex,
    string? CheckInAt,
    string? CheckOutAt,
    string Status,
    int LateMinutes,
    int EarlyLeaveMinutes,
    int OvertimeMinutes,
    int OvertimeMinutes15,
    int OvertimeMinutes20,
    int OvertimeMinutes30,
    string LockStatus,
    bool IsLocked)
{
    public int LateEarlyTotalMinutes => Math.Max(0, LateMinutes) + Math.Max(0, EarlyLeaveMinutes);

    public bool HasCheckInOrOut => !string.IsNullOrWhiteSpace(CheckInAt)
        || !string.IsNullOrWhiteSpace(CheckOutAt);

    public bool IsRegularWorkday => string.Equals(DayType, "Ngày thường", StringComparison.OrdinalIgnoreCase)
        || string.Equals(DayType, "regular", StringComparison.OrdinalIgnoreCase);

    public decimal SalaryWorkday => IsRegularWorkday ? 1m : 0m;
}
