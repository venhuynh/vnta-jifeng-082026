namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;

public sealed record OtherAllowanceListItemDto(
    Guid Id,
    Guid PayrollAllowanceSummaryRecordId,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    string AllowanceName,
    bool IsFixedAmount,
    decimal AllowanceAmount,
    string? Note,
    bool IsLocked,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);
