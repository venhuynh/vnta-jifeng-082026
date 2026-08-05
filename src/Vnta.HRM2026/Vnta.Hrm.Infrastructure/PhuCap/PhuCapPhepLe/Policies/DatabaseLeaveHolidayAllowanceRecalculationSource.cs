using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Policies;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Policies;

/// <summary>EF Core adapter for the attendance and basic-salary facts used by recalculation.</summary>
public sealed class DatabaseLeaveHolidayAllowanceRecalculationSource(ApplicationDbContext dbContext)
    : ILeaveHolidayAllowanceRecalculationSource
{
    public async Task<IReadOnlyDictionary<Guid, LeaveHolidayAllowanceRecalculationSourceValues>>
        GetSourceValuesAsync(
            LeaveHolidayAllowanceRecalculationSourceRequest request,
            CancellationToken cancellationToken = default)
    {
        var employeeIds = request.EmployeeIds;
        var dailySalaryByEmployeeId = await dbContext.BasicSalaryRecords.AsNoTracking()
            .Where(row =>
                row.PayrollYear == request.PayrollYear
                && row.PayrollMonth == request.PayrollMonth
                && employeeIds.Contains(row.EmployeeId))
            .ToDictionaryAsync(row => row.EmployeeId, row => row.DailySalary, cancellationToken);

        var firstWorkDate = new DateOnly(request.PayrollYear, request.PayrollMonth, 1);
        var lastWorkDate = firstWorkDate.AddMonths(1).AddDays(-1);
        var leaveWorkdayCountByEmployeeId = await (
                from workday in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
                join statusCode in dbContext.AttendanceStatusCodes.AsNoTracking()
                    on workday.CodeKetQuaTinhCongId equals statusCode.Id
                where employeeIds.Contains(workday.EmployeeId)
                    && workday.WorkDate >= firstWorkDate
                    && workday.WorkDate <= lastWorkDate
                    && statusCode.PhuCapPhepLe
                group workday by workday.EmployeeId into employeeWorkdays
                select new
                {
                    EmployeeId = employeeWorkdays.Key,
                    LeaveWorkdayCount = employeeWorkdays.Count()
                })
            .ToDictionaryAsync(
                row => row.EmployeeId,
                row => (decimal)row.LeaveWorkdayCount,
                cancellationToken);

        return employeeIds.ToDictionary(
            employeeId => employeeId,
            employeeId => new LeaveHolidayAllowanceRecalculationSourceValues(
                dailySalaryByEmployeeId.TryGetValue(employeeId, out var dailySalary)
                    ? dailySalary
                    : null,
                leaveWorkdayCountByEmployeeId.GetValueOrDefault(employeeId)));
    }
}
