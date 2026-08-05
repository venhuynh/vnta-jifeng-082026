namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

/// <summary>
/// Allowlist dữ liệu được phép chuyển tới lớp tạo tệp tổng kết khấu trừ.
/// </summary>
public sealed record PayrollDeductionSummaryExportItemDto(
    string EmployeeDisplay,
    string DepartmentDisplay,
    string PositionDisplay,
    string PayrollPeriodDisplay,
    decimal SocialInsuranceDeductionAmount,
    decimal PersonalIncomeTaxDeductionAmount,
    decimal UnionFeeDeductionAmount,
    decimal AdvanceDeductionAmount,
    decimal OtherDeductionAmount,
    decimal TotalDeductionAmount,
    string LockStatusText);
