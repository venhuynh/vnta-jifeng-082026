namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public sealed record SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount,
    int UnchangedCount = 0,
    int SkippedSummaryLockedCount = 0,
    bool IsLocked = false,
    bool IsWholePeriod = false,
    IReadOnlyList<PayrollEmployeeSeniorityAllowanceLockStateSkippedRow>? SkippedRows = null);
