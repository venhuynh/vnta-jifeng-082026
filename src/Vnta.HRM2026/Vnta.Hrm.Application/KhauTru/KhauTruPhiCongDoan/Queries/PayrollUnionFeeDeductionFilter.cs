namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public sealed record PayrollUnionFeeDeductionFilter(
    int? PayrollMonth,
    int? PayrollYear,
    string? SearchText,
    int Skip = 0,
    int Take = 50);
