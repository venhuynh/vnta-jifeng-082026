namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public sealed record SetLeaveHolidayAllowanceBatchLockStateResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount,
    int SkippedCount = 0);
