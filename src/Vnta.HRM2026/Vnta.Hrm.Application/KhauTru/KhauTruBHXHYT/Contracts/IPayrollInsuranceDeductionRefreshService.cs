namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public interface IPayrollInsuranceDeductionRefreshService
{
    Task<RefreshPayrollInsuranceDeductionResult> RefreshAsync(
        RefreshPayrollInsuranceDeductionRequest request,
        CancellationToken cancellationToken = default);
}
