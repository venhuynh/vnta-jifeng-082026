namespace Vnta.Hrm.Application.KhauTru.KhauTruTamUng;

public sealed record PayrollAdvanceDeductionFilter(
    int? PayrollMonth,
    int? PayrollYear,
    string? SearchText,
    int Skip = 0,
    int Take = 50);
