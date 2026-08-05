using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruBHXHYT.Models;

public sealed class PayrollInsuranceDeductionRecord : IValidatableObject
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private int payrollMonth = DateTime.Today.Month;
    private int payrollYear = DateTime.Today.Year;

    public Guid Id { get; set; }
    public Guid? PayrollDeductionSummaryRecordId { get; set; }
    [Required] public Guid? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    [Range(1, 12)] public int PayrollMonth { get => payrollMonth; set => payrollMonth = value; }
    [Range(2000, 2100)] public int PayrollYear { get => payrollYear; set => payrollYear = value; }
    [Range(typeof(decimal), "0", "9999999999999999")] public decimal InsuranceSalaryBaseAmount { get; set; }
    [Range(typeof(decimal), "0", "1")] public decimal SocialInsuranceRate { get; set; } = PayrollInsuranceDeductionStandardRates.SocialInsurance;
    [Range(typeof(decimal), "0", "1")] public decimal HealthInsuranceRate { get; set; } = PayrollInsuranceDeductionStandardRates.HealthInsurance;
    [Range(typeof(decimal), "0", "1")] public decimal UnemploymentInsuranceRate { get; set; } = PayrollInsuranceDeductionStandardRates.UnemploymentInsurance;
    public decimal TotalInsuranceRate { get; private set; }
    public decimal SocialInsuranceAmount { get; private set; }
    public decimal HealthInsuranceAmount { get; private set; }
    public decimal UnemploymentInsuranceAmount { get; private set; }
    public decimal TotalDeductionAmount { get; private set; }
    public bool IsParticipating { get; set; } = true;
    [Range(0, 3)] public short ParticipationChangeType { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public string EmployeeDisplay => JoinDisplay(EmployeeCode, EmployeeName) ?? "Chưa chọn nhân viên";
    public string DepartmentDisplay => Normalize(DepartmentName) ?? "Chưa có phòng ban";
    public string PositionDisplay => Normalize(PositionName) ?? "Chưa có chức vụ";
    public string PayrollPeriodDisplay => $"{PayrollMonth:00}/{PayrollYear}";
    public string TotalInsuranceRateDisplay => TotalInsuranceRate.ToString("P2", DisplayCulture);
    public string TotalDeductionAmountDisplay => TotalDeductionAmount.ToString("N2", DisplayCulture);
    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";
    public string LockActionText => IsLocked ? "Mở khóa" : "Khóa";
    public string ParticipationStatusText => IsParticipating ? "Tham gia" : "Không tham gia";
    public string ParticipationChangeText => ParticipationChangeType switch { 1 => "Tăng", 2 => "Giảm", 3 => "Điều chỉnh", _ => "Không đổi" };

    public void RecalculateDerivedValues() => Recalculate();

    public void SetServerCalculatedValues(decimal totalRate, decimal socialAmount, decimal healthAmount, decimal unemploymentAmount, decimal totalAmount)
    {
        TotalInsuranceRate = totalRate;
        SocialInsuranceAmount = socialAmount;
        HealthInsuranceAmount = healthAmount;
        UnemploymentInsuranceAmount = unemploymentAmount;
        TotalDeductionAmount = totalAmount;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!EmployeeId.HasValue || EmployeeId.Value == Guid.Empty)
            yield return new ValidationResult("Nhân viên không hợp lệ.", [nameof(EmployeeId)]);
    }

    private void Recalculate()
    {
        var calculatedValues = PayrollInsuranceDeductionCalculator.Calculate(
            new PayrollInsuranceDeductionCalculationInput(
                InsuranceSalaryBaseAmount,
                SocialInsuranceRate,
                HealthInsuranceRate,
                UnemploymentInsuranceRate,
                IsParticipating
                    ? InsuranceParticipationStatus.Participating
                    : InsuranceParticipationStatus.NotParticipating));
        SetServerCalculatedValues(
            calculatedValues.TotalInsuranceRate,
            calculatedValues.SocialInsuranceAmount,
            calculatedValues.HealthInsuranceAmount,
            calculatedValues.UnemploymentInsuranceAmount,
            calculatedValues.TotalDeductionAmount);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? JoinDisplay(string? code, string? name) => Normalize(code) is { } c && Normalize(name) is { } n ? $"{c} - {n}" : Normalize(name) ?? Normalize(code);
}
