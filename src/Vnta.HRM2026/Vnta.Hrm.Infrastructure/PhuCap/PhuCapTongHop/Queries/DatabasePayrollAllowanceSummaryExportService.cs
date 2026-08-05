namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Queries;

/// <summary>Read-side export use case; audit operation capture remains part of the export contract.</summary>
internal sealed class DatabasePayrollAllowanceSummaryExportService(PayrollAllowanceSummaryPersistence persistence)
    : IPayrollAllowanceSummaryExportService
{
    public Task<IReadOnlyList<PayrollAllowanceSummaryExportRowDto>> ExportAsync(
        PayrollAllowanceSummaryExportRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.ExportAsync(request, cancellationToken);
}
