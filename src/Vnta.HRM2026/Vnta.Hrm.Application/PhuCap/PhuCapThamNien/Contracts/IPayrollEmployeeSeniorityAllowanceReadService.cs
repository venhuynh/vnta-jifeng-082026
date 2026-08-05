namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>Reads seniority-allowance records without changing payroll state.</summary>
public interface IPayrollEmployeeSeniorityAllowanceReadService
{
    Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceListItemDto>> SearchAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default);

    Task<PayrollEmployeeSeniorityAllowancePageDto> SearchPageAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default);
}
