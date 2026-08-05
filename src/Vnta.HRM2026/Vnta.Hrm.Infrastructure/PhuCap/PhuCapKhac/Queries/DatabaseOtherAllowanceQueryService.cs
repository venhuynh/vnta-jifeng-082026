using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;

public sealed class DatabaseOtherAllowanceQueryService(ApplicationDbContext dbContext) : IOtherAllowanceReadService
{
    // Popup tạo hiện dùng danh sách nhân viên tĩnh; chỉ giảm giới hạn sau khi thay bằng lookup server-side.
    private const int MaxPageSize = 5000;

    public async Task<OtherAllowancePageDto> SearchPageAsync(OtherAllowanceFilter filter, CancellationToken cancellationToken = default)
    {
        OtherAllowanceSearchPolicy.ValidatePayrollPeriod(filter.PayrollYear, filter.PayrollMonth);
        var take = Math.Clamp(filter.Take, 1, MaxPageSize);
        var skip = Math.Max(0, filter.Skip);
        var searchText = OtherAllowanceSearchPolicy.NormalizeSearchText(filter.SearchText);

        // Giữ anonymous projection cho aggregate được Npgsql dịch trực tiếp sang SQL.
        var query =
            from detail in dbContext.PayrollOtherAllowanceRecords.AsNoTracking()
            join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                on detail.PayrollAllowanceSummaryRecordId equals summary.Id
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employees
            from employee in employees.DefaultIfEmpty()
            where summary.PayrollYear == filter.PayrollYear && summary.PayrollMonth == filter.PayrollMonth
            select new
            {
                Detail = detail,
                Summary = summary,
                EmployeeCode = employee == null ? null : employee.EmployeeCode,
                EmployeeName = employee == null
                    ? null
                    : ((employee.LastName ?? string.Empty) + " " + (employee.FirstName ?? string.Empty)).Trim()
            };

        if(filter.IsLocked.HasValue)
            query = query.Where(row => row.Detail.IsLocked == filter.IsLocked.Value);

        if(searchText is not null)
        {
            var pattern = $"%{searchText}%";
            query = query.Where(row =>
                (row.EmployeeCode != null && EF.Functions.ILike(row.EmployeeCode, pattern))
                || (row.EmployeeName != null && EF.Functions.ILike(row.EmployeeName, pattern))
                || EF.Functions.ILike(row.Detail.AllowanceName, pattern)
                || (row.Detail.Note != null && EF.Functions.ILike(row.Detail.Note, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalAllowanceAmount = await query.SumAsync(row => (decimal?)row.Detail.AllowanceAmount, cancellationToken) ?? 0m;
        var rows = await query
            .OrderBy(row => row.EmployeeCode ?? string.Empty)
            .ThenBy(row => row.Detail.AllowanceName)
            .ThenBy(row => row.Detail.Id)
            .Skip(skip)
            .Take(take)
            .Select(row => new OtherAllowanceListItemDto(
                row.Detail.Id, row.Summary.Id, row.Summary.EmployeeId, row.EmployeeCode, row.EmployeeName,
                null, null, row.Summary.PayrollMonth, row.Summary.PayrollYear, row.Detail.AllowanceName,
                row.Detail.IsFixedAmount, row.Detail.AllowanceAmount, row.Detail.Note, row.Detail.IsLocked,
                row.Detail.CreatedAtUtc, row.Detail.CreatedBy, row.Detail.UpdatedAtUtc, row.Detail.UpdatedBy))
            .ToListAsync(cancellationToken);

        return new OtherAllowancePageDto(rows, totalCount, totalAllowanceAmount);
    }
}

internal static class OtherAllowanceQueryProjection
{
    public static Task<OtherAllowanceCommandResult> GetRequiredCommandResultAsync(ApplicationDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        (from detail in dbContext.PayrollOtherAllowanceRecords.AsNoTracking()
         join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
             on detail.PayrollAllowanceSummaryRecordId equals summary.Id
         join employee in dbContext.Employees.AsNoTracking()
             on summary.EmployeeId equals employee.Id into employees
         from employee in employees.DefaultIfEmpty()
         where detail.Id == id
         select new OtherAllowanceCommandResult(
             detail.Id, summary.Id, summary.EmployeeId,
             employee == null ? null : employee.EmployeeCode,
             employee == null ? null : ((employee.LastName ?? string.Empty) + " " + (employee.FirstName ?? string.Empty)).Trim(),
             null, null, summary.PayrollMonth, summary.PayrollYear, detail.AllowanceName,
             detail.IsFixedAmount, detail.AllowanceAmount, detail.Note, detail.IsLocked,
             detail.CreatedAtUtc, detail.CreatedBy, detail.UpdatedAtUtc, detail.UpdatedBy))
        .SingleAsync(cancellationToken);
}
