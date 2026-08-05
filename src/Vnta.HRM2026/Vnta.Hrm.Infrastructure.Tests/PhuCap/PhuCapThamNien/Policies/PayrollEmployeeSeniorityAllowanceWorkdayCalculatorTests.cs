using Vnta.Hrm.Application.PhuCap.PhuCapThamNien;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapThamNien.Policies;

public sealed class PayrollEmployeeSeniorityAllowanceWorkdayCalculatorTests
{
    private readonly PayrollEmployeeSeniorityAllowanceWorkdayCalculator calculator = new();

    [Fact]
    public void Calculate_counts_only_eligible_days_and_rounds_late_early_leave_away_from_zero()
    {
        var workdays = new PayrollEmployeeSeniorityAllowanceWorkdayInput[]
        {
            new(PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included, 3, 0),
            new(PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included, 0, 0),
            new(PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included, 0, 0),
            new(PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included, 0, 0),
            new(PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included, 0, 0),
            new(PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included, 0, 0),
            new(PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Excluded, 0, 0)
        };

        var result = calculator.Calculate(workdays);

        Assert.Equal(6m, result.AdministrativeWorkDays);
        Assert.Equal(0.0063m, result.LateEarlyLeaveWorkDays);
        Assert.Equal(5.9937m, result.SalaryWorkDays);
    }

    [Fact]
    public void Calculate_clamps_negative_attendance_minutes_to_zero()
    {
        var result = calculator.Calculate(
        [
            new(PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included, -1, -1)
        ]);

        Assert.Equal(1m, result.AdministrativeWorkDays);
        Assert.Equal(0m, result.LateEarlyLeaveWorkDays);
        Assert.Equal(1m, result.SalaryWorkDays);
    }

    [Fact]
    public void Calculate_returns_zero_snapshot_for_no_attendance_facts()
    {
        var result = calculator.Calculate([]);

        Assert.Equal(PayrollEmployeeSeniorityAllowanceWorkdayCalculation.Empty, result);
    }

    [Fact]
    public void Calculate_rejects_null_workday_facts()
    {
        Assert.Throws<ArgumentNullException>(() => calculator.Calculate(null!));
    }
}
