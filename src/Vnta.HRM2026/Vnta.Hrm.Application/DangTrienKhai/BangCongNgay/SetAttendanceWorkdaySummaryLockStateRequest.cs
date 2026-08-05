namespace Vnta.Hrm.Application.DangTrienKhai.BangCongNgay;

public sealed record SetAttendanceWorkdaySummaryLockStateRequest(
    Guid Id,
    bool IsLocked);
