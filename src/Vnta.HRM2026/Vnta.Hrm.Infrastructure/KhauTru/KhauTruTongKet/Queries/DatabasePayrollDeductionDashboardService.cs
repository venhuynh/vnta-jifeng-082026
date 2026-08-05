using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

/// <summary>Truy vấn đọc tối ưu cho dashboard, lấy dữ liệu từ snapshot tổng kết khấu trừ.</summary>
public sealed class DatabasePayrollDeductionDashboardService(
    ApplicationDbContext dbContext,
    IPayrollDeductionSummaryRequestValidator requestValidator)
    : IPayrollDeductionDashboardService
{
    public async Task<PayrollDeductionDashboardDto> GetDashboardAsync(
        PayrollDeductionDashboardFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.ValidatePeriod(filter.PayrollYear, filter.PayrollMonth).ThrowIfInvalid();

        var currentPeriod = new PayrollPeriod((short)filter.PayrollYear, (short)filter.PayrollMonth);
        var previousPeriod = GetPreviousPeriod(currentPeriod);
        var currentRows = BuildPeriodQuery(currentPeriod);

        var overview = await GetOverviewAsync(currentRows, cancellationToken);
        var previousOverview = await GetOverviewAsync(BuildPeriodQuery(previousPeriod), cancellationToken);
        var breakdown = await GetBreakdownAsync(currentRows, cancellationToken);
        var trend = await GetTrendAsync(currentPeriod, cancellationToken);
        var monthlyComparison = await GetMonthlyComparisonAsync(currentPeriod, cancellationToken);
        var departmentMonthlyComparison = await GetDepartmentMonthlyComparisonAsync(currentPeriod, cancellationToken);

        return new PayrollDeductionDashboardDto(
            filter.PayrollMonth,
            filter.PayrollYear,
            overview,
            previousOverview,
            breakdown,
            trend,
            monthlyComparison,
            departmentMonthlyComparison);
    }

    private IQueryable<PayrollDeductionSummaryRecordRow> BuildPeriodQuery(PayrollPeriod period) =>
        dbContext.PayrollDeductionSummaryRecords
            .AsNoTracking()
            .Where(row => row.PayrollYear == period.Year && row.PayrollMonth == period.Month);

    private static async Task<PayrollDeductionDashboardOverviewDto> GetOverviewAsync(
        IQueryable<PayrollDeductionSummaryRecordRow> query,
        CancellationToken cancellationToken)
    {
        var result = await query
            .GroupBy(_ => 1)
            .Select(group => new PayrollDeductionDashboardOverviewDto(
                group.Count(),
                group.Sum(row => row.IsLocked ? 0 : 1),
                group.Sum(row => row.IsLocked ? 1 : 0),
                group.Sum(row => row.SocialInsuranceDeductionAmount
                    + row.PersonalIncomeTaxDeductionAmount
                    + row.UnionFeeDeductionAmount
                    + row.AdvanceDeductionAmount
                    + row.OtherDeductionAmount)))
            .SingleOrDefaultAsync(cancellationToken);

        return result ?? new PayrollDeductionDashboardOverviewDto(0, 0, 0, 0m);
    }

    private static async Task<IReadOnlyList<PayrollDeductionDashboardDeductionBreakdownDto>> GetBreakdownAsync(
        IQueryable<PayrollDeductionSummaryRecordRow> query,
        CancellationToken cancellationToken)
    {
        var totals = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Insurance = group.Sum(row => row.SocialInsuranceDeductionAmount),
                PersonalIncomeTax = group.Sum(row => row.PersonalIncomeTaxDeductionAmount),
                UnionFee = group.Sum(row => row.UnionFeeDeductionAmount),
                Advance = group.Sum(row => row.AdvanceDeductionAmount),
                Other = group.Sum(row => row.OtherDeductionAmount)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return
        [
            new("BHXH-YT", totals?.Insurance ?? 0m),
            new("Thuế TNCN", totals?.PersonalIncomeTax ?? 0m),
            new("Phí công đoàn", totals?.UnionFee ?? 0m),
            new("Tạm ứng", totals?.Advance ?? 0m),
            new("Khấu trừ khác", totals?.Other ?? 0m)
        ];
    }

    private async Task<IReadOnlyList<PayrollDeductionDashboardTrendPointDto>> GetTrendAsync(
        PayrollPeriod currentPeriod,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.PayrollDeductionSummaryRecords
            .AsNoTracking()
            .Where(row => row.PayrollYear == currentPeriod.Year && row.PayrollMonth <= currentPeriod.Month)
            .GroupBy(row => new { row.PayrollYear, row.PayrollMonth })
            .Select(group => new PayrollDeductionDashboardTrendPointDto(
                group.Key.PayrollMonth,
                group.Key.PayrollYear,
                group.Count(),
                group.Sum(row => row.SocialInsuranceDeductionAmount
                    + row.PersonalIncomeTaxDeductionAmount
                    + row.UnionFeeDeductionAmount
                    + row.AdvanceDeductionAmount
                    + row.OtherDeductionAmount)))
            .ToListAsync(cancellationToken);
        var rowsByPeriod = rows.ToDictionary(row => (row.PayrollYear, row.PayrollMonth));

        return Enumerable.Range(1, currentPeriod.Month)
            .Select(month => rowsByPeriod.GetValueOrDefault(
                (currentPeriod.Year, month),
                new PayrollDeductionDashboardTrendPointDto(month, currentPeriod.Year, 0, 0m)))
            .ToArray();
    }

    private async Task<IReadOnlyList<PayrollDeductionDashboardDeductionComparisonDto>> GetMonthlyComparisonAsync(
        PayrollPeriod currentPeriod,
        CancellationToken cancellationToken)
    {
        var rawTotals = await dbContext.PayrollDeductionSummaryRecords
            .AsNoTracking()
            .Where(row => row.PayrollYear == currentPeriod.Year && row.PayrollMonth <= currentPeriod.Month)
            .GroupBy(row => row.PayrollMonth)
            .Select(group => new
            {
                PayrollMonth = (int)group.Key,
                Insurance = group.Sum(row => row.SocialInsuranceDeductionAmount),
                PersonalIncomeTax = group.Sum(row => row.PersonalIncomeTaxDeductionAmount),
                UnionFee = group.Sum(row => row.UnionFeeDeductionAmount),
                Advance = group.Sum(row => row.AdvanceDeductionAmount),
                Other = group.Sum(row => row.OtherDeductionAmount)
            })
            .ToListAsync(cancellationToken);
        var totalsByMonth = rawTotals.ToDictionary(
            row => row.PayrollMonth,
            row => new MonthlyTotals(row.Insurance, row.PersonalIncomeTax, row.UnionFee, row.Advance, row.Other));

        IReadOnlyList<PayrollDeductionDashboardMonthDto> BuildMonths(Func<MonthlyTotals, decimal> amountSelector) =>
            Enumerable.Range(1, currentPeriod.Month)
                .Select(month => new PayrollDeductionDashboardMonthDto(
                    month,
                    totalsByMonth.TryGetValue(month, out var totals) ? amountSelector(totals) : 0m))
                .ToArray();

        return
        [
            new("BHXH-YT", BuildMonths(totals => totals.Insurance)),
            new("Thuế TNCN", BuildMonths(totals => totals.PersonalIncomeTax)),
            new("Phí công đoàn", BuildMonths(totals => totals.UnionFee)),
            new("Tạm ứng", BuildMonths(totals => totals.Advance)),
            new("Khấu trừ khác", BuildMonths(totals => totals.Other))
        ];
    }

    private async Task<IReadOnlyList<PayrollDeductionDashboardDepartmentTreeNodeDto>> GetDepartmentMonthlyComparisonAsync(
        PayrollPeriod currentPeriod,
        CancellationToken cancellationToken)
    {
        var departments = await dbContext.Departments
            .AsNoTracking()
            .Select(department => new
            {
                department.Id,
                department.CenterName,
                department.DepartmentOrWorkshopName,
                department.TeamName,
                department.GroupName
            })
            .ToListAsync(cancellationToken);

        var monthlyTotals = await (
            from summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            where summary.PayrollYear == currentPeriod.Year && summary.PayrollMonth <= currentPeriod.Month
            group summary by new
            {
                DepartmentId = employee == null ? (Guid?)null : employee.DepartmentId,
                summary.PayrollMonth
            } into departmentMonthGroup
            select new
            {
                departmentMonthGroup.Key.DepartmentId,
                PayrollMonth = (int)departmentMonthGroup.Key.PayrollMonth,
                Amount = departmentMonthGroup.Sum(row => row.SocialInsuranceDeductionAmount
                    + row.PersonalIncomeTaxDeductionAmount
                    + row.UnionFeeDeductionAmount
                    + row.AdvanceDeductionAmount
                    + row.OtherDeductionAmount)
            })
            .ToListAsync(cancellationToken);

        var amountsByDepartment = monthlyTotals
            .Where(row => row.DepartmentId.HasValue)
            .GroupBy(row => row.DepartmentId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(row => row.PayrollMonth, row => row.Amount));
        var nodes = new List<DepartmentTreeNodeAccumulator>();
        var nodeIndex = new Dictionary<string, DepartmentTreeNodeAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach(var department in departments)
        {
            var blockName = NormalizeNodeName(department.CenterName, "Chưa phân khối")!;
            var departmentName = NormalizeNodeName(department.DepartmentOrWorkshopName, "Chưa phân phòng ban")!;
            var teamName = NormalizeNodeName(department.TeamName, null);
            var groupName = NormalizeNodeName(department.GroupName, null);
            var blockNode = GetOrCreateNode(nodes, nodeIndex, BuildNodeId("block", blockName), null, blockName, 0);
            var departmentNode = GetOrCreateNode(nodes, nodeIndex, BuildNodeId("department", blockName, departmentName), blockNode.Id, departmentName, 1);
            var ancestors = new List<DepartmentTreeNodeAccumulator> { blockNode, departmentNode };
            var parentNode = departmentNode;

            if(teamName is not null)
            {
                var teamNode = GetOrCreateNode(nodes, nodeIndex, BuildNodeId("team", blockName, departmentName, teamName), parentNode.Id, teamName, 2);
                ancestors.Add(teamNode);
                parentNode = teamNode;
            }

            if(groupName is not null)
            {
                ancestors.Add(GetOrCreateNode(nodes, nodeIndex, BuildNodeId("group", blockName, departmentName, teamName, groupName), parentNode.Id, groupName, 3));
            }

            if(!amountsByDepartment.TryGetValue(department.Id, out var amountsByMonth))
            {
                continue;
            }

            foreach(var amountByMonth in amountsByMonth)
            {
                foreach(var ancestor in ancestors)
                {
                    ancestor.AmountByMonth[amountByMonth.Key] = ancestor.AmountByMonth.GetValueOrDefault(amountByMonth.Key) + amountByMonth.Value;
                }
            }
        }

        var unassignedAmounts = monthlyTotals.Where(row => !row.DepartmentId.HasValue);
        foreach(var amountByMonth in unassignedAmounts)
        {
            var node = GetOrCreateNode(nodes, nodeIndex, "unassigned", null, "Chưa phân phòng ban", 0);
            node.AmountByMonth[amountByMonth.PayrollMonth] = node.AmountByMonth.GetValueOrDefault(amountByMonth.PayrollMonth) + amountByMonth.Amount;
        }

        return nodes
            .OrderBy(node => node.HierarchyLevel)
            .ThenBy(node => node.DepartmentName, StringComparer.CurrentCulture)
            .Select(node => new PayrollDeductionDashboardDepartmentTreeNodeDto(
                node.DepartmentName,
                Enumerable.Range(1, currentPeriod.Month)
                    .Select(month => new PayrollDeductionDashboardMonthDto(month, node.AmountByMonth.GetValueOrDefault(month)))
                    .ToArray(),
                node.Id,
                node.ParentId,
                node.HierarchyLevel))
            .ToArray();
    }

    private static DepartmentTreeNodeAccumulator GetOrCreateNode(
        List<DepartmentTreeNodeAccumulator> nodes,
        Dictionary<string, DepartmentTreeNodeAccumulator> nodeIndex,
        string id,
        string? parentId,
        string departmentName,
        int hierarchyLevel)
    {
        if(nodeIndex.TryGetValue(id, out var existingNode))
        {
            return existingNode;
        }

        var node = new DepartmentTreeNodeAccumulator(id, parentId, departmentName, hierarchyLevel);
        nodes.Add(node);
        nodeIndex[id] = node;
        return node;
    }

    private static string BuildNodeId(string prefix, params string?[] values) =>
        $"{prefix}:{string.Join("|", values.Select(value => NormalizeNodeName(value, string.Empty)!.ToUpperInvariant()))}";

    private static string? NormalizeNodeName(string? value, string? fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static PayrollPeriod GetPreviousPeriod(PayrollPeriod period)
    {
        var previous = PayrollDeductionSummaryPeriodPolicy.Previous(period.Year, period.Month);
        return new PayrollPeriod(previous.Year, previous.Month);
    }

    // This adapter keeps dashboard's established localized validation messages while the validation rule is shared.
    private static void ValidatePeriod(int year, int month)
    {
        switch(PayrollDeductionSummaryPeriodPolicy.EvaluateRequired(year, month))
        {
            case PayrollDeductionSummaryPeriodValidationStatus.YearOutOfRange:
                throw new InvalidOperationException($"Năm dữ liệu phải nằm trong khoảng {PayrollDeductionSummaryPeriodPolicy.MinimumSupportedYear} đến {PayrollDeductionSummaryPeriodPolicy.MaximumSupportedYear}.");
            case PayrollDeductionSummaryPeriodValidationStatus.MonthOutOfRange:
                throw new InvalidOperationException("Tháng dữ liệu phải nằm trong khoảng 1 đến 12.");
            case PayrollDeductionSummaryPeriodValidationStatus.BeforeFirstSupportedMonth:
                throw new InvalidOperationException($"Mốc dữ liệu tổng kết khấu trừ bắt đầu từ {PayrollDeductionSummaryPeriodPolicy.MinimumSupportedMonth:00}/{PayrollDeductionSummaryPeriodPolicy.MinimumSupportedYear}.");
        }
    }

    private readonly record struct PayrollPeriod(short Year, short Month);

    private readonly record struct MonthlyTotals(
        decimal Insurance,
        decimal PersonalIncomeTax,
        decimal UnionFee,
        decimal Advance,
        decimal Other);

    private sealed class DepartmentTreeNodeAccumulator(string id, string? parentId, string departmentName, int hierarchyLevel)
    {
        public string Id { get; } = id;
        public string? ParentId { get; } = parentId;
        public string DepartmentName { get; } = departmentName;
        public int HierarchyLevel { get; } = hierarchyLevel;
        public Dictionary<int, decimal> AmountByMonth { get; } = [];
    }
}
