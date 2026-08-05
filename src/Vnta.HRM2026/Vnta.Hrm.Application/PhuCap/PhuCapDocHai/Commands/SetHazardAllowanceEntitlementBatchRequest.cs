namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>One selected detail row together with the timestamps observed by the client.</summary>
public sealed record HazardAllowanceEntitlementTarget(
    Guid PayrollAllowanceSummaryRecordId,
    DateTime OriginalDetailUpdatedAtUtc,
    DateTime OriginalSummaryUpdatedAtUtc);

/// <summary>Sets the allowance-entitlement state only for explicitly selected hazard snapshots.</summary>
public sealed record SetHazardAllowanceEntitlementBatchRequest(
    bool IsEligibleForAllowance,
    IReadOnlyList<HazardAllowanceEntitlementTarget> Targets,
    string RequestedBy);

/// <summary>Reports the number of selected rows inspected and changed by an entitlement command.</summary>
public sealed record SetHazardAllowanceEntitlementBatchResult(int TargetRowCount, int UpdatedCount);
