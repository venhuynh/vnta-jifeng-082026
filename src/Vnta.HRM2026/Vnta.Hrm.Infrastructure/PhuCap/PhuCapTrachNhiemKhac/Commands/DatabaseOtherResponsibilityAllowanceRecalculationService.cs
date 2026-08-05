using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac;

public sealed class DatabaseOtherResponsibilityAllowanceRecalculationService(
    ApplicationDbContext dbContext,
    IOtherResponsibilityAllowancePeriodPreparationService periodPreparationService,
    IOtherResponsibilityAllowanceCalculator calculationPolicy,
    IOtherResponsibilityAllowanceWorkdayCalculator workdayCalculationPolicy,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource)
    : IOtherResponsibilityAllowanceRecalculationService
{
    public async Task<RecalculateOtherResponsibilityAllowanceResult> RecalculateAsync(
        RecalculateOtherResponsibilityAllowanceRequest request,
        string? requestedBy = null,
        CancellationToken cancellationToken = default)
    {
        OtherResponsibilityAllowancePeriodPolicy.Validate(request.PayrollYear, request.PayrollMonth);
        var actor = OtherResponsibilityAllowancePersistenceSupport.NormalizeActor(requestedBy);
        await periodPreparationService.PreparePeriodAsync(request.PayrollYear, request.PayrollMonth, actor, cancellationToken);

        var periodRows = await (
            from detail in dbContext.PayrollAllowanceOtherResponsibilityRecords
            join summary in dbContext.PayrollAllowanceSummaryRecords
                on detail.PayrollAllowanceSummaryRecordId equals summary.Id
            where summary.PayrollYear == request.PayrollYear && summary.PayrollMonth == request.PayrollMonth
            select new { Detail = detail, Summary = summary })
            .ToListAsync(cancellationToken);
        var unlockedRows = periodRows.Where(row => !row.Detail.IsLocked && !row.Summary.IsLocked).ToArray();
        var employeeIds = unlockedRows.Select(row => row.Summary.EmployeeId).Distinct().ToArray();
        if(unlockedRows.Length == 0)
        {
            return new RecalculateOtherResponsibilityAllowanceResult(0, periodRows.Count);
        }

        var standardAllowanceByEmployee = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .AsNoTracking()
            .Where(row => row.Year == request.PayrollYear && row.Month == request.PayrollMonth && employeeIds.Contains(row.EmployeeId))
            .GroupBy(row => row.EmployeeId)
            .Select(group => new { EmployeeId = group.Key, StandardAllowanceAmount = group.Max(row => row.StandardResponsibilityAllowanceAmount) })
            .ToDictionaryAsync(row => row.EmployeeId, row => row.StandardAllowanceAmount, cancellationToken);
        var standardWorkdaysByEmployee = await basicSalaryWorkdaySource.LoadStandardWorkingDaysAsync(
            request.PayrollYear,
            request.PayrollMonth,
            employeeIds,
            cancellationToken);
        var workdayAggregateByEmployee = await LoadWorkdayAggregatesAsync(employeeIds, request.PayrollYear, request.PayrollMonth, cancellationToken);

        var now = OtherResponsibilityAllowancePersistenceSupport.GetDatabaseNow();
        foreach(var row in unlockedRows)
        {
            var employeeId = row.Summary.EmployeeId;
            var calculationWorkdays = workdayAggregateByEmployee.TryGetValue(employeeId, out var aggregate) ? aggregate.CalculationWorkdays : 0m;
            var standardWorkdays = standardWorkdaysByEmployee.GetValueOrDefault(employeeId);
            var standardAllowance = standardAllowanceByEmployee.GetValueOrDefault(employeeId);
            var actualAllowance = calculationPolicy.Calculate(new OtherResponsibilityAllowanceCalculationInput(
                standardAllowance,
                standardWorkdays,
                calculationWorkdays)).ActualResponsibilityAllowanceAmount;
            row.Detail.AllowanceWorkdayCount = calculationWorkdays;
            row.Detail.StandardResponsibilityAllowanceAmount = standardAllowance;
            row.Detail.ActualResponsibilityAllowanceAmount = actualAllowance;
            row.Detail.RefreshedAtUtc = now;
            row.Detail.RefreshedBy = actor;
            row.Detail.UpdatedAtUtc = now;
            row.Detail.UpdatedBy = actor;

            row.Summary.ResponsibilityOtherAllowanceAmount = actualAllowance;
            row.Summary.UpdatedAtUtc = now;
            row.Summary.UpdatedBy = actor;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new RecalculateOtherResponsibilityAllowanceResult(unlockedRows.Length, periodRows.Count - unlockedRows.Length);
    }

    private async Task<IReadOnlyDictionary<Guid, OtherResponsibilityWorkdayAggregate>> LoadWorkdayAggregatesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        int payrollYear,
        int payrollMonth,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(payrollYear, payrollMonth, 1);
        var monthEnd = monthStart.AddMonths(1);
        var attendanceRows = await (
            from workday in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
            join statusCode in dbContext.AttendanceStatusCodes.AsNoTracking()
                on workday.CodeKetQuaTinhCongId equals statusCode.Id into statusCodeGroup
            from statusCode in statusCodeGroup.DefaultIfEmpty()
            where employeeIds.Contains(workday.EmployeeId) && workday.WorkDate >= monthStart && workday.WorkDate < monthEnd
            select new
            {
                workday.EmployeeId,
                workday.WorkDate,
                Eligibility = statusCode != null && statusCode.CongHanhChinh
                    ? OtherResponsibilityAllowanceWorkdayEligibility.EligibleAdministrativeWorkday
                    : OtherResponsibilityAllowanceWorkdayEligibility.NotEligible,
                workday.LateMinutes,
                workday.EarlyLeaveMinutes
            })
            .ToListAsync(cancellationToken);

        return attendanceRows.GroupBy(row => row.EmployeeId).ToDictionary(
            group => group.Key,
            group => new OtherResponsibilityWorkdayAggregate(workdayCalculationPolicy.Calculate(
                group.Select(row => new OtherResponsibilityAllowanceAttendanceEntry(
                    row.WorkDate,
                    row.Eligibility,
                    row.LateMinutes,
                    row.EarlyLeaveMinutes)).ToArray()).AllowanceCalculationWorkdayCount));
    }

    private sealed record OtherResponsibilityWorkdayAggregate(decimal CalculationWorkdays);

}
