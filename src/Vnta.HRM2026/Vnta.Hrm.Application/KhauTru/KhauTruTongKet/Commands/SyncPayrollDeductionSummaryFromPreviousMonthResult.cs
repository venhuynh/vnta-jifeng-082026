namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record SyncPayrollDeductionSummaryFromPreviousMonthResult(
    int SourcePayrollMonth,
    int SourcePayrollYear,
    int TargetPayrollMonth,
    int TargetPayrollYear,
    int SourceRecordCount,
    int CreatedCount,
    int UpdatedCount,
    int SkippedLockedCount,
    int AttendanceEmployeeCount,
    int RemovedCount);
