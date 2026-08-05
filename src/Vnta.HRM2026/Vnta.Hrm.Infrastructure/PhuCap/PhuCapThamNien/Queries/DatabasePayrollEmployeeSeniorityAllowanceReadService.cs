using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Queries;

/// <summary>Handler đọc danh sách phân trang của phụ cấp thâm niên.</summary>
public sealed class DatabasePayrollEmployeeSeniorityAllowanceReadService(ApplicationDbContext dbContext)
    : IPayrollEmployeeSeniorityAllowanceReadService
{
    public Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceListItemDto>> SearchAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        SearchAsyncCore(filter, cancellationToken);

    public Task<PayrollEmployeeSeniorityAllowancePageDto> SearchPageAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        SearchPageAsyncCore(filter, cancellationToken);

    private async Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceListItemDto>> SearchAsyncCore(PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken) =>
        (await SearchPageAsyncCore(filter with { Skip = 0 }, cancellationToken)).Rows;

    private async Task<PayrollEmployeeSeniorityAllowancePageDto> SearchPageAsyncCore(PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = SeniorityAllowanceReadProjection.BuildFilteredQuery(dbContext, filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var totalAmount = await query.Select(x => (decimal?)x.Detail.AllowanceAmount).SumAsync(cancellationToken) ?? 0m;
        var skip = Math.Max(0, filter.Skip);
        if(totalCount == 0 || skip >= totalCount)
            return new PayrollEmployeeSeniorityAllowancePageDto([], totalCount, totalAmount);

        var rows = await query.OrderBy(x => x.EmployeeCode ?? string.Empty).ThenBy(x => x.EmployeeName ?? string.Empty)
            .ThenBy(x => x.Detail.PayrollAllowanceSummaryRecordId).Skip(skip).Take(Math.Clamp(filter.Take, 1, SeniorityAllowanceReadProjection.MaxTake))
            .Select(x => SeniorityAllowanceReadProjection.Map(x)).ToListAsync(cancellationToken);
        return new PayrollEmployeeSeniorityAllowancePageDto(rows, totalCount, totalAmount);
    }
}
