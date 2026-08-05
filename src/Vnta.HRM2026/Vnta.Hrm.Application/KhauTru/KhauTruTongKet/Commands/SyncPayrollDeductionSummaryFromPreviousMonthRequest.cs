namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record SyncPayrollDeductionSummaryFromPreviousMonthRequest(
    int TargetPayrollMonth,
    int TargetPayrollYear,
    string? Actor);
