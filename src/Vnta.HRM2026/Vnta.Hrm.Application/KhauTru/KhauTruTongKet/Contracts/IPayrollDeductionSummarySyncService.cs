namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

/// <summary>Synchronizes a deduction-summary period from the previous month.</summary>
public interface IPayrollDeductionSummarySyncService
{
    Task<SyncPayrollDeductionSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncPayrollDeductionSummaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default);
}
