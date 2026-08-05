namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>Loads attendance facts needed by the seniority allowance workday policy.</summary>
public interface IPayrollEmployeeSeniorityAllowanceWorkdaySource
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<PayrollEmployeeSeniorityAllowanceWorkdayInput>>> LoadAsync(
        PayrollEmployeeSeniorityAllowanceWorkdaySourceQuery query,
        CancellationToken cancellationToken = default);
}

public sealed record PayrollEmployeeSeniorityAllowanceWorkdaySourceQuery(
    int PayrollYear,
    int PayrollMonth,
    IReadOnlyCollection<Guid> EmployeeIds);
