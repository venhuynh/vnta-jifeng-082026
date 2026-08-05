namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Queries;

/// <summary>Read service for the summary grid and export consumers.</summary>
internal sealed class DatabasePayrollAllowanceSummaryReadService(PayrollAllowanceSummaryPersistence persistence)
    : IPayrollAllowanceSummaryReadService
{
    public Task<PayrollAllowanceSummaryOverviewDto> GetSummaryAsync(
        PayrollAllowanceSummaryFilter filter,
        CancellationToken cancellationToken = default) =>
        persistence.GetSummaryAsync(filter, cancellationToken);

    public Task<PayrollAllowanceSummaryPageDto> SearchAsync(
        PayrollAllowanceSummaryFilter filter,
        CancellationToken cancellationToken = default) =>
        persistence.SearchAsync(filter, cancellationToken);
}
