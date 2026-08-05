namespace Vnta.Hrm.Application.KhauTru.KhauTruTamUng;

public interface IPayrollAdvanceDeductionReadService
{
    Task<PayrollAdvanceDeductionPageDto> SearchAsync(
        PayrollAdvanceDeductionFilter filter,
        CancellationToken cancellationToken = default);
}
