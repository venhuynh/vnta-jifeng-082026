namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Commands;

/// <summary>Delete command use case; version and lock checks are enforced before dependent rows are removed.</summary>
internal sealed class DatabasePayrollAllowanceSummaryDeletionService(PayrollAllowanceSummaryPersistence persistence)
    : IPayrollAllowanceSummaryDeletionService
{
    public Task DeleteAsync(DeletePayrollAllowanceSummariesRequest request, CancellationToken cancellationToken = default) =>
        persistence.DeleteAsync(request, cancellationToken);
}
