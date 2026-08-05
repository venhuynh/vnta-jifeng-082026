using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapChuyenCan;

public sealed class AttendanceAllowanceWorkdayMetricPolicyTests
{
    private readonly AttendanceAllowanceWorkdayMetricPolicy policy = new();

    [Fact]
    public void Calculate_counts_only_eligible_workdays_and_rounds_late_early_deduction()
    {
        var result = policy.Calculate(
        [
            new AttendanceAllowanceWorkdayInput(120, 0, "CC", AttendanceAllowanceWorkdayEligibility.Eligible),
            new AttendanceAllowanceWorkdayInput(0, 120, "HC", AttendanceAllowanceWorkdayEligibility.NotEligible)
        ]);

        Assert.Equal(1m, result.AdministrativeWorkdayCount);
        Assert.Equal(240, result.LateEarlyMinutes);
        Assert.Equal(0.5m, result.LateEarlyDeductionDays);
        Assert.Equal(0.5m, result.AttendanceWorkdayCount);
    }

    [Fact]
    public void Calculate_detects_kp_without_requiring_the_status_to_be_eligible()
    {
        var result = policy.Calculate(
        [
            new AttendanceAllowanceWorkdayInput(0, 0, " kp ", AttendanceAllowanceWorkdayEligibility.NotEligible)
        ]);

        Assert.Equal(AttendanceAllowanceKpViolationState.Present, result.KpViolationState);
    }

    [Fact]
    public void Calculate_ignores_negative_minutes_and_never_returns_negative_attendance_workdays()
    {
        var result = policy.Calculate(
        [
            new AttendanceAllowanceWorkdayInput(-10, -20, null, AttendanceAllowanceWorkdayEligibility.Eligible),
            new AttendanceAllowanceWorkdayInput(960, 0, null, AttendanceAllowanceWorkdayEligibility.Eligible)
        ]);

        Assert.Equal(960, result.LateEarlyMinutes);
        Assert.Equal(2m, result.LateEarlyDeductionDays);
        Assert.Equal(0m, result.AttendanceWorkdayCount);
    }
}
