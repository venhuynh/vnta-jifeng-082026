namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Commands;

/// <summary>Lock command use case, preserving row and batch optimistic-concurrency contracts and audit capture.</summary>
internal sealed class DatabasePayrollAllowanceSummaryLockService(PayrollAllowanceSummaryPersistence persistence)
    : IPayrollAllowanceSummaryLockService
{
    public Task<PayrollAllowanceSummaryListItemDto> SetLockStateAsync(
        SetPayrollAllowanceSummaryLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.SetLockStateAsync(request, cancellationToken);

    public Task<SetPayrollAllowanceSummaryBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollAllowanceSummaryBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.SetLockStateBatchAsync(request, cancellationToken);
}
