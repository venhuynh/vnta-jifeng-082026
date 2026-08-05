using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Policies;

/// <summary>EF adapter cho dữ liệu bảng công và cấu hình mã phụ cấp chuyên cần.</summary>
public sealed class DatabaseAttendanceAllowanceWorkdaySource(ApplicationDbContext dbContext)
    : IAttendanceAllowanceWorkdaySource
{
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<AttendanceAllowanceWorkdayInput>>> LoadByEmployeeIdAsync(
        AttendanceAllowanceWorkdaySourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var employeeIds = request.EmployeeIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if(employeeIds.Length == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<AttendanceAllowanceWorkdayInput>>();
        }

        var periodStart = new DateOnly(request.PayrollYear, request.PayrollMonth, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var sourceRows = await (
                from summary in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
                join statusCode in dbContext.AttendanceStatusCodes.AsNoTracking()
                    on summary.CodeKetQuaTinhCongId equals statusCode.Id into statusCodeGroup
                from statusCode in statusCodeGroup.DefaultIfEmpty()
                where employeeIds.Contains(summary.EmployeeId)
                    && summary.DayType == AttendanceWorkCalendarDayTypes.Regular
                    && summary.WorkDate >= periodStart
                    && summary.WorkDate <= periodEnd
                select new AttendanceAllowanceWorkdaySourceRow(
                    summary.EmployeeId,
                    summary.LateMinutes,
                    summary.EarlyLeaveMinutes,
                    statusCode == null ? null : statusCode.Code,
                    statusCode != null && statusCode.PhuCapChuyenCan))
            .ToListAsync(cancellationToken);

        return sourceRows
            .GroupBy(row => row.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<AttendanceAllowanceWorkdayInput>)group
                    .Select(row => new AttendanceAllowanceWorkdayInput(
                        row.LateMinutes,
                        row.EarlyLeaveMinutes,
                        row.AttendanceStatusCode,
                        row.IsEligible
                            ? AttendanceAllowanceWorkdayEligibility.Eligible
                            : AttendanceAllowanceWorkdayEligibility.NotEligible))
                    .ToArray());
    }

    public async Task<IReadOnlyList<string>> LoadEligibleStatusCodesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.AttendanceStatusCodes
            .AsNoTracking()
            .Where(statusCode => statusCode.PhuCapChuyenCan)
            .OrderBy(statusCode => statusCode.Code)
            .Select(statusCode => statusCode.Code)
            .ToListAsync(cancellationToken);

    private sealed record AttendanceAllowanceWorkdaySourceRow(
        Guid EmployeeId,
        int LateMinutes,
        int EarlyLeaveMinutes,
        string? AttendanceStatusCode,
        bool IsEligible);
}
