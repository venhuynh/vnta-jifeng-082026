using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.ChamCong.DuLieuTho;

public sealed class DatabaseAttendanceLogReadService(ApplicationDbContext dbContext)
    : IAttendanceLogReadService
{
    private const int MaxLogLimit = 5000;

    public Task<IReadOnlyList<AttendanceLogListItemDto>> GetRecentAsync(
        int take = 500,
        CancellationToken cancellationToken = default) =>
        SearchAsync(new AttendanceLogFilter(null, null, null, Take: take), cancellationToken);

    public Task<IReadOnlyList<AttendanceLogListItemDto>> GetByDateRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        int take = 2000,
        CancellationToken cancellationToken = default)
    {
        if(toDate < fromDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        return SearchAsync(
            new AttendanceLogFilter(
                null,
                fromDate.ToDateTime(TimeOnly.MinValue),
                toDate.ToDateTime(TimeOnly.MinValue),
                Take: take),
            cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceLogListItemDto>> SearchAsync(
        AttendanceLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedTake = Math.Clamp(filter.Take, 1, MaxLogLimit);
        var normalizedSearchTerm = NormalizeOptional(filter.SearchTerm);
        var (fromDate, toDate) = NormalizeDateRange(filter.FromDate, filter.ToDate);

        var query =
            from log in dbContext.AttendanceLogs.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on log.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join device in dbContext.Devices.AsNoTracking()
                on log.DeviceId equals device.Id into deviceGroup
            from device in deviceGroup.DefaultIfEmpty()
            select new { log, employee, device };

        if(fromDate.HasValue)
        {
            var normalizedFromDate = NormalizeDatabaseTimestamp(fromDate.Value.Date);
            query = query.Where(x =>
                x.log.AttTime.HasValue
                && x.log.AttTime.Value >= normalizedFromDate);
        }

        if(toDate.HasValue)
        {
            var normalizedToExclusive = NormalizeDatabaseTimestamp(toDate.Value.Date.AddDays(1));
            query = query.Where(x =>
                x.log.AttTime.HasValue
                && x.log.AttTime.Value < normalizedToExclusive);
        }

        if(filter.EmployeeId.HasValue)
        {
            query = query.Where(x => x.log.EmployeeId == filter.EmployeeId.Value);
        }

        if(!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            var searchPattern = $"%{normalizedSearchTerm}%";
            query = query.Where(x =>
                (x.log.DeviceCode != null && EF.Functions.ILike(x.log.DeviceCode, searchPattern))
                || (x.log.WorkCode != null && EF.Functions.ILike(x.log.WorkCode, searchPattern))
                || (x.log.DedupKey != null && EF.Functions.ILike(x.log.DedupKey, searchPattern))
                || (x.log.Status != null && EF.Functions.ILike(x.log.Status, searchPattern))
                || (x.log.Verify != null && EF.Functions.ILike(x.log.Verify, searchPattern))
                || (x.device != null && x.device.Code != null && EF.Functions.ILike(x.device.Code, searchPattern))
                || (x.device != null && x.device.Name != null && EF.Functions.ILike(x.device.Name, searchPattern))
                || (x.employee != null && x.employee.EmployeeCode != null && EF.Functions.ILike(x.employee.EmployeeCode, searchPattern))
                || (x.employee != null && x.employee.FirstName != null && EF.Functions.ILike(x.employee.FirstName, searchPattern))
                || (x.employee != null && x.employee.LastName != null && EF.Functions.ILike(x.employee.LastName, searchPattern)));
        }

        return await query
            .OrderByDescending(x => x.log.AttTime ?? DateTime.MinValue)
            .ThenByDescending(x => x.log.UpdateTime)
            .ThenByDescending(x => x.log.Id)
            .Take(normalizedTake)
            .Select(x => new AttendanceLogListItemDto(
                x.log.Id,
                x.log.DeviceId,
                x.log.EmployeeId,
                !string.IsNullOrWhiteSpace(x.log.DeviceCode)
                    ? x.log.DeviceCode
                    : x.device == null ? null : x.device.Code,
                x.device == null ? null : x.device.Name,
                x.employee == null ? null : x.employee.EmployeeCode,
                x.employee == null ? null : BuildEmployeeName(x.employee),
                x.log.AttTime,
                x.log.Status,
                x.log.Verify,
                x.log.WorkCode,
                x.log.Reserved1,
                x.log.Reserved2,
                x.log.MaskFlag,
                x.log.Temperature,
                x.log.DedupKey,
                x.log.UpdateTime,
                x.log.CreatedAtUtc,
                x.log.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static string BuildEmployeeName(AttendanceGatewayEmployeeRow employee)
    {
        var parts = new[] { employee.LastName, employee.FirstName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part!.Trim());

        return string.Join(" ", parts);
    }

    private static (DateTime? FromDate, DateTime? ToDate) NormalizeDateRange(
        DateTime? fromDate,
        DateTime? toDate)
    {
        if(fromDate.HasValue && toDate.HasValue && toDate.Value.Date < fromDate.Value.Date)
        {
            return (toDate.Value.Date, fromDate.Value.Date);
        }

        return (fromDate?.Date, toDate?.Date);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime NormalizeDatabaseTimestamp(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }
}
