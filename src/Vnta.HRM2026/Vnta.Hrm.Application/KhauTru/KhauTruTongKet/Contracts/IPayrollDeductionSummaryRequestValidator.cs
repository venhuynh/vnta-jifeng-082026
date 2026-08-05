using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

/// <summary>
/// Single application-boundary validation contract for deduction-summary commands and queries.
/// HTTP and database adapters use the same rules, following the meal-allowance feature pattern.
/// </summary>
public interface IPayrollDeductionSummaryRequestValidator
{
    PayrollDeductionSummaryValidationResult ValidatePeriod(int payrollYear, int payrollMonth);
    PayrollDeductionSummaryValidationResult Validate(PayrollDeductionSummaryFilter filter);
    PayrollDeductionSummaryValidationResult Validate(PayrollDeductionSummaryExportRequest request);
    PayrollDeductionSummaryValidationResult Validate(SyncPayrollDeductionSummaryFromPreviousMonthRequest request);
    PayrollDeductionSummaryValidationResult Validate(RefreshPayrollDeductionSummaryRequest request);
    PayrollDeductionSummaryValidationResult Validate(RecalculatePayrollDeductionSummaryPeriodRequest request);
    PayrollDeductionSummaryValidationResult Validate(UpdatePayrollDeductionSummaryManualOtherDeductionRequest request);
    PayrollDeductionSummaryValidationResult Validate(SetPayrollDeductionSummaryLockStateRequest request);
    PayrollDeductionSummaryValidationResult Validate(SetPayrollDeductionSummaryBatchLockStateRequest request);
}

/// <summary>Transport-neutral validation result shared by HTTP and persistence adapters.</summary>
public sealed record PayrollDeductionSummaryValidationResult(string? ErrorMessage)
{
    public bool IsValid => string.IsNullOrWhiteSpace(ErrorMessage);

    public void ThrowIfInvalid()
    {
        if(!IsValid)
            throw new InvalidOperationException(ErrorMessage);
    }
}
