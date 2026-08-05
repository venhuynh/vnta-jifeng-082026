using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapDocHai.Policies;

public sealed class HazardAllowanceSupportingPoliciesTests
{
    [Fact]
    public void Workday_metrics_count_eligible_codes_but_deduct_late_early_from_entire_month()
    {
        var calculator = new HazardAllowanceWorkdayMetricsCalculator();

        var result = calculator.Calculate(
        [
            new HazardAllowanceWorkday(" Xưởng ", 0m, 0m, true),
            new HazardAllowanceWorkday("Xưởng", 30m, 15m, false),
            new HazardAllowanceWorkday(null, -5m, -10m, false)
        ]);

        Assert.Equal("Xưởng", result.DepartmentPath);
        Assert.Equal(1m, result.QualifiedWorkdayCount);
        Assert.Equal(0.0938m, result.LateEarlyDeductionDays);
    }

    [Fact]
    public void Manual_adjustment_rounds_workdays_and_vnd_away_from_zero()
    {
        var policy = new HazardAllowanceManualAdjustmentPolicy();

        var result = policy.ValidateAndNormalize(new HazardAllowanceManualAdjustmentInput(
            QualifiedWorkdayCount: 20.125m,
            LateEarlyDeductionDays: 0.06255m,
            HazardAllowancePerDay: 7_700.5m,
            HazardAllowanceAmount: 15_400.5m,
            IsEligibleDepartment: true,
            ExclusionReason: "obsolete"));

        Assert.Equal(20.13m, result.QualifiedWorkdayCount);
        Assert.Equal(0.0626m, result.LateEarlyDeductionDays);
        Assert.Equal(20.0674m, result.PayableWorkdayCount);
        Assert.Equal(7_701m, result.HazardAllowancePerDay);
        Assert.Equal(15_401m, result.HazardAllowanceAmount);
        Assert.Null(result.ExclusionReason);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(1, -0.1, 0, 0)]
    [InlineData(1, 0, -1, 0)]
    [InlineData(1, 0, 0, -1)]
    [InlineData(1, 1.1, 0, 0)]
    public void Manual_adjustment_rejects_negative_or_inconsistent_workdays(
        decimal qualifiedWorkdays,
        decimal deductionDays,
        decimal perDay,
        decimal amount)
    {
        var policy = new HazardAllowanceManualAdjustmentPolicy();

        Assert.Throws<InvalidOperationException>(() => policy.ValidateAndNormalize(
            new HazardAllowanceManualAdjustmentInput(
                qualifiedWorkdays,
                deductionDays,
                perDay,
                amount,
                true,
                null)));
    }

    [Fact]
    public void Manual_adjustment_requires_zero_amount_and_reason_for_ineligible_department()
    {
        var policy = new HazardAllowanceManualAdjustmentPolicy();

        Assert.Throws<InvalidOperationException>(() => policy.ValidateAndNormalize(
            new HazardAllowanceManualAdjustmentInput(1m, 0m, 0m, 1m, false, "Không thuộc diện")));
        Assert.Throws<InvalidOperationException>(() => policy.ValidateAndNormalize(
            new HazardAllowanceManualAdjustmentInput(1m, 0m, 0m, 0m, false, null)));
    }

    [Theory]
    [InlineData(HazardAllowanceRowLockState.Open, HazardAllowanceRowLockState.Open, false)]
    [InlineData(HazardAllowanceRowLockState.Open, HazardAllowanceRowLockState.Locked, true)]
    [InlineData(HazardAllowanceRowLockState.Locked, HazardAllowanceRowLockState.Open, true)]
    [InlineData(HazardAllowanceRowLockState.Locked, HazardAllowanceRowLockState.Locked, false)]
    public void Lock_state_policy_is_idempotent(
        HazardAllowanceRowLockState currentState,
        HazardAllowanceRowLockState requestedState,
        bool shouldUpdate)
    {
        var policy = new HazardAllowanceLockStatePolicy();

        Assert.Equal(shouldUpdate, policy.ShouldUpdate(currentState, requestedState));
    }
}
