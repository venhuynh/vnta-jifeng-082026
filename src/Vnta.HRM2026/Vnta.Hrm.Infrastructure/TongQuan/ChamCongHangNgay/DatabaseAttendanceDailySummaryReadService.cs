using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.TongQuan.ChamCongHangNgay;

public sealed class DatabaseAttendanceDailySummaryReadService(ApplicationDbContext dbContext)
    : IAttendanceDailySummaryReadService
{
    private const int MaxSearchResultLimit = 5000;

    public async Task<IReadOnlyList<AttendanceDailySummaryListItemDto>> SearchAsync(
        AttendanceDailySummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedTake = Math.Clamp(filter.Take, 1, MaxSearchResultLimit);
        var normalizedSearchTerm = NormalizeOptional(filter.SearchText);
        var (fromDate, toDate) = NormalizeDateRange(filter.FromDate, filter.ToDate);

        var query =
            from summary in dbContext.AttendanceDailySummaries.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new { summary, employee, department, position };

        if(fromDate.HasValue)
        {
            query = query.Where(x => x.summary.WorkDate >= fromDate.Value);
        }

        if(toDate.HasValue)
        {
            query = query.Where(x => x.summary.WorkDate <= toDate.Value);
        }

        if(!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            var searchPattern = $"%{normalizedSearchTerm}%";
            query = query.Where(x =>
                (x.summary.PunchMomentsText != null && EF.Functions.ILike(x.summary.PunchMomentsText, searchPattern))
                || (x.employee != null && x.employee.EmployeeCode != null && EF.Functions.ILike(x.employee.EmployeeCode, searchPattern))
                || (x.employee != null && x.employee.FirstName != null && EF.Functions.ILike(x.employee.FirstName, searchPattern))
                || (x.employee != null && x.employee.LastName != null && EF.Functions.ILike(x.employee.LastName, searchPattern)));
        }

        return await query
            .OrderByDescending(x => x.summary.WorkDate)
            .ThenBy(x => x.employee == null ? string.Empty : x.employee.EmployeeCode)
            .ThenByDescending(x => x.summary.FirstPunchTime ?? DateTime.MinValue)
            .ThenByDescending(x => x.summary.Id)
            .Take(normalizedTake)
            .Select(x => new AttendanceDailySummaryListItemDto(
                x.summary.Id,
                x.summary.EmployeeId,
                x.employee == null ? null : x.employee.EmployeeCode,
                x.employee == null ? null : BuildEmployeeName(x.employee),
                x.department == null ? null : BuildDepartmentName(x.department),
                x.position == null ? null : x.position.Name,
                x.summary.WorkDate,
                x.summary.PunchCount,
                BuildResultCode(x.summary.PunchCount),
                BuildResultText(x.summary.PunchCount),
                x.summary.PunchMomentsText,
                x.summary.FirstPunchTime,
                x.summary.LastPunchTime,
                x.summary.CreatedAtUtc,
                x.summary.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static string BuildEmployeeName(AttendanceGatewayEmployeeRow employee)
    {
        var parts = new[] { employee.LastName, employee.FirstName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part.Trim());

        return string.Join(" ", parts);
    }

    private static string BuildDepartmentName(AttendanceDepartmentRow department)
    {
        return NormalizeOptional(department.DepartmentOrWorkshopName)
            ?? NormalizeOptional(department.TeamName)
            ?? NormalizeOptional(department.GroupName)
            ?? NormalizeOptional(department.CenterName)
            ?? string.Empty;
    }

    private static (DateOnly? FromDate, DateOnly? ToDate) NormalizeDateRange(
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        if(fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
        {
            return (toDate.Value, fromDate.Value);
        }

        return (fromDate, toDate);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildResultCode(int punchCount) => punchCount switch
    {
        <= 0 => "NO_PUNCH",
        1 => "SINGLE_PUNCH",
        2 => "PAIR_PUNCH",
        _ => "MULTI_PUNCH"
    };

    private static string BuildResultText(int punchCount) => punchCount switch
    {
        <= 0 => "Chưa có lượt chấm",
        1 => "Thiếu cặp chấm công",
        2 => "Đủ cặp vào/ra",
        _ => "Nhiều lượt chấm"
    };
}
