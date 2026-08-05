namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Commands;

/// <summary>Manual adjustment command use case for a single allowance-summary snapshot.</summary>
internal sealed class DatabasePayrollAllowanceSummaryManualAdjustmentService(PayrollAllowanceSummaryPersistence persistence)
    : IPayrollAllowanceSummaryManualAdjustmentService
{
    public Task<PayrollAllowanceSummaryListItemDto> UpdateManualValuesAsync(
        UpdatePayrollAllowanceSummaryManualNoteRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.UpdateManualValuesAsync(request, cancellationToken);

    public Task<PayrollAllowanceSummaryListItemDto> UpdateManualValuesAsync(
        UpdatePayrollAllowanceSummaryManualValuesRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.UpdateManualValuesAsync(request, cancellationToken);
}
