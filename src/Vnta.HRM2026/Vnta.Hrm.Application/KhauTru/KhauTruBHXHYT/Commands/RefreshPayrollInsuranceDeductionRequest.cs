namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public sealed record RefreshPayrollInsuranceDeductionRequest(
    int TargetPayrollMonth,
    int TargetPayrollYear,
    Guid? PayrollDeductionSummaryRecordId = null);
