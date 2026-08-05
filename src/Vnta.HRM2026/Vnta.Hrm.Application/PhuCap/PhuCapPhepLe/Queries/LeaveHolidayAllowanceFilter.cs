namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;

public sealed record LeaveHolidayAllowanceFilter(
    int PayrollMonth,
    int PayrollYear,
    string? SearchText,
    int Take = 2000);
