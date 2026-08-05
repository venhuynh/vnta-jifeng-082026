namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public interface IPayrollInsuranceDeductionLockService
{
    Task<PayrollInsuranceDeductionListItemDto> SetLockStateAsync(
        SetPayrollInsuranceDeductionLockStateRequest request,
        CancellationToken cancellationToken = default);

    Task<SetPayrollInsuranceDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollInsuranceDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default);
}
