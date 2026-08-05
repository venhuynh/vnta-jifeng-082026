using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruTongHop.Policies;

public sealed class PayrollDeductionSummaryPoliciesTests
{
    [Fact]
    public void Reconciliation_uses_all_detail_amounts_and_marks_changed_snapshot()
    {
        var result = PayrollDeductionSummaryReconciliationCalculator.Calculate(new(
            new(1m, 2m, 3m, 4m, 5m),
            new(100m, 200m, 300m, 400m, 500m)));

        Assert.Equal(new PayrollDeductionSummaryAmounts(100m, 200m, 300m, 400m, 500m), result.RecalculatedSnapshot);
        Assert.Equal(0, result.MissingDetailSourceCount);
        Assert.Equal(PayrollDeductionSummaryReconciliationStatus.SnapshotChanged, result.Status);
    }

    [Fact]
    public void Reconciliation_converts_each_missing_detail_to_zero_and_counts_it()
    {
        var result = PayrollDeductionSummaryReconciliationCalculator.Calculate(new(
            new(0m, 20m, 0m, 40m, 0m),
            new(null, 20m, null, 40m, null)));

        Assert.Equal(new PayrollDeductionSummaryAmounts(0m, 20m, 0m, 40m, 0m), result.RecalculatedSnapshot);
        Assert.Equal(3, result.MissingDetailSourceCount);
        Assert.Equal(PayrollDeductionSummaryReconciliationStatus.AlreadyReconciled, result.Status);
    }

    [Fact]
    public void Reconciliation_preserves_detail_precision_without_rounding()
    {
        var result = PayrollDeductionSummaryReconciliationCalculator.Calculate(new(
            new(0m, 0m, 0m, 0m, 0m),
            new(0.005m, 1.235m, 0m, 0m, 0m)));

        Assert.Equal(0.005m, result.RecalculatedSnapshot.SocialInsuranceDeductionAmount);
        Assert.Equal(1.235m, result.RecalculatedSnapshot.PersonalIncomeTaxDeductionAmount);
    }

    [Fact]
    public void Reconciliation_preserves_negative_detail_amounts_without_new_validation()
    {
        var result = PayrollDeductionSummaryReconciliationCalculator.Calculate(new(
            new(0m, 0m, 0m, 0m, 0m),
            new(-1m, 0m, 0m, 0m, 0m)));

        Assert.Equal(-1m, result.RecalculatedSnapshot.SocialInsuranceDeductionAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12.34)]
    public void Manual_other_deduction_accepts_non_negative_amounts_with_at_most_two_decimals(decimal amount)
    {
        PayrollDeductionSummaryManualOtherDeductionPolicy.Validate(new(amount));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(12.345)]
    public void Manual_other_deduction_rejects_negative_or_over_precision_amounts(decimal amount)
    {
        Assert.Throws<InvalidOperationException>(() =>
            PayrollDeductionSummaryManualOtherDeductionPolicy.Validate(new(amount)));
    }

    [Theory]
    [InlineData(PayrollDeductionSummarySyncTargetState.Locked, PayrollDeductionSummarySyncSourceState.Available, PayrollDeductionSummarySyncAction.PreserveLockedTarget)]
    [InlineData(PayrollDeductionSummarySyncTargetState.Absent, PayrollDeductionSummarySyncSourceState.Available, PayrollDeductionSummarySyncAction.CreateTargetFromPreviousMonth)]
    [InlineData(PayrollDeductionSummarySyncTargetState.Absent, PayrollDeductionSummarySyncSourceState.Missing, PayrollDeductionSummarySyncAction.CreateEmptyTarget)]
    [InlineData(PayrollDeductionSummarySyncTargetState.Unlocked, PayrollDeductionSummarySyncSourceState.Available, PayrollDeductionSummarySyncAction.UpdateUnlockedTargetFromPreviousMonth)]
    [InlineData(PayrollDeductionSummarySyncTargetState.Unlocked, PayrollDeductionSummarySyncSourceState.Missing, PayrollDeductionSummarySyncAction.KeepUnlockedTargetAndEnsureDetails)]
    public void Previous_month_sync_selects_the_existing_target_behavior(
        PayrollDeductionSummarySyncTargetState targetState,
        PayrollDeductionSummarySyncSourceState sourceState,
        PayrollDeductionSummarySyncAction expectedAction)
    {
        var action = PayrollDeductionSummarySyncPolicy.Decide(new(targetState, sourceState));

        Assert.Equal(expectedAction, action);
    }

    [Fact]
    public void Lock_and_concurrency_policies_keep_idempotent_and_stale_version_behavior()
    {
        var currentVersion = new DateTime(2026, 7, 31, 8, 0, 0);

        Assert.Equal(
            PayrollDeductionSummaryLockStateChangeDecision.NoChangeRequired,
            PayrollDeductionSummaryLockStatePolicy.Decide(
                PayrollDeductionSummaryLockState.Locked,
                PayrollDeductionSummaryLockState.Locked));
        Assert.Equal(
            PayrollDeductionSummaryLockStateChangeDecision.ChangeRequired,
            PayrollDeductionSummaryLockStatePolicy.Decide(
                PayrollDeductionSummaryLockState.Unlocked,
                PayrollDeductionSummaryLockState.Locked));
        Assert.Equal(
            PayrollDeductionSummaryConcurrencyStatus.VersionMatches,
            PayrollDeductionSummaryConcurrencyPolicy.Evaluate(new(currentVersion, currentVersion)));
        Assert.Equal(
            PayrollDeductionSummaryConcurrencyStatus.VersionConflict,
            PayrollDeductionSummaryConcurrencyPolicy.Evaluate(new(currentVersion, currentVersion.AddTicks(1))));
    }

    [Theory]
    [InlineData(2026, 6)]
    [InlineData(2100, 12)]
    public void Period_policy_accepts_supported_boundaries(int year, int month)
    {
        PayrollDeductionSummaryPeriodPolicy.ValidateRequired(year, month);
    }

    [Theory]
    [InlineData(2026, 5)]
    [InlineData(2025, 12)]
    [InlineData(2101, 1)]
    [InlineData(2026, 13)]
    public void Period_policy_rejects_unsupported_boundaries(int year, int month)
    {
        Assert.Throws<InvalidOperationException>(() =>
            PayrollDeductionSummaryPeriodPolicy.ValidateRequired(year, month));
    }

    [Fact]
    public void Request_validator_enforces_the_manual_other_deduction_boundary()
    {
        var validator = new PayrollDeductionSummaryRequestValidator();
        var result = validator.Validate(new UpdatePayrollDeductionSummaryManualOtherDeductionRequest(
            Guid.NewGuid(), 12.345m, null, new DateTime(2026, 7, 1)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Request_validator_accepts_versioned_selected_rows_when_both_lists_describe_the_same_scope()
    {
        var validator = new PayrollDeductionSummaryRequestValidator();
        var id = Guid.NewGuid();
        var result = validator.Validate(new SetPayrollDeductionSummaryBatchLockStateRequest(
            2026,
            7,
            true,
            [id],
            Items: [new PayrollDeductionSummaryLockItem(id, new DateTime(2026, 7, 1))]));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Request_validator_rejects_mismatched_versioned_selected_rows()
    {
        var validator = new PayrollDeductionSummaryRequestValidator();
        var result = validator.Validate(new SetPayrollDeductionSummaryBatchLockStateRequest(
            2026,
            7,
            true,
            [Guid.NewGuid()],
            Items: [new PayrollDeductionSummaryLockItem(Guid.NewGuid(), new DateTime(2026, 7, 1))]));

        Assert.False(result.IsValid);
    }
}
