namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public sealed record PayrollInsuranceDeductionFilter(
    int? PayrollMonth,
    int? PayrollYear,
    string? SearchText,
    int Skip = 0,
    int Take = 50);
