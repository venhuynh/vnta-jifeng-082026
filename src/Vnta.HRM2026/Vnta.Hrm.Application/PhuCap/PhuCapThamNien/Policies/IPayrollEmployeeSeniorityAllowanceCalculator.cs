namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>Calculates a seniority allowance from server-owned payroll inputs.</summary>
public interface IPayrollEmployeeSeniorityAllowanceCalculator
{
    PayrollEmployeeSeniorityAllowanceCalculation Calculate(
        PayrollEmployeeSeniorityAllowanceCalculationInput input);
}

/// <summary>Server-owned facts needed to select a seniority allowance tier.</summary>
public sealed record PayrollEmployeeSeniorityAllowanceCalculationInput(
    short CompletedSeniorityYears,
    decimal SalaryWorkDays,
    string? PositionName = null);

/// <summary>Stable rules persisted with the calculated seniority allowance snapshot.</summary>
public enum PayrollEmployeeSeniorityAllowanceRule
{
    NoAllowance = 0,
    SalaryWorkDaysAtOrBelowMinimum = 1,
    OneToUnderThreeYears = 2,
    ThreeToUnderSixYears = 3,
    SixToUnderTenYears = 4,
    TenToUnderThirteenYears = 5,
    ThirteenYearsOrMore = 6,
    TemporaryPosition = 7
}

public sealed record PayrollEmployeeSeniorityAllowanceCalculation(
    PayrollEmployeeSeniorityAllowanceRule AppliedRule,
    decimal AllowanceAmount);
