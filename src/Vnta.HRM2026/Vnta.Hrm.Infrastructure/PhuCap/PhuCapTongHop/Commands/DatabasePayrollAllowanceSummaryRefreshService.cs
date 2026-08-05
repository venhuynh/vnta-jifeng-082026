namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Commands;

/// <summary>Command use case that recalculates summary snapshots from their source allowance details.</summary>
internal sealed class DatabasePayrollAllowanceSummaryRefreshService(PayrollAllowanceSummaryPersistence persistence)
    : IPayrollAllowanceSummaryRefreshService
{
    public Task<RefreshPayrollAllowanceSummaryResult> RefreshAsync(
        RefreshPayrollAllowanceSummaryRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.RefreshAsync(request, cancellationToken);
}
