namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>
/// Server-side page for seniority allowance records. TotalCount applies the same
/// filter as Rows and is not limited by the requested page size.
/// </summary>
public sealed record PayrollEmployeeSeniorityAllowancePageDto(
    IReadOnlyList<PayrollEmployeeSeniorityAllowanceListItemDto> Rows,
    int TotalCount,
    decimal TotalAllowanceAmount);
