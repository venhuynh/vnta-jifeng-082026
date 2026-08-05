using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapChuyenCan;

internal static class AttendanceAllowanceResultRecordMapper
{
    public static AttendanceAllowanceResultRecord MapRecord(AttendanceAllowanceResultListItemDto source)
    {
        var record = new AttendanceAllowanceResultRecord
        {
            Id = source.Id,
            AllowanceKind = source.AllowanceKind,
            EmployeeId = source.EmployeeId,
            EmployeeCode = source.EmployeeCode,
            EmployeeName = source.EmployeeName,
            DepartmentName = source.DepartmentName,
            PositionName = source.PositionName,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            StandardAllowanceAmount = source.StandardAllowanceAmount,
            StandardWorkdayCount = source.StandardWorkdayCount,
            ActualWorkdayCount = source.ActualWorkdayCount,
            IsLocked = source.IsLocked,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

        record.SetServerCalculatedValues(
            source.AttendanceRate,
            source.ActualAllowanceAmount,
            source.AppliedRuleKey,
            source.AttendanceClass,
            source.CtlWorkdayCount,
            source.LateEarlyMinutes,
            source.Kqcc,
            source.HasKpViolation,
            source.AdministrativeWorkdayCount,
            source.LateEarlyDeductionDays);
        return record;
    }
}
