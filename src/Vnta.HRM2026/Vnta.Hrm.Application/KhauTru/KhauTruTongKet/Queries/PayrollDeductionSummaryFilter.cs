namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record PayrollDeductionSummaryFilter(
    int? PayrollMonth,
    int? PayrollYear,
    string? SearchText,
    bool? IsLocked = null,
    int Skip = 0,
    int Take = 50);
