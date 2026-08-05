namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Queries;

public sealed record OtherResponsibilityAllowanceFilter(
    int PayrollMonth,
    int PayrollYear,
    string? SearchText,
    bool? IsLocked = null,
    int Take = 2000);
