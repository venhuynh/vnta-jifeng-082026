namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record PayrollDeductionSummaryPageDto(
    IReadOnlyList<PayrollDeductionSummaryListItemDto> Rows,
    int TotalCount,
    PayrollDeductionSummaryAggregateDto Totals,
    PayrollDeductionSummaryLockStatusCountsDto LockStatusCounts);
