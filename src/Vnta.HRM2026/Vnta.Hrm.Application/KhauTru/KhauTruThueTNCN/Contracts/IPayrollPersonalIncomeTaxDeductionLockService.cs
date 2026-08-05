namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

public interface IPayrollPersonalIncomeTaxDeductionLockService
{
    Task<SetPayrollPersonalIncomeTaxDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default);
}
