using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.TinhLuong.BangCongTongHop;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.TinhLuong.BangCongTongHop;

/// <summary>
/// Tổng hợp snapshot công tháng từ bảng công ngày. Mỗi kỳ chỉ cập nhật các dòng chưa khóa.
/// </summary>
public sealed class DatabasePayrollMonthlyWorkInputRefreshService(
    ApplicationDbContext dbContext)
    : IPayrollMonthlyWorkInputRefreshService
{
    public async Task<RefreshPayrollMonthlyWorkInputsResult> RefreshAsync(
        RefreshPayrollMonthlyWorkInputsRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request);

        var periodStart = new DateOnly(request.PayrollYear, request.PayrollMonth, 1);
        var periodEnd = periodStart.AddMonths(1);
        var payrollYear = checked((short)request.PayrollYear);
        var payrollMonth = checked((short)request.PayrollMonth);

        var employees = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => !employee.IsDeleted)
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);

        var sourceRows = await (
                from workday in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
                join statusCode in dbContext.AttendanceStatusCodes.AsNoTracking()
                    on workday.CodeKetQuaTinhCongId equals statusCode.Id into statusCodeGroup
                from statusCode in statusCodeGroup.DefaultIfEmpty()
                where workday.WorkDate >= periodStart && workday.WorkDate < periodEnd
                select new WorkdaySourceRow(
                    workday.EmployeeId,
                    statusCode != null && statusCode.CongHanhChinh,
                    workday.LateMinutes,
                    workday.EarlyLeaveMinutes,
                    workday.OvertimeMinutes15,
                    workday.OvertimeMinutes20,
                    workday.OvertimeMinutes30))
            .ToListAsync(cancellationToken);

        var aggregates = sourceRows
            .GroupBy(row => row.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group => new MonthlyWorkAggregate(
                    group.Count(row => row.IsAdministrativeWorkday),
                    group.Sum(row => Math.Max(0, row.LateMinutes) + Math.Max(0, row.EarlyLeaveMinutes)),
                    group.Sum(row => Math.Max(0, row.OvertimeMinutes15)),
                    group.Sum(row => Math.Max(0, row.OvertimeMinutes20)),
                    group.Sum(row => Math.Max(0, row.OvertimeMinutes30))));

        var rows = await dbContext.PayrollMonthlyWorkInputs
            .Where(row => row.PayrollYear == payrollYear && row.PayrollMonth == payrollMonth)
            .ToDictionaryAsync(row => row.EmployeeId, cancellationToken);

        var now = DateTime.UtcNow;
        var createdCount = 0;
        var updatedCount = 0;
        var skippedLockedCount = 0;

        foreach(var employeeId in employees)
        {
            var aggregate = aggregates.GetValueOrDefault(employeeId, MonthlyWorkAggregate.Empty);
            var administrativeWorkDays = (decimal)aggregate.AdministrativeWorkDays;
            var payrollWorkDays = PayrollMonthlyWorkInputCalculator.CalculatePayrollWorkDays(
                administrativeWorkDays,
                aggregate.LateEarlyLeaveMinutes);

            if(!rows.TryGetValue(employeeId, out var row))
            {
                dbContext.PayrollMonthlyWorkInputs.Add(new PayrollMonthlyWorkInputRow
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employeeId,
                    PayrollYear = payrollYear,
                    PayrollMonth = payrollMonth,
                    AdministrativeWorkDays = administrativeWorkDays,
                    LateEarlyLeaveMinutes = aggregate.LateEarlyLeaveMinutes,
                    OvertimeMinutes15 = aggregate.OvertimeMinutes15,
                    OvertimeMinutes20 = aggregate.OvertimeMinutes20,
                    OvertimeMinutes30 = aggregate.OvertimeMinutes30,
                    PayrollWorkDays = payrollWorkDays,
                    IsLocked = false,
                    CreatedAtUtc = now
                });
                createdCount++;
                continue;
            }

            if(row.IsLocked)
            {
                skippedLockedCount++;
                continue;
            }

            if(row.AdministrativeWorkDays == administrativeWorkDays
                && row.LateEarlyLeaveMinutes == aggregate.LateEarlyLeaveMinutes
                && row.OvertimeMinutes15 == aggregate.OvertimeMinutes15
                && row.OvertimeMinutes20 == aggregate.OvertimeMinutes20
                && row.OvertimeMinutes30 == aggregate.OvertimeMinutes30
                && row.PayrollWorkDays == payrollWorkDays)
            {
                continue;
            }

            row.AdministrativeWorkDays = administrativeWorkDays;
            row.LateEarlyLeaveMinutes = aggregate.LateEarlyLeaveMinutes;
            row.OvertimeMinutes15 = aggregate.OvertimeMinutes15;
            row.OvertimeMinutes20 = aggregate.OvertimeMinutes20;
            row.OvertimeMinutes30 = aggregate.OvertimeMinutes30;
            row.PayrollWorkDays = payrollWorkDays;
            row.UpdatedAtUtc = now;
            updatedCount++;
        }

        if(createdCount > 0 || updatedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new RefreshPayrollMonthlyWorkInputsResult(
            request.PayrollMonth,
            request.PayrollYear,
            employees.Count,
            createdCount,
            updatedCount,
            skippedLockedCount);
    }

    private static void ValidatePeriod(RefreshPayrollMonthlyWorkInputsRequest request)
    {
        if(request.PayrollMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng kỳ lương phải từ 1 đến 12.");
        }

        if(request.PayrollYear is < 1 or > 9999)
        {
            throw new InvalidOperationException("Năm kỳ lương phải từ 1 đến 9999.");
        }
    }

    private sealed record WorkdaySourceRow(
        Guid EmployeeId,
        bool IsAdministrativeWorkday,
        int LateMinutes,
        int EarlyLeaveMinutes,
        int OvertimeMinutes15,
        int OvertimeMinutes20,
        int OvertimeMinutes30);

    private sealed record MonthlyWorkAggregate(
        int AdministrativeWorkDays,
        int LateEarlyLeaveMinutes,
        int OvertimeMinutes15,
        int OvertimeMinutes20,
        int OvertimeMinutes30)
    {
        public static MonthlyWorkAggregate Empty { get; } = new(0, 0, 0, 0, 0);
    }
}
