namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Policies;

/// <summary>
/// Pure business policy for rebuilding one payroll allowance-summary snapshot from
/// the already-aggregated source allowance amounts.
/// </summary>
/// <remarks>
/// Source retrieval, persistence and audit timestamps deliberately sit outside this policy.
/// The policy intentionally performs neither validation nor rounding because the former
/// refresh implementation assigned the source decimal values as-is.
/// </remarks>
public static class PayrollAllowanceSummaryRefreshPolicy
{
    public static PayrollAllowanceSummaryRefreshDecision Decide(
        PayrollAllowanceSummaryRefreshInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.CurrentAmounts);
        ArgumentNullException.ThrowIfNull(input.SourceAmounts);

        if(input.LockState is PayrollAllowanceSummaryLockState.Locked)
        {
            return new PayrollAllowanceSummaryRefreshDecision(
                PayrollAllowanceSummaryRefreshDisposition.SkippedBecauseLocked,
                input.CurrentAmounts,
                input.PreservedManualNote);
        }

        if(input.CurrentAmounts == input.SourceAmounts)
        {
            return new PayrollAllowanceSummaryRefreshDecision(
                PayrollAllowanceSummaryRefreshDisposition.NoAllowanceChanges,
                input.CurrentAmounts,
                input.PreservedManualNote);
        }

        return new PayrollAllowanceSummaryRefreshDecision(
            PayrollAllowanceSummaryRefreshDisposition.SourceAmountsApplied,
            input.SourceAmounts,
            input.PreservedManualNote);
    }
}

/// <summary>Named allowance components used by the allowance-summary snapshot.</summary>
public sealed record PayrollAllowanceSummaryAllowanceAmounts(
    decimal Responsibility,
    decimal ResponsibilityOther,
    decimal Seniority,
    decimal Attendance,
    decimal Meal,
    decimal Hazard,
    decimal Other,
    decimal LeaveHoliday)
{
    public static PayrollAllowanceSummaryAllowanceAmounts Empty { get; } = new(
        Responsibility: 0m,
        ResponsibilityOther: 0m,
        Seniority: 0m,
        Attendance: 0m,
        Meal: 0m,
        Hazard: 0m,
        Other: 0m,
        LeaveHoliday: 0m);
}

/// <summary>Lock state expressed in the payroll-summary domain rather than as a transport boolean.</summary>
public enum PayrollAllowanceSummaryLockState
{
    Open,
    Locked
}

/// <summary>All facts the pure refresh policy needs for one existing or new summary row.</summary>
public sealed record PayrollAllowanceSummaryRefreshInput(
    PayrollAllowanceSummaryLockState LockState,
    PayrollAllowanceSummaryAllowanceAmounts CurrentAmounts,
    PayrollAllowanceSummaryAllowanceAmounts SourceAmounts,
    string? PreservedManualNote);

/// <summary>The distinct refresh outcomes supported by the established summary semantics.</summary>
public enum PayrollAllowanceSummaryRefreshDisposition
{
    SkippedBecauseLocked,
    NoAllowanceChanges,
    SourceAmountsApplied
}

/// <summary>Result to persist after applying the refresh policy.</summary>
public sealed record PayrollAllowanceSummaryRefreshDecision(
    PayrollAllowanceSummaryRefreshDisposition Disposition,
    PayrollAllowanceSummaryAllowanceAmounts ResultingAmounts,
    string? PreservedManualNote);
