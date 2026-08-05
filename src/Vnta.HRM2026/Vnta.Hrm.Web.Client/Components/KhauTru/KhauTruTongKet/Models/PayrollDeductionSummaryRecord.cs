using System.Globalization;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Models;

/// <summary>UI model for one deduction-summary row.</summary>
public sealed class PayrollDeductionSummaryRecord
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public int PayrollMonth { get; set; }
    public int PayrollYear { get; set; }
    public decimal SocialInsuranceDeductionAmount { get; set; }
    public decimal PersonalIncomeTaxDeductionAmount { get; set; }
    public decimal UnionFeeDeductionAmount { get; set; }
    public decimal AdvanceDeductionAmount { get; set; }
    public decimal OtherDeductionAmount { get; set; }
    public bool IsLocked { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    public string EmployeeDisplay
    {
        get
        {
            var code = NormalizeDisplayText(EmployeeCode);
            var fullName = NormalizeDisplayText(EmployeeName);
            return code is not null && fullName is not null
                ? $"{code} - {fullName}"
                : fullName ?? code ?? "Chưa có nhân viên";
        }
    }

    public string DepartmentDisplay => NormalizeDisplayText(DepartmentName) ?? "Chưa có phòng ban";
    public string PositionDisplay => NormalizeDisplayText(PositionName) ?? "Chưa có chức vụ";
    public string PayrollPeriodDisplay => $"{PayrollMonth:00}/{PayrollYear}";
    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";
    public string LockActionText => IsLocked ? "Mở khóa" : "Khóa";
    public decimal TotalDeductionAmount => SocialInsuranceDeductionAmount + PersonalIncomeTaxDeductionAmount
        + UnionFeeDeductionAmount + AdvanceDeductionAmount + OtherDeductionAmount;
    public string TotalDeductionAmountDisplay => TotalDeductionAmount.ToString("N0", DisplayCulture);

    private static string? NormalizeDisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
