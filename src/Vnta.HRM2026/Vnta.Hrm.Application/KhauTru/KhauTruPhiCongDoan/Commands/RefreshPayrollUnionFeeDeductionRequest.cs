namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public sealed record RefreshPayrollUnionFeeDeductionRequest(
    int PayrollYear,
    int PayrollMonth,
    Guid? PayrollDeductionSummaryRecordId = null);
