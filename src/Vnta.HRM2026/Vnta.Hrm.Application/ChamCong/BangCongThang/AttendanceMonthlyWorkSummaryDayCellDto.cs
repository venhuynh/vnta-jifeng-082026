namespace Vnta.Hrm.Application.ChamCong.BangCongThang;

/// <summary>
/// Snapshot read-only của một ngày công, đủ dữ liệu cho ô grid và tooltip nhưng không lộ row persistence.
/// </summary>
public sealed record AttendanceMonthlyWorkSummaryDayCellDto(
    Guid Id,
    DateOnly WorkDate,
    string DayType,
    string? ShiftCode,
    string? ShiftShortName,
    string? ShiftName,
    string? ShiftColorHex,
    string? CheckInAt,
    string? CheckOutAt,
    int LateMinutes,
    int EarlyLeaveMinutes,
    string Status,
    bool IsLocked,
    int OvertimeMinutes,
    int OvertimeMinutes15,
    int OvertimeMinutes20,
    int OvertimeMinutes30,
    DateTime ComputedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
