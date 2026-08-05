using Vnta.Hrm.Application.PhuCap.PhuCapThamNien;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapThamNien.Policies;

public sealed class PayrollEmployeeSeniorityAllowanceTenureCalculatorTests
{
    private readonly PayrollEmployeeSeniorityAllowanceTenureCalculator calculator = new();

    [Theory]
    [InlineData(2023, 7, 31, 2026, 7, 30, 2, 11)]
    [InlineData(2023, 7, 31, 2026, 7, 31, 3, 0)]
    [InlineData(2026, 8, 1, 2026, 7, 31, 0, 0)]
    public void Calculate_preserves_completed_calendar_month_semantics(
        int startYear,
        int startMonth,
        int startDay,
        int endYear,
        int endMonth,
        int endDay,
        short expectedYears,
        short expectedMonths)
    {
        var result = calculator.Calculate(new(
            new DateOnly(startYear, startMonth, startDay),
            new DateOnly(endYear, endMonth, endDay)));

        Assert.Equal(expectedYears, result.CompletedYears);
        Assert.Equal(expectedMonths, result.CompletedMonths);
    }
}
