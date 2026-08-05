namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Models;

/// <summary>
/// Client-side allowlist used exclusively to render deduction-summary exports.
/// </summary>
public sealed class PayrollDeductionSummaryExportRecord
{
    public string EmployeeDisplay { get; init; } = string.Empty;

    public string DepartmentDisplay { get; init; } = string.Empty;

    public string PositionDisplay { get; init; } = string.Empty;

    public string PayrollPeriodDisplay { get; init; } = string.Empty;

    public decimal SocialInsuranceDeductionAmount { get; init; }

    public decimal PersonalIncomeTaxDeductionAmount { get; init; }

    public decimal UnionFeeDeductionAmount { get; init; }

    public decimal AdvanceDeductionAmount { get; init; }

    public decimal OtherDeductionAmount { get; init; }

    public decimal TotalDeductionAmount { get; init; }

    public string LockStatusText { get; init; } = string.Empty;
}
