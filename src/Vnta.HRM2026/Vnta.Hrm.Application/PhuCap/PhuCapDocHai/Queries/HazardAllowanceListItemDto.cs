namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

public sealed record HazardAllowanceListItemDto(
    Guid PayrollAllowanceSummaryRecordId,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    int PayrollMonth,
    int PayrollYear,
    decimal QualifiedWorkdayCount,
    decimal LateEarlyDeductionDays,
    decimal PayableWorkdayCount,
    decimal HazardAllowancePerDay,
    decimal HazardAllowanceAmount,
    bool IsEligibleDepartment,
    string? ExclusionReason,
    bool IsLocked,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    DateTime? SummaryUpdatedAtUtc)
{
    public bool IsEligibleForAllowance { get; init; }
    public string? DepartmentName { get; init; }
    public string? PositionName { get; init; }
}
