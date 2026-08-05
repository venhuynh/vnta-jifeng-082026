namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

public sealed record PayrollPersonalIncomeTaxDeductionPageDto(
    IReadOnlyList<PayrollPersonalIncomeTaxDeductionListItemDto> Items,
    int TotalCount);
