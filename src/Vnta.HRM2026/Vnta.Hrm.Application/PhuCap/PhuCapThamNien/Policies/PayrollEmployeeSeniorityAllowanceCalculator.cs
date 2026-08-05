namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>Default implementation of the seniority allowance ladder.</summary>
public sealed class PayrollEmployeeSeniorityAllowanceCalculator
    : IPayrollEmployeeSeniorityAllowanceCalculator
{
    public PayrollEmployeeSeniorityAllowanceCalculation Calculate(
        PayrollEmployeeSeniorityAllowanceCalculationInput input)
    {
        if (input.CompletedSeniorityYears < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                input.CompletedSeniorityYears,
                "Completed seniority years cannot be negative.");
        }

        if (input.SalaryWorkDays < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                input.SalaryWorkDays,
                "Salary work days cannot be negative.");
        }

        if (IsTemporaryPosition(input.PositionName))
        {
            return new(PayrollEmployeeSeniorityAllowanceRule.TemporaryPosition, 0m);
        }

        if (input.SalaryWorkDays <= 5m)
        {
            return new(PayrollEmployeeSeniorityAllowanceRule.SalaryWorkDaysAtOrBelowMinimum, 0m);
        }

        return input.CompletedSeniorityYears switch
        {
            >= 13 => new(PayrollEmployeeSeniorityAllowanceRule.ThirteenYearsOrMore, 400_000m),
            >= 10 => new(PayrollEmployeeSeniorityAllowanceRule.TenToUnderThirteenYears, 350_000m),
            >= 6 => new(PayrollEmployeeSeniorityAllowanceRule.SixToUnderTenYears, 250_000m),
            >= 3 => new(PayrollEmployeeSeniorityAllowanceRule.ThreeToUnderSixYears, 200_000m),
            >= 1 => new(PayrollEmployeeSeniorityAllowanceRule.OneToUnderThreeYears, 150_000m),
            _ => new(PayrollEmployeeSeniorityAllowanceRule.NoAllowance, 0m)
        };
    }

    private static bool IsTemporaryPosition(string? positionName) =>
        !string.IsNullOrWhiteSpace(positionName)
        && positionName.Contains("Thời vụ", StringComparison.OrdinalIgnoreCase);
}
