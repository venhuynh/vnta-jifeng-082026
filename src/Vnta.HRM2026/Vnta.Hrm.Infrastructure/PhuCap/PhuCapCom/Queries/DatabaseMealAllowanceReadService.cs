using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Queries;

public sealed class DatabaseMealAllowanceReadService(
    ApplicationDbContext dbContext,
    IMealAllowanceRequestValidator requestValidator)
    : IMealAllowanceReadService, IMealAllowanceExportService
{
    private const int MaxSearchResultLimit = 5000;

    public async Task<IReadOnlyList<MealAllowanceListItemDto>> SearchAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        (await SearchPageAsync(filter with { Skip = 0 }, cancellationToken)).Rows;

    public async Task<MealAllowancePageDto> SearchPageAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedSkip = Math.Max(0, filter.Skip);
        var query = MealAllowanceReadProjection.BuildFilteredQuery(dbContext, filter);
        var totalCount = filter.IncludeTotalCount ? await query.CountAsync(cancellationToken) : (int?)null;
        if(totalCount is 0 || (totalCount.HasValue && normalizedSkip >= totalCount.Value))
            return new MealAllowancePageDto([], totalCount ?? -1);

        var rows = await query
            .OrderByDescending(x => x.Summary.PayrollYear)
            .ThenByDescending(x => x.Summary.PayrollMonth)
            .ThenBy(x => x.Employee == null ? string.Empty : x.Employee.EmployeeCode)
            .ThenByDescending(x => x.Result.UpdatedAtUtc ?? x.Result.CreatedAtUtc)
            .ThenByDescending(x => x.Result.PayrollAllowanceSummaryRecordId)
            .Skip(normalizedSkip)
            .Take(NormalizeTake(filter.Take))
            .Select(x => MealAllowanceReadProjection.MapToDto(x))
            .ToListAsync(cancellationToken);

        return new MealAllowancePageDto(rows, totalCount ?? -1);
    }

    public async Task<IReadOnlyList<MealAllowanceListItemDto>> ExportPeriodAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken = default)
    {
        requestValidator.ValidatePeriod(payrollMonth, payrollYear).ThrowIfInvalid();

        return await MealAllowanceReadProjection
            .BuildFilteredQuery(dbContext, new MealAllowanceFilter(payrollMonth, payrollYear, null, 1))
            .OrderBy(x => x.Employee == null ? string.Empty : x.Employee.EmployeeCode)
            .ThenByDescending(x => x.Result.UpdatedAtUtc ?? x.Result.CreatedAtUtc)
            .ThenByDescending(x => x.Result.PayrollAllowanceSummaryRecordId)
            .Select(x => MealAllowanceReadProjection.MapToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<MealAllowanceSummaryDto> GetSummaryAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = MealAllowanceReadProjection.BuildFilteredQuery(dbContext, filter with { SummaryBucketKey = null });
        var summary = await query.GroupBy(_ => 1).Select(group => new
        {
            TotalCount = group.Count(),
            QualifiedRuleCount = group.Count(x => !x.Result.IsLocked && x.Result.RuleCode == MealAllowancePolicy.QualifiedMealRuleCode),
            ManualAdjustmentCount = group.Count(x => !x.Result.IsLocked && x.Result.RuleCode == MealAllowancePolicy.ManualAdjustmentRuleCode),
            LockedCount = group.Count(x => x.Result.IsLocked),
            WithAllowanceCount = group.Count(x => x.Result.Overtime1900Days > 0),
            WithoutAllowanceCount = group.Count(x => x.Result.Overtime1900Days == 0),
            TotalAllowanceAmount = group.Sum(x => x.Result.MealAllowanceAmount),
            OtherCount = group.Count(x => !x.Result.IsLocked
                && x.Result.RuleCode != MealAllowancePolicy.QualifiedMealRuleCode
                && x.Result.RuleCode != MealAllowancePolicy.ManualAdjustmentRuleCode)
        }).SingleOrDefaultAsync(cancellationToken);

        return summary is null
            ? new MealAllowanceSummaryDto(0, 0, 0, 0, 0, 0, 0, 0m)
            : new MealAllowanceSummaryDto(summary.TotalCount, summary.QualifiedRuleCount, summary.ManualAdjustmentCount,
                summary.LockedCount, summary.OtherCount, summary.WithAllowanceCount, summary.WithoutAllowanceCount,
                summary.TotalAllowanceAmount);
    }

    private static int NormalizeTake(int take) => take <= 0 ? 50 : Math.Min(take, MaxSearchResultLimit);
}
