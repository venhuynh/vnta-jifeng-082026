namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>Calculates completed seniority as at the supplied payroll period end date.</summary>
public interface IPayrollEmployeeSeniorityAllowanceTenureCalculator
{
    PayrollEmployeeSeniorityAllowanceTenure Calculate(
        PayrollEmployeeSeniorityAllowanceTenureInput input);
}

public sealed record PayrollEmployeeSeniorityAllowanceTenureInput(
    DateOnly EmploymentStartDate,
    DateOnly PayrollPeriodEndDate);

public sealed record PayrollEmployeeSeniorityAllowanceTenure(
    short CompletedYears,
    short CompletedMonths);

public sealed class PayrollEmployeeSeniorityAllowanceTenureCalculator
    : IPayrollEmployeeSeniorityAllowanceTenureCalculator
{
    public PayrollEmployeeSeniorityAllowanceTenure Calculate(
        PayrollEmployeeSeniorityAllowanceTenureInput input)
    {
        var totalMonths =
            ((input.PayrollPeriodEndDate.Year - input.EmploymentStartDate.Year) * 12)
            + input.PayrollPeriodEndDate.Month
            - input.EmploymentStartDate.Month;

        if (input.PayrollPeriodEndDate.Day < input.EmploymentStartDate.Day)
        {
            totalMonths--;
        }

        if (totalMonths < 0)
        {
            return new(0, 0);
        }

        return new((short)(totalMonths / 12), (short)(totalMonths % 12));
    }
}
