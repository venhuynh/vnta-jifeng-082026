namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record SetPayrollDeductionSummaryBatchLockStateResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount,
    int SkippedCount = 0);
