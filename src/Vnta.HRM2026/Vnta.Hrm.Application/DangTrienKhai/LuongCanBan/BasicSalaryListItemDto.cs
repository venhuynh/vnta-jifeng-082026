namespace Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;

public sealed record BasicSalaryListItemDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? DepartmentName,
    string? DepartmentPath,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    decimal BasicSalary,
    decimal StandardWorkingDays,
    decimal DailySalary,
    decimal HourlySalary,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
