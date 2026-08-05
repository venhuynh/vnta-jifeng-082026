namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

/// <summary>Tổng các khoản khấu trừ của toàn bộ tập kết quả đã được server lọc.</summary>
public sealed record PayrollDeductionSummaryAggregateDto(
    decimal SocialInsuranceDeductionAmount,
    decimal PersonalIncomeTaxDeductionAmount,
    decimal UnionFeeDeductionAmount,
    decimal AdvanceDeductionAmount,
    decimal OtherDeductionAmount,
    decimal TotalDeductionAmount)
{
    public static PayrollDeductionSummaryAggregateDto Empty { get; } = new(0m, 0m, 0m, 0m, 0m, 0m);
}
