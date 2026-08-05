namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public sealed record RefreshPayrollInsuranceDeductionResult(
    int PayrollMonth,
    int PayrollYear,
    int MatchedRowCount,
    int UpdatedCount,
    int SkippedLockedCount);
