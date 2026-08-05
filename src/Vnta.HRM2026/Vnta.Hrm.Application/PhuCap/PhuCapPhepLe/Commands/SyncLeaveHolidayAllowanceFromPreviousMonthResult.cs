namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public sealed record SyncLeaveHolidayAllowanceFromPreviousMonthResult(
    int SourcePayrollMonth,
    int SourcePayrollYear,
    int TargetPayrollMonth,
    int TargetPayrollYear,
    int SourceRowCount,
    int TargetRowCount,
    int UpdatedCount,
    int SkippedLockedCount,
    int MissingSourceCount,
    int UnchangedCount);
