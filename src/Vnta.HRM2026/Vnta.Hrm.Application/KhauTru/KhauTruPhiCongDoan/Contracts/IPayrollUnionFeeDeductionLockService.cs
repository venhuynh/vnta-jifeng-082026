namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public interface IPayrollUnionFeeDeductionLockService
{
    Task<PayrollUnionFeeDeductionListItemDto> SetLockStateAsync(
        SetPayrollUnionFeeDeductionLockStateRequest request,
        CancellationToken cancellationToken = default);

    Task<SetPayrollUnionFeeDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollUnionFeeDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default);
}
