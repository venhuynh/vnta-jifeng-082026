namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Queries;

public sealed record OtherResponsibilityAllowanceListItemDto(
    Guid Id,
    Guid PayrollAllowanceSummaryRecordId,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    decimal AllowanceWorkdayCount,
    decimal StandardResponsibilityAllowanceAmount,
    decimal ActualResponsibilityAllowanceAmount,
    string? Note,
    bool IsLocked,
    DateTime? RefreshedAtUtc,
    string? RefreshedBy,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);
