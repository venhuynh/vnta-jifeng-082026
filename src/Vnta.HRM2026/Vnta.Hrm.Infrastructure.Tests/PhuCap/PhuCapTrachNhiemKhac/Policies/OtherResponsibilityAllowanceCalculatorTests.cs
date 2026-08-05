using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTrachNhiemKhac.Policies;

public sealed class OtherResponsibilityAllowanceCalculatorTests
{
    private readonly IOtherResponsibilityAllowanceCalculator calculator =
        new OtherResponsibilityAllowanceCalculator();

    [Fact]
    public void Returns_full_amount_when_missing_workdays_are_at_most_one()
    {
        var result = calculator.Calculate(new(1_000_000m, 26m, 25m));

        Assert.Equal(1_000_000m, result.ActualResponsibilityAllowanceAmount);
    }

    [Fact]
    public void Prorates_amount_when_more_than_one_workday_is_missing()
    {
        var result = calculator.Calculate(new(1_000_000m, 26m, 20m));

        Assert.Equal(769_230.77m, result.ActualResponsibilityAllowanceAmount);
    }

    [Fact]
    public void Returns_full_amount_at_the_one_missing_workday_boundary()
    {
        var result = calculator.Calculate(new(100m, 3m, 2m));

        Assert.Equal(100m, result.ActualResponsibilityAllowanceAmount);
    }

    [Fact]
    public void Prorates_when_missing_workdays_are_just_above_the_one_day_boundary()
    {
        var result = calculator.Calculate(new(100m, 3m, 1.9999m));

        Assert.Equal(66.66m, result.ActualResponsibilityAllowanceAmount);
    }

    [Theory]
    [InlineData(0, 26, 20)]
    [InlineData(-1, 26, 20)]
    [InlineData(100, 0, 20)]
    [InlineData(100, -1, 20)]
    public void Returns_zero_when_standard_inputs_are_not_positive(
        decimal standardAllowance,
        decimal standardWorkdays,
        decimal calculationWorkdays)
    {
        var result = calculator.Calculate(new(standardAllowance, standardWorkdays, calculationWorkdays));

        Assert.Equal(0m, result.ActualResponsibilityAllowanceAmount);
    }

    [Fact]
    public void Preserves_negative_calculation_workdays_without_silently_changing_legacy_semantics()
    {
        var result = calculator.Calculate(new(100m, 26m, -1m));

        Assert.Equal(-3.85m, result.ActualResponsibilityAllowanceAmount);
    }

    [Fact]
    public void Rounds_prorated_currency_away_from_zero_to_two_decimal_places()
    {
        var result = calculator.Calculate(new(1m, 8m, 1m));

        Assert.Equal(0.13m, result.ActualResponsibilityAllowanceAmount);
    }

    [Fact]
    public void Rejects_a_null_calculation_input()
    {
        Assert.Throws<ArgumentNullException>(() => calculator.Calculate(null!));
    }
}
