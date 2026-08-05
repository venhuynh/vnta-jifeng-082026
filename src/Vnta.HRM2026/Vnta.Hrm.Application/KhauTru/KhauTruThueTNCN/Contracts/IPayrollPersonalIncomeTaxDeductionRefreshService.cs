namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

public interface IPayrollPersonalIncomeTaxDeductionRefreshService
{
    Task<RefreshPayrollPersonalIncomeTaxDeductionResult> RefreshAsync(
        RefreshPayrollPersonalIncomeTaxDeductionRequest request,
        CancellationToken cancellationToken = default);
}
