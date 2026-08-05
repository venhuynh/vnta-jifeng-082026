namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public sealed record SyncPayrollInsuranceDeductionFromPreviousMonthResult(
    int SourcePayrollMonth,
    int SourcePayrollYear,
    int TargetPayrollMonth,
    int TargetPayrollYear,
    int MatchedEmployeeCount,
    int SeededSourceSummaryCount,
    int SeededTargetSummaryCount,
    int SourceRowCount,
    int CreatedCount,
    int UpdatedCount,
    int SkippedLockedCount);
