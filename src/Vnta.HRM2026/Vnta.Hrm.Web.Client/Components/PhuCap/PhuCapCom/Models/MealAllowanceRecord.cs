using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Vnta.Hrm.Web.Client.Models.Payroll;

/// <summary>
/// UI record for the meal allowance feature. The established public namespace
/// remains unchanged while the source lives with its owning feature.
/// </summary>
public sealed class MealAllowanceRecord : IValidatableObject
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private const decimal DefaultMealAllowancePerQualifiedDay = 18000m;
    private int payrollMonth = DateTime.Today.Month;
    private int payrollYear = DateTime.Today.Year;
    private int qualifiedMealDays;
    private int overtime1900Days;
    private decimal mealAllowancePerQualifiedDay = DefaultMealAllowancePerQualifiedDay;

    public Guid Id { get; set; }

    [Required(ErrorMessage = "Nhân viên không được để trống.")]
    public Guid? EmployeeId { get; set; }

    public string? EmployeeCode { get; set; }

    public string? EmployeeName { get; set; }

    public string? DepartmentName { get; set; }

    public string? PositionName { get; set; }

    [Range(1, 12, ErrorMessage = "Tháng kỳ lương phải nằm trong khoảng từ 1 đến 12.")]
    public int PayrollMonth
    {
        get => payrollMonth;
        set => payrollMonth = value;
    }

    [Range(2000, 2100, ErrorMessage = "Năm kỳ lương không hợp lệ.")]
    public int PayrollYear
    {
        get => payrollYear;
        set => payrollYear = value;
    }

    [Range(0, int.MaxValue, ErrorMessage = "Số ngày đủ điều kiện không được âm.")]
    public int QualifiedMealDays
    {
        get => qualifiedMealDays;
        set
        {
            qualifiedMealDays = Math.Max(0, value);
            Recalculate();
        }
    }

    [Range(0, int.MaxValue, ErrorMessage = "Số ngày tăng ca 19:00 không được âm.")]
    public int Overtime1900Days
    {
        get => overtime1900Days;
        set => overtime1900Days = Math.Max(0, value);
    }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Đơn giá phụ cấp cơm không được âm.")]
    public decimal MealAllowancePerQualifiedDay
    {
        get => mealAllowancePerQualifiedDay;
        set
        {
            mealAllowancePerQualifiedDay = Math.Max(0m, value);
            Recalculate();
        }
    }

    public decimal MealAllowanceAmount { get; private set; }

    public string RuleCode { get; set; } = "qualified-meal";

    public string? RuleVersion { get; set; }

    public string? Note { get; set; }

    public bool IsLocked { get; set; }

    public DateTime CalculatedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string EmployeeDisplay
    {
        get
        {
            var code = NormalizeDisplayText(EmployeeCode);
            var fullName = NormalizeDisplayText(EmployeeName);

            if(code is not null && fullName is not null)
            {
                return $"{code} - {fullName}";
            }

            return fullName ?? code ?? "Chưa chọn nhân viên";
        }
    }

    public string DepartmentDisplay => NormalizeDisplayText(DepartmentName) ?? "Chưa có phòng ban";

    public string PositionDisplay => NormalizeDisplayText(PositionName) ?? "Chưa có chức vụ";

    public string PayrollPeriodDisplay => $"{PayrollMonth:00}/{PayrollYear}";

    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";

    public string RuleCodeDisplay => NormalizeDisplayText(RuleCode) ?? "--";

    public string MealAllowancePerQualifiedDayDisplay => MealAllowancePerQualifiedDay.ToString("N0", DisplayCulture);

    public string MealAllowanceAmountDisplay => MealAllowanceAmount.ToString("N0", DisplayCulture);

    public string UpdatedAtDisplay =>
        (UpdatedAtUtc ?? CalculatedAtUtc).ToString("dd/MM/yyyy HH:mm", DisplayCulture);

    public void RecalculateDerivedValues() => Recalculate();

    public void SetServerCalculatedValues(decimal mealAllowanceAmount)
    {
        MealAllowanceAmount = decimal.Round(mealAllowanceAmount, 2, MidpointRounding.AwayFromZero);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(EmployeeId.HasValue && EmployeeId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "Nhân viên không hợp lệ.",
                [nameof(EmployeeId)]);
        }
    }

    private void Recalculate()
    {
        MealAllowanceAmount = decimal.Round(
            Overtime1900Days * MealAllowancePerQualifiedDay,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static string? NormalizeDisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
