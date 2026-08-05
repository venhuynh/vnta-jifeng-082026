using Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapCom;

public sealed class MealAllowancePolicyTests
{
    [Theory]
    [InlineData(120)]
    [InlineData(150)]
    public void Calculate_counts_each_regular_production_workday_with_overtime_inclusive_range(int overtimeMinutes)
    {
        var result = MealAllowancePolicy.Calculate(new MealAllowanceCalculationInput(
            [Workday(overtimeMinutes), Workday(overtimeMinutes)],
            MealAllowancePolicy.DefaultMealAllowancePerQualifiedDay));

        Assert.Equal(2, result.QualifiedMealDays);
        Assert.Equal(2, result.Overtime1900Days);
        Assert.Equal(36_000m, result.MealAllowanceAmount);
    }

    [Theory]
    [InlineData(119)]
    [InlineData(151)]
    [InlineData(-1)]
    public void EvaluateWorkday_rejects_overtime_outside_inclusive_range(int overtimeMinutes) =>
        Assert.Equal(
            MealAllowanceWorkdayEligibility.OvertimeMinutesOutsideQualifyingRange,
            MealAllowancePolicy.EvaluateWorkday(Workday(overtimeMinutes)));

    [Fact]
    public void EvaluateWorkday_rejects_non_regular_and_non_production_inputs()
    {
        Assert.Equal(
            MealAllowanceWorkdayEligibility.NotRegularWorkday,
            MealAllowancePolicy.EvaluateWorkday(Workday(120, workdayType: "Ngày nghỉ")));
        Assert.Equal(
            MealAllowanceWorkdayEligibility.NotProductionShift,
            MealAllowancePolicy.EvaluateWorkday(Workday(120, shift: new MealAllowanceShift("HC", "Hành chính", null))));
    }

    [Fact]
    public void Calculate_recognizes_existing_vietnamese_workday_and_production_shift_conventions()
    {
        var result = MealAllowancePolicy.Calculate(new MealAllowanceCalculationInput(
            [Workday(135, "Ngày thường", new MealAllowanceShift(null, "Ca sản xuất", null))],
            MealAllowancePolicy.DefaultMealAllowancePerQualifiedDay));

        Assert.Equal(1, result.QualifiedMealDays);
        Assert.Equal(18_000m, result.MealAllowanceAmount);
    }

    [Fact]
    public async Task RefreshCalculator_returns_only_employees_with_qualified_workdays()
    {
        var qualifyingEmployeeId = Guid.NewGuid();
        var excludedEmployeeId = Guid.NewGuid();
        var calculator = new MealAllowanceRefreshCalculator(new StubWorkdaySource(
        [
            new MealAllowanceEmployeeWorkday(qualifyingEmployeeId, Workday(120)),
            new MealAllowanceEmployeeWorkday(qualifyingEmployeeId, Workday(151)),
            new MealAllowanceEmployeeWorkday(excludedEmployeeId, Workday(119))
        ]));

        var result = await calculator.CalculateAsync(new MealAllowanceRefreshPeriod(7, 2026, null));

        var employeeResult = Assert.Single(result);
        Assert.Equal(qualifyingEmployeeId, employeeResult.Key);
        Assert.Equal(1, employeeResult.Value.QualifiedMealDays);
        Assert.Equal(18_000m, employeeResult.Value.MealAllowanceAmount);
    }

    [Theory]
    [InlineData(2, 18000.005, 36000.01)]
    [InlineData(-1, 18000, 0)]
    [InlineData(2, -1, 0)]
    public void CalculateAllowanceAmount_normalizes_negative_values_and_rounds_away_from_zero(
        int allowanceDayCount,
        decimal unitPrice,
        decimal expected) =>
        Assert.Equal(
            expected,
            MealAllowancePolicy.CalculateAllowanceAmount(new MealAllowanceAmountInput(allowanceDayCount, unitPrice)));

    private static MealAllowanceWorkday Workday(
        int overtimeMinutes,
        string workdayType = "regular",
        MealAllowanceShift? shift = null) =>
        new(workdayType, shift ?? new MealAllowanceShift("SX-A", "Ca sản xuất", "SX"), overtimeMinutes);

    private sealed class StubWorkdaySource(IReadOnlyList<MealAllowanceEmployeeWorkday> rows)
        : IMealAllowanceWorkdaySource
    {
        public Task<IReadOnlyList<MealAllowanceEmployeeWorkday>> LoadAsync(
            MealAllowanceRefreshPeriod period,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(rows);
    }
}
