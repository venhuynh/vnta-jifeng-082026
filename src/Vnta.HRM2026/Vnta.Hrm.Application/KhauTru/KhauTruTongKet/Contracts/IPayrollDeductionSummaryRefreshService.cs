namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

/// <summary>Refreshes one row or recalculates a deduction-summary period.</summary>
public interface IPayrollDeductionSummaryRefreshService
{
    Task<RefreshPayrollDeductionSummaryResult> RefreshAsync(
        RefreshPayrollDeductionSummaryRequest request,
        CancellationToken cancellationToken = default);

    Task<RecalculatePayrollDeductionSummaryPeriodResult> RecalculatePeriodAsync(
        RecalculatePayrollDeductionSummaryPeriodRequest request,
        CancellationToken cancellationToken = default);
}
