namespace Vnta.Hrm.Web.Client.Models.Payroll;

/// <summary>
/// Model dữ liệu được client dùng để dựng Excel/PDF.
/// Nó chỉ nhận các trường đã được backend allowlist qua DTO xuất dữ liệu.
/// </summary>
public sealed class PayrollAllowanceSummaryExportRecord
{
    #region Dữ liệu xuất

    public string? EmployeeCode { get; init; }

    public string? EmployeeName { get; init; }

    public string? DepartmentName { get; init; }

    public string? PositionName { get; init; }

    public int PayrollMonth { get; init; }

    public int PayrollYear { get; init; }

    public decimal ResponsibilityAllowanceAmount { get; init; }

    public decimal ResponsibilityOtherAllowanceAmount { get; init; }

    public decimal SeniorityAllowanceAmount { get; init; }

    public decimal AttendanceAllowanceAmount { get; init; }

    public decimal MealAllowanceAmount { get; init; }

    public decimal HazardAllowanceAmount { get; init; }

    public decimal OtherAllowanceAmount { get; init; }

    public decimal LeaveHolidayAllowanceAmount { get; init; }

    public decimal TotalAllowanceAmount { get; init; }

    public bool IsLocked { get; init; }

    public string? Note { get; init; }

    #endregion

    #region Giá trị hiển thị

    public string PayrollPeriodDisplay => $"{PayrollMonth:00}/{PayrollYear}";

    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";
    #endregion
}
