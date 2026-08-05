using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Vnta.Hrm.Web.Client.Validation;
namespace Vnta.Hrm.Web.Client.Models.Payroll;

public sealed class AttendanceAllowanceResultRecord : IValidatableObject
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private int payrollMonth = DateTime.Today.Month;
    private int payrollYear = DateTime.Today.Year;
    private decimal standardAllowanceAmount;
    private decimal standardWorkdayCount;
    private decimal actualWorkdayCount;

    public Guid Id { get; set; }

    public PayrollAllowanceKind AllowanceKind { get; set; } = PayrollAllowanceKind.Attendance;

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

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Số tiền phụ cấp chuẩn không được âm.")]
    public decimal StandardAllowanceAmount
    {
        get => standardAllowanceAmount;
        set => standardAllowanceAmount = value;
    }

    [InvariantDecimalRange("0.01", "9999999999", ErrorMessage = "Số ngày công chuẩn phải lớn hơn 0.")]
    public decimal StandardWorkdayCount
    {
        get => standardWorkdayCount;
        set => standardWorkdayCount = value;
    }

    [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "Số ngày công thực tế không được âm.")]
    public decimal ActualWorkdayCount
    {
        get => actualWorkdayCount;
        set => actualWorkdayCount = value;
    }

    public decimal AttendanceRate { get; private set; }

    public decimal ActualAllowanceAmount { get; private set; }

    public string? AppliedRuleKey { get; private set; }

    public string? AttendanceClass { get; private set; }

    public decimal? CtlWorkdayCount { get; private set; }

    public decimal? AdministrativeWorkdayCount { get; private set; }

    public decimal? LateEarlyDeductionDays { get; private set; }

    public int? LateEarlyMinutes { get; private set; }

    public decimal? Kqcc { get; private set; }

    public bool HasKpViolation { get; set; }

    public bool IsLocked { get; set; }

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

    public string AttendanceRateDisplay => AttendanceRate.ToString("P1", DisplayCulture);

    public string StandardAllowanceAmountDisplay => $"{StandardAllowanceAmount.ToString("N0", DisplayCulture)} ₫";

    public string ActualAllowanceAmountDisplay => $"{ActualAllowanceAmount.ToString("N0", DisplayCulture)} ₫";

    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";

    public string LockActionText => IsLocked ? "Mở khóa" : "Khóa";

    public void SetServerCalculatedValues(
        decimal attendanceRate,
        decimal actualAllowanceAmount,
        string? appliedRuleKey = null,
        string? attendanceClass = null,
        decimal? ctlWorkdayCount = null,
        int? lateEarlyMinutes = null,
        decimal? kqcc = null,
        bool hasKpViolation = false,
        decimal? administrativeWorkdayCount = null,
        decimal? lateEarlyDeductionDays = null)
    {
        HasKpViolation = hasKpViolation;
        AttendanceRate = attendanceRate;
        ActualAllowanceAmount = actualAllowanceAmount;
        AppliedRuleKey = appliedRuleKey;
        AttendanceClass = attendanceClass;
        CtlWorkdayCount = ctlWorkdayCount;
        AdministrativeWorkdayCount = administrativeWorkdayCount;
        LateEarlyDeductionDays = lateEarlyDeductionDays;
        LateEarlyMinutes = lateEarlyMinutes;
        Kqcc = kqcc;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(PayrollYear < 2026)
        {
            yield return new ValidationResult(
                "Năm kỳ lương phải từ 2026 trở đi.",
                [nameof(PayrollYear)]);
        }

        if(PayrollYear == 2026 && PayrollMonth < 6)
        {
            yield return new ValidationResult(
                "Tháng kỳ lương phải từ 06/2026 trở đi.",
                [nameof(PayrollMonth)]);
        }

        if(EmployeeId.HasValue && EmployeeId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "Nhân viên không hợp lệ.",
                [nameof(EmployeeId)]);
        }

        if(StandardWorkdayCount > 0 && ActualWorkdayCount > StandardWorkdayCount)
        {
            yield return new ValidationResult(
                "Số ngày công thực tế không được lớn hơn số ngày công chuẩn.",
                [nameof(ActualWorkdayCount)]);
        }
    }

    private static string? NormalizeDisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
