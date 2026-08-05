namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public interface IPayrollUnionFeeDeductionRefreshService
{
    Task<RefreshPayrollUnionFeeDeductionResult> RefreshAsync(
        RefreshPayrollUnionFeeDeductionRequest request,
        CancellationToken cancellationToken = default);
}
