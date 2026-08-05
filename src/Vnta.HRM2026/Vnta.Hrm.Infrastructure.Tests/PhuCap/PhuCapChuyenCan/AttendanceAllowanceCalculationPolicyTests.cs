using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapChuyenCan;

public sealed class AttendanceAllowanceCalculationPolicyTests
{
    private readonly AttendanceAllowanceCalculationPolicy policy = new();

    [Theory]
    [InlineData(20.9375, AttendanceAllowanceClass.A, 600000)]
    [InlineData(18.9375, AttendanceAllowanceClass.B, 300000)]
    [InlineData(18.9374, AttendanceAllowanceClass.C, 0)]
    public void Calculate_applies_current_attendance_thresholds(
        decimal attendanceWorkdayCount,
        AttendanceAllowanceClass expectedClass,
        decimal expectedAmount)
    {
        var result = policy.Calculate(new AttendanceAllowanceCalculationInput(
            StandardWorkdayCount: 22m,
            AttendanceWorkdayCount: attendanceWorkdayCount,
            MissingWorkdayCount: null,
            KpViolationState: AttendanceAllowanceKpViolationState.NotPresent));

        Assert.Equal(22m - attendanceWorkdayCount, result.MissingWorkdayCount);
        Assert.Equal(expectedClass, result.AttendanceClass);
        Assert.Equal(expectedAmount, result.ActualAllowanceAmount);
    }

    [Theory]
    [InlineData(1.0625, AttendanceAllowanceClass.A, 600000)]
    [InlineData(1.0626, AttendanceAllowanceClass.B, 300000)]
    [InlineData(3.0625, AttendanceAllowanceClass.B, 300000)]
    [InlineData(3.0626, AttendanceAllowanceClass.C, 0)]
    public void Calculate_keeps_inclusive_threshold_boundaries(
        decimal missingWorkdayCount,
        AttendanceAllowanceClass expectedClass,
        decimal expectedAmount)
    {
        var result = policy.Calculate(new AttendanceAllowanceCalculationInput(
            StandardWorkdayCount: 22m,
            AttendanceWorkdayCount: 0m,
            MissingWorkdayCount: missingWorkdayCount,
            KpViolationState: AttendanceAllowanceKpViolationState.NotPresent));

        Assert.Equal(expectedClass, result.AttendanceClass);
        Assert.Equal(expectedAmount, result.ActualAllowanceAmount);
    }

    [Fact]
    public void Calculate_prioritizes_kp_over_attendance_threshold()
    {
        var result = policy.Calculate(new AttendanceAllowanceCalculationInput(
            StandardWorkdayCount: 22m,
            AttendanceWorkdayCount: 22m,
            MissingWorkdayCount: null,
            KpViolationState: AttendanceAllowanceKpViolationState.Present));

        Assert.Equal(AttendanceAllowanceClass.C, result.AttendanceClass);
        Assert.Equal(0m, result.ActualAllowanceAmount);
        Assert.Equal(AttendanceAllowanceAppliedRule.KpOverride, result.AppliedRule);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(22, -0.5)]
    public void Calculate_keeps_current_invalid_input_fallback(
        decimal standardWorkdayCount,
        decimal attendanceWorkdayCount)
    {
        var result = policy.Calculate(new AttendanceAllowanceCalculationInput(
            standardWorkdayCount,
            attendanceWorkdayCount,
            MissingWorkdayCount: null,
            KpViolationState: AttendanceAllowanceKpViolationState.NotPresent));

        Assert.Equal(0m, result.AttendanceRate);
        Assert.Equal(
            standardWorkdayCount <= 0m
                ? null
                : Math.Max(standardWorkdayCount - attendanceWorkdayCount, 0m),
            result.MissingWorkdayCount);
        Assert.Equal(0m, result.ActualAllowanceAmount);
    }

    [Fact]
    public void Calculate_rounds_attendance_rate_away_from_zero_to_four_decimal_places()
    {
        var result = policy.Calculate(new AttendanceAllowanceCalculationInput(
            StandardWorkdayCount: 3m,
            AttendanceWorkdayCount: 1m,
            MissingWorkdayCount: null,
            KpViolationState: AttendanceAllowanceKpViolationState.NotPresent));

        Assert.Equal(0.3333m, result.AttendanceRate);
    }

    [Fact]
    public void Characterization_attendance_above_standard_clamps_missing_workdays_to_zero()
    {
        var result = policy.Calculate(new AttendanceAllowanceCalculationInput(
            StandardWorkdayCount: 22m,
            AttendanceWorkdayCount: 23m,
            MissingWorkdayCount: null,
            KpViolationState: AttendanceAllowanceKpViolationState.NotPresent));

        Assert.Equal(0m, result.MissingWorkdayCount);
        Assert.Equal(AttendanceAllowanceClass.A, result.AttendanceClass);
    }
}
