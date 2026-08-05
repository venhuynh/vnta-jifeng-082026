namespace Vnta.Hrm.Application.DangTrienKhai.BangCongNgay;

public sealed record UpdateAttendanceWorkdaySummaryRequest(
    Guid Id,
    string DayType,
    string? CheckInAt,
    string? CheckOutAt,
    string? StatusCode,
    int LateMinutes,
    int EarlyLeaveMinutes,
    bool IsRegisterForOT,
    int OvertimeMinutes,
    bool RequireDocument,
    string? Note);
