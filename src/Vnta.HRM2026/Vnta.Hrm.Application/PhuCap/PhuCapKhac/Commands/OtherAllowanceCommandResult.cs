namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

/// <summary>
/// Snapshot returned by a successful create or update command. Its JSON shape is
/// intentionally the same as the historical row response.
/// </summary>
public sealed record OtherAllowanceCommandResult(
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
