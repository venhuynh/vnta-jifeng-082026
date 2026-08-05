namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>
/// Allowlist dữ liệu được phép rời backend để tạo báo cáo tổng hợp phụ cấp.
/// </summary>
public sealed record PayrollAllowanceSummaryExportRowDto(
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    decimal ResponsibilityAllowanceAmount,
    decimal ResponsibilityOtherAllowanceAmount,
    decimal SeniorityAllowanceAmount,
    decimal AttendanceAllowanceAmount,
    decimal MealAllowanceAmount,
    decimal HazardAllowanceAmount,
    decimal OtherAllowanceAmount,
    decimal LeaveHolidayAllowanceAmount,
    decimal TotalAllowanceAmount,
    bool IsLocked,
    string? Note);
