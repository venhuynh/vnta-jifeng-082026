namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;

/// <summary>
/// Reconciles the deduction-summary snapshot from the five detail sources. A missing detail is
/// deliberately represented as <c>null</c> and contributes zero, matching the established refresh behavior.
/// </summary>
public static class PayrollDeductionSummaryReconciliationCalculator
{
    public static PayrollDeductionSummaryReconciliationResult Calculate(
        PayrollDeductionSummaryReconciliationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.CurrentSnapshot);
        ArgumentNullException.ThrowIfNull(input.DetailAmounts);

        var recalculatedAmounts = new PayrollDeductionSummaryAmounts(
            input.DetailAmounts.SocialInsuranceDeductionAmount ?? 0m,
            input.DetailAmounts.PersonalIncomeTaxDeductionAmount ?? 0m,
            input.DetailAmounts.UnionFeeDeductionAmount ?? 0m,
            input.DetailAmounts.AdvanceDeductionAmount ?? 0m,
            input.DetailAmounts.OtherDeductionAmount ?? 0m);

        var missingDetailSourceCount = new decimal?[]
        {
            input.DetailAmounts.SocialInsuranceDeductionAmount,
            input.DetailAmounts.PersonalIncomeTaxDeductionAmount,
            input.DetailAmounts.UnionFeeDeductionAmount,
            input.DetailAmounts.AdvanceDeductionAmount,
            input.DetailAmounts.OtherDeductionAmount
        }.Count(amount => !amount.HasValue);

        var status = input.CurrentSnapshot == recalculatedAmounts
            ? PayrollDeductionSummaryReconciliationStatus.AlreadyReconciled
            : PayrollDeductionSummaryReconciliationStatus.SnapshotChanged;

        return new PayrollDeductionSummaryReconciliationResult(
            recalculatedAmounts,
            missingDetailSourceCount,
            status);
    }
}

/// <summary>Amounts persisted in a deduction-summary snapshot.</summary>
public sealed record PayrollDeductionSummaryAmounts(
    decimal SocialInsuranceDeductionAmount,
    decimal PersonalIncomeTaxDeductionAmount,
    decimal UnionFeeDeductionAmount,
    decimal AdvanceDeductionAmount,
    decimal OtherDeductionAmount);

/// <summary>Amounts read from detail sources; null means that the corresponding detail row is absent.</summary>
public sealed record PayrollDeductionSummaryDetailAmounts(
    decimal? SocialInsuranceDeductionAmount,
    decimal? PersonalIncomeTaxDeductionAmount,
    decimal? UnionFeeDeductionAmount,
    decimal? AdvanceDeductionAmount,
    decimal? OtherDeductionAmount);

public sealed record PayrollDeductionSummaryReconciliationInput(
    PayrollDeductionSummaryAmounts CurrentSnapshot,
    PayrollDeductionSummaryDetailAmounts DetailAmounts);

public sealed record PayrollDeductionSummaryReconciliationResult(
    PayrollDeductionSummaryAmounts RecalculatedSnapshot,
    int MissingDetailSourceCount,
    PayrollDeductionSummaryReconciliationStatus Status);

public enum PayrollDeductionSummaryReconciliationStatus
{
    AlreadyReconciled,
    SnapshotChanged
}
