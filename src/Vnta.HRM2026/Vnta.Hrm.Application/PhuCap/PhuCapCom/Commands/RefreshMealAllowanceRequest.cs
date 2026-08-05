namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;

public sealed record RefreshMealAllowanceRequest(
    int TargetPayrollMonth,
    int TargetPayrollYear,
    Guid? EmployeeId = null,
    string? Actor = null);
