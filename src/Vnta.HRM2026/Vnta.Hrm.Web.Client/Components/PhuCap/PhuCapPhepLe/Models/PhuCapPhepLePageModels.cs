namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

/// <summary>Feature-local option rendered by the payroll-period toolbar.</summary>
public sealed record LeaveHolidayAllowanceMonthOption(int Value, string Text);

/// <summary>Immutable reload input captured before an asynchronous data request.</summary>
public sealed record LeaveHolidayAllowanceReloadRequest(
    int PayrollMonth,
    int PayrollYear,
    string? SearchText,
    LeaveHolidayAllowanceLockFilter LockFilter,
    int PageIndex,
    int PageSize);

public enum LeaveHolidayAllowanceLockFilter
{
    All,
    OpenOnly,
    LockedOnly
}
