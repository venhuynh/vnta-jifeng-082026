using Vnta.Hrm.Application.PhuCap.PhuCapThamNien;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapThamNien.Policies;

public sealed class PayrollEmployeeSeniorityAllowanceCalculatorTests
{
    private readonly PayrollEmployeeSeniorityAllowanceCalculator calculator = new();

    [Theory]
    [InlineData(13, PayrollEmployeeSeniorityAllowanceRule.ThirteenYearsOrMore, 400000)]
    [InlineData(10, PayrollEmployeeSeniorityAllowanceRule.TenToUnderThirteenYears, 350000)]
    [InlineData(6, PayrollEmployeeSeniorityAllowanceRule.SixToUnderTenYears, 250000)]
    [InlineData(3, PayrollEmployeeSeniorityAllowanceRule.ThreeToUnderSixYears, 200000)]
    [InlineData(1, PayrollEmployeeSeniorityAllowanceRule.OneToUnderThreeYears, 150000)]
    [InlineData(0, PayrollEmployeeSeniorityAllowanceRule.NoAllowance, 0)]
    public void Calculate_applies_the_existing_seniority_ladder_above_the_workday_threshold(
        short completedSeniorityYears,
        PayrollEmployeeSeniorityAllowanceRule expectedRule,
        decimal expectedAmount)
    {
        var result = calculator.Calculate(new(completedSeniorityYears, 5.0001m));

        Assert.Equal(expectedRule, result.AppliedRule);
        Assert.Equal(expectedAmount, result.AllowanceAmount);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(0)]
    public void Calculate_prioritizes_the_existing_salary_workday_block(short salaryWorkDays)
    {
        var result = calculator.Calculate(new(20, salaryWorkDays));

        Assert.Equal(PayrollEmployeeSeniorityAllowanceRule.SalaryWorkDaysAtOrBelowMinimum, result.AppliedRule);
        Assert.Equal(0m, result.AllowanceAmount);
    }

    [Fact]
    public void Calculate_prioritizes_a_temporary_position_over_every_other_allowance_rule()
    {
        var result = calculator.Calculate(new(20, 26m, "Công nhân Thời vụ"));

        Assert.Equal(PayrollEmployeeSeniorityAllowanceRule.TemporaryPosition, result.AppliedRule);
        Assert.Equal(0m, result.AllowanceAmount);
    }

    [Fact]
    public void Calculate_rejects_negative_seniority_years()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(new(-1, 6m)));
    }

    [Fact]
    public void Calculate_rejects_negative_salary_workdays()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(new(1, -0.0001m)));
    }

    [Fact]
    public void Rule_storage_keys_preserve_the_existing_persisted_values()
    {
        Assert.Equal("temporary-position", PayrollEmployeeSeniorityAllowanceRule.TemporaryPosition.ToStorageKey());
        Assert.Equal("blocked-salary-work", PayrollEmployeeSeniorityAllowanceRule.SalaryWorkDaysAtOrBelowMinimum.ToStorageKey());
        Assert.Equal("13-plus", PayrollEmployeeSeniorityAllowanceRule.ThirteenYearsOrMore.ToStorageKey());
        Assert.True(PayrollEmployeeSeniorityAllowanceRuleExtensions.TryFromStorageKey(
            "3-6",
            out var parsedRule));
        Assert.Equal(PayrollEmployeeSeniorityAllowanceRule.ThreeToUnderSixYears, parsedRule);
    }
}
