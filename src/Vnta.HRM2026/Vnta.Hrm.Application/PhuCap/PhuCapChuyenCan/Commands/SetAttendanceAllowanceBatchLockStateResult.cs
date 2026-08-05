namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

public sealed record SetAttendanceAllowanceBatchLockStateResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount,
    int UnchangedCount = 0,
    int SkippedSummaryLockedCount = 0,
    bool IsLocked = false,
    bool IsWholePeriod = false);
