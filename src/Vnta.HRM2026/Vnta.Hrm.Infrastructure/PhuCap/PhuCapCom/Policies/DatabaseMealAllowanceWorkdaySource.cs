using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Policies;

/// <summary>EF read adapter supplying workdays to the application refresh calculator.</summary>
public sealed class DatabaseMealAllowanceWorkdaySource(ApplicationDbContext dbContext)
    : IMealAllowanceWorkdaySource
{
    public async Task<IReadOnlyList<MealAllowanceEmployeeWorkday>> LoadAsync(
        MealAllowanceRefreshPeriod period,
        CancellationToken cancellationToken = default)
    {
        var periodStart = new DateOnly(period.PayrollYear, period.PayrollMonth, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        return await (
            from summary in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking() on summary.EmployeeId equals employee.Id
            join shift in dbContext.Shifts.AsNoTracking() on summary.ShiftId equals shift.Id into shiftGroup
            from shift in shiftGroup.DefaultIfEmpty()
            where !employee.IsDeleted
                && summary.WorkDate >= periodStart && summary.WorkDate <= periodEnd
                && (!period.EmployeeId.HasValue || summary.EmployeeId == period.EmployeeId.Value)
            select new MealAllowanceEmployeeWorkday(summary.EmployeeId, new MealAllowanceWorkday(
                summary.DayType,
                new MealAllowanceShift(shift == null ? null : shift.Code, shift == null ? null : shift.Name,
                    shift == null ? null : shift.ShortName),
                summary.OvertimeMinutes15)))
            .ToListAsync(cancellationToken);
    }
}
