namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;

/// <summary>Pure lock transition rule for a deduction-summary snapshot.</summary>
public static class PayrollDeductionSummaryLockStatePolicy
{
    public static PayrollDeductionSummaryLockStateChangeDecision Decide(
        PayrollDeductionSummaryLockState currentState,
        PayrollDeductionSummaryLockState requestedState) =>
        currentState == requestedState
            ? PayrollDeductionSummaryLockStateChangeDecision.NoChangeRequired
            : PayrollDeductionSummaryLockStateChangeDecision.ChangeRequired;

    public static PayrollDeductionSummaryLockState FromPersistenceFlag(bool isLocked) =>
        isLocked ? PayrollDeductionSummaryLockState.Locked : PayrollDeductionSummaryLockState.Unlocked;
}

public enum PayrollDeductionSummaryLockState
{
    Unlocked,
    Locked
}

public enum PayrollDeductionSummaryLockStateChangeDecision
{
    NoChangeRequired,
    ChangeRequired
}

/// <summary>Compares a client-supplied snapshot version with the server record version.</summary>
public static class PayrollDeductionSummaryConcurrencyPolicy
{
    public static PayrollDeductionSummaryConcurrencyStatus Evaluate(
        PayrollDeductionSummaryVersionComparisonInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.CurrentRecordVersion == input.RequestedOriginalVersion
            ? PayrollDeductionSummaryConcurrencyStatus.VersionMatches
            : PayrollDeductionSummaryConcurrencyStatus.VersionConflict;
    }
}

public sealed record PayrollDeductionSummaryVersionComparisonInput(
    DateTime CurrentRecordVersion,
    DateTime? RequestedOriginalVersion);

public enum PayrollDeductionSummaryConcurrencyStatus
{
    VersionMatches,
    VersionConflict
}
