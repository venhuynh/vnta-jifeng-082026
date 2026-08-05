namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Queries;

/// <summary>
/// Dữ liệu một dòng snapshot dành cho danh sách; gồm định danh nhân sự, các khoản phụ cấp và metadata kiểm soát đồng thời.
/// </summary>
public sealed record PayrollAllowanceSummaryListItemDto(
    Guid Id,
    Guid EmployeeId,
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
    bool IsLocked,
    string? Note,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);
