namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public sealed record SetPayrollUnionFeeDeductionBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    IReadOnlyCollection<Guid>? PayrollDeductionSummaryRecordIds);
