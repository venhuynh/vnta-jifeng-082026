namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>Read-only capability for the insurance deduction feature.</summary>
public interface IPayrollInsuranceDeductionReadService
{
    Task<PayrollInsuranceDeductionPageDto> SearchAsync(
        PayrollInsuranceDeductionFilter filter,
        CancellationToken cancellationToken = default);
}
