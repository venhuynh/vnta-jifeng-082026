using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

/// <summary>Single AsNoTracking projection used by the read and export use cases.</summary>
public sealed class HazardAllowanceReadProjection(
    ApplicationDbContext dbContext,
    IHazardAllowanceRequestValidator requestValidator)
{
    private const int MaxSearchResultLimit = 5000;

    public async Task<IReadOnlyList<HazardAllowanceListItemDto>> SearchAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken)
        => (await SearchPageAsync(filter with { Skip = 0 }, cancellationToken)).Rows;

    public async Task<HazardAllowancePageDto> SearchPageAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = BuildFilteredQuery(filter);
        var totalCount = filter.IncludeTotalCount ? await query.CountAsync(cancellationToken) : -1;
        var skip = Math.Max(filter.Skip, 0);
        if (totalCount == 0 || (totalCount >= 0 && skip >= totalCount)) return new HazardAllowancePageDto([], totalCount);

        var rows = await ApplyStableOrdering(query)
            .Skip(skip)
            .Take(Math.Clamp(filter.Take, 1, MaxSearchResultLimit))
            .ToListAsync(cancellationToken);
        return new HazardAllowancePageDto(rows.Select(ToDto).ToArray(), totalCount);
    }

    public async Task<HazardAllowanceSummaryDto> GetSummaryAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var summaryFilter = filter with { SummaryBucket = HazardAllowanceSummaryBucket.All };
        requestValidator.Validate(summaryFilter).ThrowIfInvalid();
        var month = (short)summaryFilter.PayrollMonth;
        var year = (short)summaryFilter.PayrollYear;
        var search = HazardAllowancePersistence.NormalizeOptional(summaryFilter.SearchText);

        var query =
            from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
            join detail in dbContext.PayrollHazardAllowanceRecords.AsNoTracking() on summary.Id equals detail.PayrollAllowanceSummaryRecordId
            join employee in dbContext.Employees.AsNoTracking() on summary.EmployeeId equals employee.Id into employees
            from employee in employees.DefaultIfEmpty()
            where summary.PayrollMonth == month && summary.PayrollYear == year
            select new
            {
                IsLocked = detail.IsLocked || summary.IsLocked, detail.IsEligibleDepartment, detail.IsEligibleForAllowance, detail.QualifiedWorkdayCount, detail.ExclusionReason,
                EmployeeCode = employee == null ? null : employee.EmployeeCode,
                EmployeeFirstName = employee == null ? null : employee.FirstName,
                EmployeeLastName = employee == null ? null : employee.LastName
            };

        query = summaryFilter.LockState switch
        {
            HazardAllowanceLockState.Open => query.Where(row => !row.IsLocked),
            HazardAllowanceLockState.Locked => query.Where(row => row.IsLocked),
            _ => query
        };
        if (search is not null)
        {
            var pattern = $"%{search}%";
            query = query.Where(row =>
                (row.EmployeeCode != null && EF.Functions.ILike(row.EmployeeCode, pattern))
                || (row.EmployeeFirstName != null && EF.Functions.ILike(row.EmployeeFirstName, pattern))
                || (row.EmployeeLastName != null && EF.Functions.ILike(row.EmployeeLastName, pattern))
                || (row.ExclusionReason != null && EF.Functions.ILike(row.ExclusionReason, pattern)));
        }

        var result = await query.GroupBy(_ => 1).Select(group => new
        {
            Total = group.Count(), Eligible = group.Count(row => row.IsEligibleForAllowance),
            Exceptions = group.Count(row => !row.IsEligibleForAllowance),
            Locked = group.Count(row => row.IsLocked), Open = group.Count(row => !row.IsLocked)
        }).SingleOrDefaultAsync(cancellationToken);
        return result is null ? new HazardAllowanceSummaryDto(0, 0, 0, 0, 0)
            : new HazardAllowanceSummaryDto(result.Total, result.Eligible, result.Exceptions, result.Locked, result.Open);
    }

    public async Task<IReadOnlyList<HazardAllowanceListItemDto>> ExportAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken) =>
        (await ApplyStableOrdering(BuildFilteredQuery(filter)).ToListAsync(cancellationToken)).Select(ToDto).ToArray();

    private IQueryable<Row> BuildFilteredQuery(HazardAllowanceFilter filter)
    {
        requestValidator.Validate(filter).ThrowIfInvalid();
        var search = HazardAllowancePersistence.NormalizeOptional(filter.SearchText);
        var month = (short)filter.PayrollMonth;
        var year = (short)filter.PayrollYear;
        IQueryable<Row> query =
            from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
            join detail in dbContext.PayrollHazardAllowanceRecords.AsNoTracking() on summary.Id equals detail.PayrollAllowanceSummaryRecordId
            join employee in dbContext.Employees.AsNoTracking() on summary.EmployeeId equals employee.Id into employees
            from employee in employees.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking() on employee.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking() on employee.PositionId equals position.Id into positions
            from position in positions.DefaultIfEmpty()
            where summary.PayrollMonth == month && summary.PayrollYear == year
            select new Row
            {
                Summary = summary, Detail = detail,
                EmployeeCode = employee == null ? null : employee.EmployeeCode,
                EmployeeName = employee == null ? null : ((employee.LastName ?? string.Empty) + " " + (employee.FirstName ?? string.Empty)).Trim(),
                DepartmentCenterName = department == null ? null : department.CenterName,
                DepartmentOrWorkshopName = department == null ? null : department.DepartmentOrWorkshopName,
                DepartmentTeamName = department == null ? null : department.TeamName,
                DepartmentGroupName = department == null ? null : department.GroupName,
                PositionName = position == null ? null : position.Name
            };
        query = filter.LockState switch
        {
            HazardAllowanceLockState.Open => query.Where(row => !row.Detail.IsLocked && !row.Summary.IsLocked),
            HazardAllowanceLockState.Locked => query.Where(row => row.Detail.IsLocked || row.Summary.IsLocked), _ => query
        };
        query = filter.SummaryBucket switch
        {
            HazardAllowanceSummaryBucket.Eligible => query.Where(row => row.Detail.IsEligibleForAllowance),
            HazardAllowanceSummaryBucket.Exception => query.Where(row => !row.Detail.IsEligibleForAllowance),
            HazardAllowanceSummaryBucket.Locked => query.Where(row => row.Detail.IsLocked || row.Summary.IsLocked),
            HazardAllowanceSummaryBucket.Open => query.Where(row => !row.Detail.IsLocked && !row.Summary.IsLocked), _ => query
        };
        if (search is not null)
        {
            var pattern = $"%{search}%";
            query = query.Where(row => (row.EmployeeCode != null && EF.Functions.ILike(row.EmployeeCode, pattern))
                || (row.EmployeeName != null && EF.Functions.ILike(row.EmployeeName, pattern))
                || (row.DepartmentCenterName != null && EF.Functions.ILike(row.DepartmentCenterName, pattern))
                || (row.DepartmentOrWorkshopName != null && EF.Functions.ILike(row.DepartmentOrWorkshopName, pattern))
                || (row.DepartmentTeamName != null && EF.Functions.ILike(row.DepartmentTeamName, pattern))
                || (row.DepartmentGroupName != null && EF.Functions.ILike(row.DepartmentGroupName, pattern))
                || (row.PositionName != null && EF.Functions.ILike(row.PositionName, pattern))
                || (row.Detail.ExclusionReason != null && EF.Functions.ILike(row.Detail.ExclusionReason, pattern)));
        }
        return query;
    }

    private static IOrderedQueryable<Row> ApplyStableOrdering(IQueryable<Row> query) => query
        .OrderBy(row => row.EmployeeCode ?? string.Empty).ThenBy(row => row.EmployeeName ?? string.Empty).ThenBy(row => row.Summary.Id);

    private static HazardAllowanceListItemDto ToDto(Row row) => new(
        row.Summary.Id, row.Summary.EmployeeId, row.EmployeeCode, row.EmployeeName, row.Summary.PayrollMonth, row.Summary.PayrollYear,
        row.Detail.QualifiedWorkdayCount, row.Detail.LateEarlyDeductionDays, row.Detail.PayableWorkdayCount,
        row.Detail.HazardAllowancePerDay, row.Detail.HazardAllowanceAmount, row.Detail.IsEligibleDepartment,
        row.Detail.ExclusionReason, row.Detail.IsLocked || row.Summary.IsLocked, row.Detail.CreatedAtUtc, row.Detail.CreatedBy,
        row.Detail.UpdatedAtUtc, row.Detail.UpdatedBy, row.Summary.UpdatedAtUtc)
    {
        IsEligibleForAllowance = row.Detail.IsEligibleForAllowance,
        DepartmentName = BuildDepartmentPath(row),
        PositionName = row.PositionName
    };

    private static string? BuildDepartmentPath(Row row)
    {
        var segments = new[]
        {
            HazardAllowancePersistence.NormalizeOptional(row.DepartmentCenterName),
            HazardAllowancePersistence.NormalizeOptional(row.DepartmentOrWorkshopName),
            HazardAllowancePersistence.NormalizeOptional(row.DepartmentTeamName),
            HazardAllowancePersistence.NormalizeOptional(row.DepartmentGroupName)
        }.Where(static segment => segment is not null).ToArray();
        return segments.Length == 0 ? null : string.Join(" / ", segments);
    }

    private sealed class Row
    {
        public PayrollAllowanceSummaryRecordRow Summary { get; init; } = default!;
        public PayrollHazardAllowanceRecordRow Detail { get; init; } = default!;
        public string? EmployeeCode { get; init; }
        public string? EmployeeName { get; init; }
        public string? DepartmentCenterName { get; init; }
        public string? DepartmentOrWorkshopName { get; init; }
        public string? DepartmentTeamName { get; init; }
        public string? DepartmentGroupName { get; init; }
        public string? PositionName { get; init; }
    }
}
