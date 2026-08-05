namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

public interface IPayrollPersonalIncomeTaxDeductionManualAdjustmentService
{
    Task<PayrollPersonalIncomeTaxDeductionListItemDto> UpdateManualValueAsync(
        UpdatePayrollPersonalIncomeTaxDeductionManualValueRequest request,
        CancellationToken cancellationToken = default);
}
