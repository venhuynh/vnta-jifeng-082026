namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public interface IPayrollUnionFeeDeductionManualAdjustmentService
{
    Task<PayrollUnionFeeDeductionListItemDto> UpdateManualValueAsync(
        UpdatePayrollUnionFeeDeductionManualValueRequest request,
        CancellationToken cancellationToken = default);
}
