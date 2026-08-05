namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

public interface IPayrollPersonalIncomeTaxDeductionReadService
{
    Task<PayrollPersonalIncomeTaxDeductionPageDto> SearchAsync(
        PayrollPersonalIncomeTaxDeductionFilter filter,
        CancellationToken cancellationToken = default);
}
