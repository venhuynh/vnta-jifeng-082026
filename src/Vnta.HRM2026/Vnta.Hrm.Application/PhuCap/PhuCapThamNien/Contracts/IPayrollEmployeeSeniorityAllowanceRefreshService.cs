namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public interface IPayrollEmployeeSeniorityAllowanceRefreshService
{
    Task<RefreshPayrollEmployeeSeniorityAllowanceResult> RefreshAsync(
        RefreshPayrollEmployeeSeniorityAllowanceRequest request,
        CancellationToken cancellationToken = default);
}
