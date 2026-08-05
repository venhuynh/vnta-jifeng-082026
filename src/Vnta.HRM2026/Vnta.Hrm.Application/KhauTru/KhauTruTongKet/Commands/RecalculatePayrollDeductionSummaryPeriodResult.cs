namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record RecalculatePayrollDeductionSummaryPeriodResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount,
    int UnchangedCount,
    int SkippedLockedCount,
    int MissingSourceCount);
