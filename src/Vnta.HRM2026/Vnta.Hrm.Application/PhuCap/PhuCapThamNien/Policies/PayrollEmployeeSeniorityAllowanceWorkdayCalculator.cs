namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>Determines the salary workday snapshot from already loaded attendance facts.</summary>
public interface IPayrollEmployeeSeniorityAllowanceWorkdayCalculator
{
    PayrollEmployeeSeniorityAllowanceWorkdayCalculation Calculate(
        IReadOnlyCollection<PayrollEmployeeSeniorityAllowanceWorkdayInput> workdays);
}

public enum PayrollEmployeeSeniorityAllowanceWorkdayEligibility
{
    Excluded = 0,
    Included = 1
}

/// <summary>One attendance fact used by the seniority allowance workday rule.</summary>
public sealed record PayrollEmployeeSeniorityAllowanceWorkdayInput(
    PayrollEmployeeSeniorityAllowanceWorkdayEligibility Eligibility,
    int LateMinutes,
    int EarlyLeaveMinutes);

/// <summary>Rounded workday values persisted with the seniority allowance snapshot.</summary>
public sealed record PayrollEmployeeSeniorityAllowanceWorkdayCalculation(
    decimal AdministrativeWorkDays,
    decimal LateEarlyLeaveWorkDays,
    decimal SalaryWorkDays)
{
    public static PayrollEmployeeSeniorityAllowanceWorkdayCalculation Empty { get; } = new(0m, 0m, 0m);
}

public sealed class PayrollEmployeeSeniorityAllowanceWorkdayCalculator
    : IPayrollEmployeeSeniorityAllowanceWorkdayCalculator
{
    private const decimal MinutesPerWorkday = 480m;

    public PayrollEmployeeSeniorityAllowanceWorkdayCalculation Calculate(
        IReadOnlyCollection<PayrollEmployeeSeniorityAllowanceWorkdayInput> workdays)
    {
        ArgumentNullException.ThrowIfNull(workdays);

        var administrativeWorkDays = workdays.Count(workday =>
            workday.Eligibility == PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included);
        var lateEarlyLeaveMinutes = workdays.Sum(workday =>
            Math.Max(workday.LateMinutes, 0) + Math.Max(workday.EarlyLeaveMinutes, 0));
        var lateEarlyLeaveWorkDays = RoundWorkDays(lateEarlyLeaveMinutes / MinutesPerWorkday);

        return new(
            RoundWorkDays(administrativeWorkDays),
            lateEarlyLeaveWorkDays,
            RoundWorkDays(administrativeWorkDays - lateEarlyLeaveWorkDays));
    }

    private static decimal RoundWorkDays(decimal value) =>
        decimal.Round(value, 4, MidpointRounding.AwayFromZero);
}
