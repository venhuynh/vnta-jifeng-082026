namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

public sealed record RefreshAttendanceAllowanceRequest(int TargetPayrollMonth, int TargetPayrollYear, Guid? PayrollAllowanceSummaryRecordId = null);
