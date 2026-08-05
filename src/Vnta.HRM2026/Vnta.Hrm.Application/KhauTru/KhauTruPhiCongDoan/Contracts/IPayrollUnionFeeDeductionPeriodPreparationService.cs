namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public interface IPayrollUnionFeeDeductionPeriodPreparationService
{
    Task<PreparePayrollUnionFeeDeductionPeriodResult> PreparePeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
