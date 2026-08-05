namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

public sealed record UpdateHazardAllowanceManualValuesRequest(
    Guid PayrollAllowanceSummaryRecordId,
    decimal QualifiedWorkdayCount,
    decimal LateEarlyDeductionDays,
    decimal HazardAllowancePerDay,
    decimal HazardAllowanceAmount,
    bool IsEligibleDepartment,
    string? ExclusionReason,
    DateTime OriginalDetailUpdatedAtUtc,
    DateTime OriginalSummaryUpdatedAtUtc,
    string RequestedBy);
