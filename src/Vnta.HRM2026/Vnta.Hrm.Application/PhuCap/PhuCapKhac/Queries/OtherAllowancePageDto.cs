namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;

public sealed record OtherAllowancePageDto(
    IReadOnlyList<OtherAllowanceListItemDto> Rows,
    int TotalCount,
    decimal TotalAllowanceAmount);
