namespace Vnta.Hrm.Application.KhauTru.KhauTruTamUng;

public sealed record PayrollAdvanceDeductionPageDto(
    IReadOnlyList<PayrollAdvanceDeductionListItemDto> Items,
    int TotalCount);
