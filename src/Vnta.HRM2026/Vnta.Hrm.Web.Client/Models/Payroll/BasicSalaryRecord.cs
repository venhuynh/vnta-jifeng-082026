using System.ComponentModel.DataAnnotations;
using Vnta.Hrm.Web.Client.Validation;

namespace Vnta.Hrm.Web.Client.Models.Payroll;

public sealed class BasicSalaryRecord : IValidatableObject
{
    [Required(ErrorMessage = "Nhân viên không được để trống.")]
    public Guid? EmployeeId { get; set; }

    public Guid Id { get; set; }

    public string? EmployeeCode { get; set; }

    public string? EmployeeName { get; set; }

    public string? DepartmentName { get; set; }

    public string? DepartmentPath { get; set; }

    public string? PositionName { get; set; }

    [Range(1, 12, ErrorMessage = "Tháng áp dụng phải nằm trong khoảng từ 1 đến 12.")]
    public int PayrollMonth { get; set; }

    [Range(1, 9999, ErrorMessage = "Năm áp dụng không hợp lệ.")]
    public int PayrollYear { get; set; }

    [InvariantDecimalRange("0.01", "9999999999999999.99", ErrorMessage = "Lương căn bản phải lớn hơn 0.")]
    public decimal BasicSalary { get; set; }

    [InvariantDecimalRange("0.01", "999.99", ErrorMessage = "Số ngày làm việc tiêu chuẩn phải lớn hơn 0.")]
    public decimal StandardWorkingDays { get; set; }

    [InvariantDecimalRange("0", "9999999999999999.9999", ErrorMessage = "Lương ngày không được nhỏ hơn 0.")]
    public decimal DailySalary { get; set; }

    [InvariantDecimalRange("0", "9999999999999999.9999", ErrorMessage = "Lương giờ không được nhỏ hơn 0.")]
    public decimal HourlySalary { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string EmployeeDisplayText
    {
        get
        {
            var employeeName = NormalizeDisplayText(EmployeeName) ?? "Chưa chọn nhân viên";
            var employeeCode = NormalizeDisplayText(EmployeeCode);

            return employeeCode is null
                ? employeeName
                : $"{employeeCode} - {employeeName}";
        }
    }

    public string PeriodDisplayText => $"Tháng {PayrollMonth:00}/{PayrollYear:0000}";

    public string SummaryText => $"{EmployeeDisplayText} - {PeriodDisplayText}";

    public string DepartmentDisplayText =>
        NormalizeDisplayText(DepartmentPath)
        ?? NormalizeDisplayText(DepartmentName)
        ?? "Chưa có phòng ban";

    public string PositionDisplayText =>
        NormalizeDisplayText(PositionName)
        ?? "Chưa có chức danh";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EmployeeId is null || EmployeeId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Nhân viên không hợp lệ.",
                [nameof(EmployeeId)]);
        }
    }

    private static string? NormalizeDisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
