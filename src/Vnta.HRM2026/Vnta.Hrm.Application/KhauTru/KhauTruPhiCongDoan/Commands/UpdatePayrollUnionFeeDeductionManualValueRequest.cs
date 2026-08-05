namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

/// <summary>Giá trị được phép điều chỉnh thủ công trên một dòng phí công đoàn.</summary>
public sealed record UpdatePayrollUnionFeeDeductionManualValueRequest(
    Guid PayrollDeductionSummaryRecordId,
    decimal DeductionAmount,
    DateTime OriginalVersionAtUtc);
