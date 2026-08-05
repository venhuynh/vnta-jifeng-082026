namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

/// <summary>Owns row and batch lock-state transitions.</summary>
public interface IPayrollDeductionSummaryLockService
{
    Task<PayrollDeductionSummaryListItemDto> SetLockStateAsync(
        SetPayrollDeductionSummaryLockStateRequest request,
        CancellationToken cancellationToken = default);

    Task<SetPayrollDeductionSummaryBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollDeductionSummaryBatchLockStateRequest request,
        CancellationToken cancellationToken = default);
}
