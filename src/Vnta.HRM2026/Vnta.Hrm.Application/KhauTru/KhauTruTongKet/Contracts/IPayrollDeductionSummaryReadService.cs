namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

/// <summary>Paginated read capability for the deduction summary feature.</summary>
public interface IPayrollDeductionSummaryReadService
{
    Task<PayrollDeductionSummaryPageDto> SearchAsync(
        PayrollDeductionSummaryFilter filter,
        CancellationToken cancellationToken = default);

}
