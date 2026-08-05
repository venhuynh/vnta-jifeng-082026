namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public sealed class PayrollEmployeeSeniorityAllowanceConflictException(string message)
    : InvalidOperationException(message);
