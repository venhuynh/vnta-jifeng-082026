namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public interface IPayrollUnionFeeDeductionReadService
{
    Task<PayrollUnionFeeDeductionPageDto> SearchAsync(
        PayrollUnionFeeDeductionFilter filter,
        CancellationToken cancellationToken = default);
}
