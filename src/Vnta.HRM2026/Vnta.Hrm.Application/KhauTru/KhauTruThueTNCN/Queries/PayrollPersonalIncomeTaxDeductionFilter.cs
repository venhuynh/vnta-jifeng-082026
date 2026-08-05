namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

public sealed record PayrollPersonalIncomeTaxDeductionFilter(
    int? PayrollMonth,
    int? PayrollYear,
    string? SearchText,
    int Skip = 0,
    int Take = 50);
