namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Queries;

public sealed record MealAllowanceListItemDto(
    Guid Id,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    int QualifiedMealDays,
    int Overtime1900Days,
    decimal MealAllowancePerQualifiedDay,
    decimal MealAllowanceAmount,
    string RuleCode,
    string? RuleVersion,
    string? Note,
    bool IsLocked,
    DateTime CalculatedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
