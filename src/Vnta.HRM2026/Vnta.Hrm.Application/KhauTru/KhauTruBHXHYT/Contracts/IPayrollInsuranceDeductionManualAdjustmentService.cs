namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public interface IPayrollInsuranceDeductionManualAdjustmentService
{
    Task<PayrollInsuranceDeductionListItemDto> UpdateManualValuesAsync(
        UpdatePayrollInsuranceDeductionManualValuesRequest request,
        CancellationToken cancellationToken = default);
}
