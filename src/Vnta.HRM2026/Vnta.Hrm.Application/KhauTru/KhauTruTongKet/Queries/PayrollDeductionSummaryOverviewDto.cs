namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record PayrollDeductionSummaryOverviewDto(
    int TotalCount,
    int OpenCount,
    int LockedCount,
    decimal TotalDeductionAmount);
