using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapThamNien;

internal static class PayrollEmployeeSeniorityAllowanceRecordMapper
{
    public static PhuCapThamNienRecord Map(PayrollEmployeeSeniorityAllowanceListItemDto source) => new()
    {
        Id = source.Id,
        PayrollAllowanceSummaryRecordId = source.PayrollAllowanceSummaryRecordId,
        EmployeeId = source.EmployeeId,
        EmployeeCode = source.EmployeeCode,
        EmployeeName = source.EmployeeName,
        DepartmentName = source.DepartmentName,
        PositionName = source.PositionName,
        PayrollMonth = source.PayrollMonth,
        PayrollYear = source.PayrollYear,
        EmploymentStartDate = source.EmploymentStartDate?.ToDateTime(TimeOnly.MinValue),
        CompletedSeniorityYears = source.CompletedSeniorityYears,
        CompletedSeniorityMonths = source.CompletedSeniorityMonths,
        AdministrativeWorkDays = source.AdministrativeWorkDays,
        LateEarlyLeaveWorkDays = source.LateEarlyLeaveWorkDays,
        SalaryWorkDays = source.SalaryWorkDays,
        AppliedRuleKey = source.AppliedRuleKey,
        AllowanceAmount = source.AllowanceAmount,
        Note = source.Note,
        IsLocked = source.IsLocked,
        IsSummaryLocked = source.IsSummaryLocked,
        RefreshedAtUtc = source.RefreshedAtUtc,
        RefreshedBy = source.RefreshedBy,
        UpdatedAtUtc = source.UpdatedAtUtc
    };
}
