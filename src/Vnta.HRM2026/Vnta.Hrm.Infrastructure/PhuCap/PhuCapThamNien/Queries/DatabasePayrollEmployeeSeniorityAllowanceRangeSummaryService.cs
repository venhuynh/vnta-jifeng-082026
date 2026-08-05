using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Queries;

/// <summary>Handler thống kê nhanh theo khoảng thâm niên.</summary>
public sealed class DatabasePayrollEmployeeSeniorityAllowanceRangeSummaryService(ApplicationDbContext dbContext)
    : IPayrollEmployeeSeniorityAllowanceRangeSummaryService
{
    public Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>> GetRangeSummariesAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        GetRangeSummariesCoreAsync(filter, cancellationToken);

    private async Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>> GetRangeSummariesCoreAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var keys = new[] { string.Empty, "under-1", "1-3", "3-6", "6-10", "10-13", "13-plus" };
        var result = new List<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>(keys.Length);
        foreach(var key in keys)
        {
            var count = await SeniorityAllowanceReadProjection.BuildFilteredQuery(dbContext, filter with { SeniorityRangeKey = key })
                .CountAsync(cancellationToken);
            result.Add(new PayrollEmployeeSeniorityAllowanceRangeSummaryDto(key, count));
        }
        return result;
    }
}
