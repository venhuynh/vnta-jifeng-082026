using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.TinhLuong.LuongCanBan;

/// <summary>EF adapter dùng chung để đọc công chuẩn từ bảng lương căn bản.</summary>
public sealed class DatabaseBasicSalaryWorkdaySource(ApplicationDbContext dbContext)
    : IBasicSalaryWorkdaySource
{
    public async Task<IReadOnlyDictionary<Guid, decimal>> LoadStandardWorkingDaysAsync(
        int payrollYear,
        int payrollMonth,
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmployeeIds = employeeIds
            .Where(employeeId => employeeId != Guid.Empty)
            .Distinct()
            .ToArray();
        if(normalizedEmployeeIds.Length == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        return await dbContext.BasicSalaryRecords
            .AsNoTracking()
            .Where(row => row.PayrollYear == payrollYear
                && row.PayrollMonth == payrollMonth
                && normalizedEmployeeIds.Contains(row.EmployeeId))
            .GroupBy(row => row.EmployeeId)
            .Select(group => new
            {
                EmployeeId = group.Key,
                StandardWorkingDays = group.Max(row => row.StandardWorkingDays)
            })
            .ToDictionaryAsync(
                row => row.EmployeeId,
                row => row.StandardWorkingDays,
                cancellationToken);
    }
}
