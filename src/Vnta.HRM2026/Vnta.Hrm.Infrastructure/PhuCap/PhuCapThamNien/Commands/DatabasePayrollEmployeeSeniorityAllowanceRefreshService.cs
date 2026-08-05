using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Persistence;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Commands;

public sealed class DatabasePayrollEmployeeSeniorityAllowanceRefreshService(SeniorityAllowancePeriodWriter writer)
    : IPayrollEmployeeSeniorityAllowanceRefreshService
{
    public Task<RefreshPayrollEmployeeSeniorityAllowanceResult> RefreshAsync(
        RefreshPayrollEmployeeSeniorityAllowanceRequest request,
        CancellationToken cancellationToken = default) =>
        writer.RefreshAsync(request, cancellationToken);
}
