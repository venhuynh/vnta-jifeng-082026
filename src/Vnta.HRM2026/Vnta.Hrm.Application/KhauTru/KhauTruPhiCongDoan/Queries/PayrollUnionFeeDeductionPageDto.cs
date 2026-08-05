namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public sealed record PayrollUnionFeeDeductionPageDto(
    IReadOnlyList<PayrollUnionFeeDeductionListItemDto> Items,
    int TotalCount);
