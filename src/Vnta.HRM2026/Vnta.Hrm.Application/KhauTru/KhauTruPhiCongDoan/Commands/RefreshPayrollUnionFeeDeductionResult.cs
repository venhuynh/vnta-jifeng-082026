namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public sealed record RefreshPayrollUnionFeeDeductionResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount,
    int SkippedLockedCount);
