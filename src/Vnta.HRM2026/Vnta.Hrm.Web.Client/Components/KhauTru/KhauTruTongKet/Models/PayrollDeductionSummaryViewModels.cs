namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Models;

/// <summary>Mapped page result. Kept with UI models because the provider is the anti-corruption boundary.</summary>
public sealed record PayrollDeductionSummaryLoadResult(
    IReadOnlyList<PayrollDeductionSummaryRecord> Rows,
    int TotalCount,
    PayrollDeductionSummaryTotals Totals,
    PayrollDeductionSummaryLockStatusCounts LockStatusCounts);

public sealed record PayrollDeductionSummaryTotals(
    decimal SocialInsuranceDeductionAmount,
    decimal PersonalIncomeTaxDeductionAmount,
    decimal UnionFeeDeductionAmount,
    decimal AdvanceDeductionAmount,
    decimal OtherDeductionAmount,
    decimal TotalDeductionAmount)
{
    public static PayrollDeductionSummaryTotals Empty { get; } = new(0m, 0m, 0m, 0m, 0m, 0m);
}

public sealed record PayrollDeductionSummaryLockStatusCounts(int All, int Open, int Locked)
{
    public static PayrollDeductionSummaryLockStatusCounts Empty { get; } = new(0, 0, 0);
}
