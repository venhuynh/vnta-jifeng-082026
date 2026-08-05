namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>Describes a row that could not be changed during a batch lock-state operation.</summary>
public sealed record PayrollEmployeeSeniorityAllowanceLockStateSkippedRow(
    Guid PayrollAllowanceSummaryRecordId,
    string? EmployeeCode,
    string? EmployeeName,
    string Reason);
