namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

/// <summary>Updates only the server-approved manual "other deduction" value.</summary>
public interface IPayrollDeductionSummaryManualAdjustmentService
{
    Task<PayrollDeductionSummaryListItemDto> UpdateManualOtherDeductionAsync(
        UpdatePayrollDeductionSummaryManualOtherDeductionRequest request,
        CancellationToken cancellationToken = default);
}
