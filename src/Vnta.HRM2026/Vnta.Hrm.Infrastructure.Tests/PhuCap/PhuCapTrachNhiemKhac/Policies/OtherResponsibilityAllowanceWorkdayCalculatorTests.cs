using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTrachNhiemKhac.Policies;

public sealed class OtherResponsibilityAllowanceWorkdayCalculatorTests
{
    private readonly IOtherResponsibilityAllowanceWorkdayCalculator calculator =
        new OtherResponsibilityAllowanceWorkdayCalculator();

    [Fact]
    public void Calculates_eligible_workdays_after_late_and_early_leave_adjustments()
    {
        var result = calculator.Calculate(
        [
            Entry(new DateOnly(2026, 7, 1), OtherResponsibilityAllowanceWorkdayEligibility.EligibleAdministrativeWorkday, 60m, 60m),
            Entry(new DateOnly(2026, 7, 2), OtherResponsibilityAllowanceWorkdayEligibility.EligibleAdministrativeWorkday, 0m, 0m)
        ]);

        Assert.Equal(1.75m, result.AllowanceCalculationWorkdayCount);
    }

    [Fact]
    public void Counts_at_most_one_eligible_workday_per_date_and_caps_adjustments_at_one_day()
    {
        var result = calculator.Calculate(
        [
            Entry(new DateOnly(2026, 7, 1), OtherResponsibilityAllowanceWorkdayEligibility.EligibleAdministrativeWorkday, 240m, 0m),
            Entry(new DateOnly(2026, 7, 1), OtherResponsibilityAllowanceWorkdayEligibility.EligibleAdministrativeWorkday, 240m, 0m),
            Entry(new DateOnly(2026, 7, 2), OtherResponsibilityAllowanceWorkdayEligibility.EligibleAdministrativeWorkday, 999m, 0m)
        ]);

        Assert.Equal(0m, result.AllowanceCalculationWorkdayCount);
    }

    [Fact]
    public void Excludes_dates_without_an_eligible_administrative_workday()
    {
        var result = calculator.Calculate(
        [
            Entry(new DateOnly(2026, 7, 1), OtherResponsibilityAllowanceWorkdayEligibility.NotEligible, 0m, 0m)
        ]);

        Assert.Equal(0m, result.AllowanceCalculationWorkdayCount);
    }

    [Fact]
    public void Ignores_negative_late_or_early_leave_minutes_and_rounds_workdays_to_four_decimal_places()
    {
        var result = calculator.Calculate(
        [
            Entry(new DateOnly(2026, 7, 1), OtherResponsibilityAllowanceWorkdayEligibility.EligibleAdministrativeWorkday, -30m, 1m)
        ]);

        Assert.Equal(0.9979m, result.AllowanceCalculationWorkdayCount);
    }

    [Fact]
    public void Rejects_null_attendance_entries()
    {
        Assert.Throws<ArgumentNullException>(() => calculator.Calculate(null!));
    }

    private static OtherResponsibilityAllowanceAttendanceEntry Entry(
        DateOnly workDate,
        OtherResponsibilityAllowanceWorkdayEligibility eligibility,
        decimal lateMinutes,
        decimal earlyLeaveMinutes) =>
        new(workDate, eligibility, lateMinutes, earlyLeaveMinutes);
}
