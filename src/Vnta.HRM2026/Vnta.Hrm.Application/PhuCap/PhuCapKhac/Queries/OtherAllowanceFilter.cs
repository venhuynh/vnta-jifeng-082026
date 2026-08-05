namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;

public sealed record OtherAllowanceFilter(
    int PayrollMonth,
    int PayrollYear,
    string? SearchText = null,
    bool? IsLocked = null,
    int Take = 100,
    int Skip = 0);
