namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public sealed record SyncLeaveHolidayAllowanceFromPreviousMonthRequest(
    int TargetPayrollMonth,
    int TargetPayrollYear,
    string? Actor = null);
