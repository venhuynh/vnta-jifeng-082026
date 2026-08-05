using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien;

/// <summary>EF adapter that supplies attendance facts to the seniority allowance workday policy.</summary>
public sealed class DatabasePayrollEmployeeSeniorityAllowanceWorkdaySource(ApplicationDbContext dbContext)
    : IPayrollEmployeeSeniorityAllowanceWorkdaySource
{
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<PayrollEmployeeSeniorityAllowanceWorkdayInput>>> LoadAsync(
        PayrollEmployeeSeniorityAllowanceWorkdaySourceQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.EmployeeIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyCollection<PayrollEmployeeSeniorityAllowanceWorkdayInput>>();
        }

        var periodStart = new DateOnly(query.PayrollYear, query.PayrollMonth, 1);
        var periodEnd = new DateOnly(
            query.PayrollYear,
            query.PayrollMonth,
            DateTime.DaysInMonth(query.PayrollYear, query.PayrollMonth));

        var workdays = await (
            from summary in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
            join statusCode in dbContext.AttendanceStatusCodes.AsNoTracking()
                on summary.CodeKetQuaTinhCongId equals statusCode.Id into statusCodeGroup
            from statusCode in statusCodeGroup.DefaultIfEmpty()
            where query.EmployeeIds.Contains(summary.EmployeeId)
                && summary.WorkDate >= periodStart
                && summary.WorkDate <= periodEnd
            select new
            {
                summary.EmployeeId,
                summary.LateMinutes,
                summary.EarlyLeaveMinutes,
                // Công HC của phụ cấp thâm niên được quyết định bởi cờ "Thâm niên"
                // trong Code kết quả tính công, độc lập với cờ Công hành chính.
                Eligibility = statusCode != null && statusCode.PhuCapThamNien
                    ? PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included
                    : PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Excluded
            })
            .ToListAsync(cancellationToken);

        return workdays
            .GroupBy(workday => workday.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<PayrollEmployeeSeniorityAllowanceWorkdayInput>)group
                    .Select(workday => new PayrollEmployeeSeniorityAllowanceWorkdayInput(
                        workday.Eligibility,
                        workday.LateMinutes,
                        workday.EarlyLeaveMinutes))
                    .ToArray());
    }
}
