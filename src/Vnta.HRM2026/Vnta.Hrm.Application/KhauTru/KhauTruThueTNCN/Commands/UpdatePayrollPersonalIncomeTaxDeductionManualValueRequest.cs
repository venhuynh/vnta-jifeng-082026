namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

public sealed record UpdatePayrollPersonalIncomeTaxDeductionManualValueRequest(
    Guid PayrollDeductionSummaryRecordId,
    decimal DeductionAmount,
    DateTime? OriginalUpdatedAtUtc);
