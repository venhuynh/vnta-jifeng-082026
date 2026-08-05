namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Commands;

/// <summary>Transactional command use case for creating/updating a period from its preceding period.</summary>
internal sealed class DatabasePayrollAllowanceSummaryPreviousMonthSyncService(PayrollAllowanceSummaryPersistence persistence)
    : IPayrollAllowanceSummaryPreviousMonthSyncService
{
    public Task<SyncPayrollAllowanceSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncPayrollAllowanceSummaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.SyncFromPreviousMonthAsync(request, cancellationToken);
}
