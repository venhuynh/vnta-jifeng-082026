using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapPhepLe.Policies;

public sealed class LeaveHolidayAllowanceCalculationPolicyTests
{
    [Theory]
    [InlineData(100000, 1, 2, 100000, 1, 2, 300000)]
    [InlineData(0, 2, 3, 0, 2, 3, 0)]
    [InlineData(100000, 0, 0, 100000, 0, 0, 0)]
    public void Calculate_returns_normalized_source_values_and_total(
        decimal dailyWageAmount,
        decimal leaveDayCount,
        decimal holidayDayCount,
        decimal expectedDailyWageAmount,
        decimal expectedLeaveDayCount,
        decimal expectedHolidayDayCount,
        decimal expectedAmount)
    {
        var result = LeaveHolidayAllowanceCalculationPolicy.Calculate(
            new LeaveHolidayAllowanceCalculationInput(
                dailyWageAmount,
                leaveDayCount,
                holidayDayCount));

        Assert.Equal(expectedDailyWageAmount, result.DailyWageAmount);
        Assert.Equal(expectedLeaveDayCount, result.LeaveDayCount);
        Assert.Equal(expectedHolidayDayCount, result.HolidayDayCount);
        Assert.Equal(expectedAmount, result.AllowanceAmount);
    }

    [Fact]
    public void Calculate_rounds_each_persisted_input_before_calculating_the_total()
    {
        var result = LeaveHolidayAllowanceCalculationPolicy.Calculate(
            new LeaveHolidayAllowanceCalculationInput(100000.125m, 1.005m, 0m));

        Assert.Equal(100000.13m, result.DailyWageAmount);
        Assert.Equal(1.01m, result.LeaveDayCount);
        Assert.Equal(101000.13m, result.AllowanceAmount);
    }

    [Fact]
    public void Calculate_uses_away_from_zero_rounding_for_a_negative_midpoint_without_deciding_its_validity()
    {
        var result = LeaveHolidayAllowanceCalculationPolicy.Calculate(
            new LeaveHolidayAllowanceCalculationInput(-100000.125m, 1m, 0m));

        Assert.Equal(-100000.13m, result.DailyWageAmount);
        Assert.Equal(-100000.13m, result.AllowanceAmount);
    }

    [Fact]
    public void Preview_policy_characterizes_the_existing_client_formula_without_per_input_normalization()
    {
        var serverAmount = LeaveHolidayAllowanceCalculationPolicy.Calculate(
            new LeaveHolidayAllowanceCalculationInput(100000m, 1m, 1.005m)).AllowanceAmount;
        var previewAmount = LeaveHolidayAllowancePreviewPolicy.Calculate(
            new LeaveHolidayAllowancePreviewCalculationInput(100000m, 1m, 1.005m)).AllowanceAmount;

        Assert.Equal(201000m, serverAmount);
        Assert.Equal(200500m, previewAmount);
    }
}

public sealed class LeaveHolidayAllowanceManualAdjustmentPolicyTests
{
    [Theory]
    [InlineData(true, 100000, 1, 100000, 1, 1, LeaveHolidayAllowanceManualAdjustmentDecision.AllowanceRecordLocked)]
    [InlineData(false, 100000, 1, -1, 1, 1, LeaveHolidayAllowanceManualAdjustmentDecision.NegativeDailyWageAmount)]
    [InlineData(false, 100000, 1, 100000, -1, 1, LeaveHolidayAllowanceManualAdjustmentDecision.NegativeLeaveDayCount)]
    [InlineData(false, 100000, 1, 100000, 1, -1, LeaveHolidayAllowanceManualAdjustmentDecision.NegativeHolidayDayCount)]
    [InlineData(false, 100000, 1, 100000.004, 1.004, 2, LeaveHolidayAllowanceManualAdjustmentDecision.Allowed)]
    [InlineData(false, 100000, 1, 100000.005, 1, 2, LeaveHolidayAllowanceManualAdjustmentDecision.CalculatedSourceValuesChanged)]
    public void Evaluate_preserves_lock_negative_and_server_calculated_value_rules(
        bool isAllowanceRecordLocked,
        decimal calculatedDailyWageAmount,
        decimal calculatedLeaveDayCount,
        decimal submittedDailyWageAmount,
        decimal submittedLeaveDayCount,
        decimal submittedHolidayDayCount,
        LeaveHolidayAllowanceManualAdjustmentDecision expected)
    {
        var decision = LeaveHolidayAllowanceManualAdjustmentPolicy.Evaluate(
            new LeaveHolidayAllowanceManualAdjustmentInput(
                isAllowanceRecordLocked,
                calculatedDailyWageAmount,
                calculatedLeaveDayCount,
                submittedDailyWageAmount,
                submittedLeaveDayCount,
                submittedHolidayDayCount));

        Assert.Equal(expected, decision);
    }
}
