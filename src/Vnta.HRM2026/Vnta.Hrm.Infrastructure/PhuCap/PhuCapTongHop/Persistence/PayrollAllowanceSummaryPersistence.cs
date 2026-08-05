using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapDashboard.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

/// <summary>
/// Triển khai persistence cho snapshot tổng hợp phụ cấp.
/// Dịch vụ hợp nhất các bảng phụ cấp thành phần theo nhân viên/kỳ lương và bảo toàn trạng thái khóa,
/// phiên bản đồng thời cùng dấu vết audit cho các thao tác cần ghi nhận.
/// </summary>
/// <summary>
/// EF persistence primitives shared by allowance-summary use cases. Public contracts
/// are implemented by the focused services in Queries and Commands.
/// </summary>
internal sealed class PayrollAllowanceSummaryPersistence(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    ILogger<PayrollAllowanceSummaryPersistence>? logger = null)
{
    private const int MinimumSupportedYear = 2026;
    private const int MinimumSupportedMonth = 6;
    private const int DefaultPageSize = 50;
    private const int MaximumPageSize = 200;
    private const int MaximumSupportedYear = 2100;

    #region Truy vấn và xuất dữ liệu

    public async Task<PayrollAllowanceSummaryOverviewDto> GetSummaryAsync(
        PayrollAllowanceSummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = BuildSearchQuery(filter);

        var result = await query
            .Select(x => x.Summary)
            .GroupBy(_ => 1)
            .Select(group => new PayrollAllowanceSummaryOverviewDto(
                group.Count(),
                group.Sum(row => row.IsLocked ? 0 : 1),
                group.Sum(row => row.IsLocked ? 1 : 0),
                group.Sum(row =>
                    row.ResponsibilityAllowanceAmount
                    + row.ResponsibilityOtherAllowanceAmount
                    + row.SeniorityAllowanceAmount
                    + row.AttendanceAllowanceAmount
                    + row.MealAllowanceAmount
                    + row.HazardAllowanceAmount
                    + row.OtherAllowanceAmount
                    + row.LeaveHolidayAllowanceAmount)))
            .SingleOrDefaultAsync(cancellationToken);

        return result ?? new PayrollAllowanceSummaryOverviewDto(0, 0, 0, 0);
    }

    public async Task<PayrollAllowanceDashboardDto> GetDashboardAsync(
        PayrollAllowanceDashboardFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PayrollAllowanceDashboardPeriodPolicy.Validate(filter);

        var currentPeriod = new PayrollPeriod((short)filter.PayrollYear, (short)filter.PayrollMonth);
        var previousPeriod = GetPreviousPayrollPeriod(currentPeriod.Year, currentPeriod.Month);
        // Dashboard comparisons follow the selected payroll period. The UI contract
        // requires a January-to-selected-month series, including for past months.
        var comparisonPeriod = currentPeriod;
        var currentRows = BuildDashboardPeriodQuery(currentPeriod);
        var previousRows = BuildDashboardPeriodQuery(previousPeriod);

        var overview = await GetDashboardOverviewAsync(currentRows, cancellationToken);
        var previousOverview = await GetDashboardOverviewAsync(previousRows, cancellationToken);
        var breakdown = await GetAllowanceBreakdownAsync(currentRows, cancellationToken);
        // Xu hướng luôn bao phủ từ tháng 01 đến kỳ được chọn trên toolbar.
        var trend = await GetDashboardTrendAsync(comparisonPeriod, cancellationToken);
        // DepartmentMonthlyComparison is the canonical full hierarchy consumed by
        // the dashboard. TopDepartments remains in the DTO for wire compatibility.
        var departments = Array.Empty<PayrollAllowanceDashboardDepartmentDto>();
        var monthlyComparison = await GetAllowanceMonthlyComparisonAsync(comparisonPeriod, cancellationToken);
        var departmentMonthlyComparison = await GetDepartmentMonthlyComparisonAsync(
            comparisonPeriod,
            cancellationToken);

        return new PayrollAllowanceDashboardDto(
            filter.PayrollMonth,
            filter.PayrollYear,
            overview,
            previousOverview,
            breakdown,
            trend,
            departments,
            monthlyComparison,
            departmentMonthlyComparison);
    }

    public async Task<PayrollAllowanceSummaryPageDto> SearchAsync(
        PayrollAllowanceSummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var skip = Math.Max(0, filter.Skip);
        var take = NormalizePageSize(filter.Take);
        var query = BuildSearchQuery(filter);
        var totalCount = await query.CountAsync(cancellationToken);
        if(totalCount == 0 || skip >= totalCount)
        {
            return new PayrollAllowanceSummaryPageDto([], totalCount);
        }

        var rows = await query
            .OrderByDescending(x => x.Summary.PayrollYear)
            .ThenByDescending(x => x.Summary.PayrollMonth)
            .ThenBy(x => x.Employee == null ? string.Empty : x.Employee.EmployeeCode)
            .ThenBy(x => x.Employee == null ? string.Empty : x.Employee.LastName)
            .ThenBy(x => x.Employee == null ? string.Empty : x.Employee.FirstName)
            .ThenBy(x => x.Summary.Id)
            .Skip(skip)
            .Take(take)
            .Select(x => MapToDto(x.Summary, x.Employee, x.Department, x.Position))
            .ToListAsync(cancellationToken);

        return new PayrollAllowanceSummaryPageDto(rows, totalCount);
    }

    public async Task<IReadOnlyList<PayrollAllowanceSummaryExportRowDto>> ExportAsync(
        PayrollAllowanceSummaryExportRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredPeriod(request.PayrollYear, request.PayrollMonth);
        ValidateExportFormat(request.Format);

        var auditCommand = auditScope.Current ?? new AuditCommand(
            Guid.NewGuid(),
            AuditActions.AllowanceSummary.Exported,
            new AuditActor("system", "system", AuditActorKind.System, AuditSource.Worker),
            Guid.NewGuid().ToString("N"),
            AuditCaptureMode.OperationOnly,
            Metadata: new Dictionary<string, string>
            {
                ["auditScope"] = "system-fallback"
            });

        return await auditedMutation.ExecuteAsync(
            auditCommand with { ActionIntent = AuditActions.AllowanceSummary.Exported },
            async token =>
            {
                var sourceRows = await BuildSearchQuery(new PayrollAllowanceSummaryFilter(
                        request.PayrollMonth,
                        request.PayrollYear,
                        SearchText: null,
                        IsLocked: null,
                        Skip: 0,
                        Take: int.MaxValue))
                    .OrderBy(x => x.Employee == null ? string.Empty : x.Employee.EmployeeCode)
                    .ThenBy(x => x.Employee == null ? string.Empty : x.Employee.LastName)
                    .ThenBy(x => x.Employee == null ? string.Empty : x.Employee.FirstName)
                    .ThenBy(x => x.Summary.Id)
                    .Select(x => MapToDto(x.Summary, x.Employee, x.Department, x.Position))
                    .ToListAsync(token);

                var rows = sourceRows
                    .Select(MapToExportRow)
                    .ToArray();

                return (IReadOnlyList<PayrollAllowanceSummaryExportRowDto>)rows;
            },
            rows => new AuditOperationEvent(
                AuditActions.AllowanceSummary.Exported,
                AuditEntityTypes.AllowanceSummary,
                EntityDisplayName: $"{request.PayrollMonth:00}/{request.PayrollYear}",
                Outcome: rows.Count == 0
                    ? AuditOperationOutcome.NoChanges
                    : AuditOperationOutcome.Succeeded,
                Metadata: new Dictionary<string, string>
                {
                    ["format"] = request.Format.ToString(),
                    ["scope"] = "wholePeriod",
                    ["payrollPeriod"] = $"{request.PayrollMonth:00}/{request.PayrollYear}",
                    ["rowCount"] = rows.Count.ToString()
                }),
            cancellationToken);
    }

    private IQueryable<PayrollAllowanceSummarySearchProjection> BuildSearchQuery(
        PayrollAllowanceSummaryFilter filter)
    {
        ValidateSearchPeriod(filter.PayrollYear, filter.PayrollMonth);

        var normalizedSearchText = NormalizeOptional(filter.SearchText);
        var query =
            from summary in PayrollAllowanceSummaryPopulationQuery.All(dbContext)
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new PayrollAllowanceSummarySearchProjection
            {
                Summary = summary,
                Employee = employee,
                Department = department,
                Position = position
            };

        if(filter.PayrollMonth.HasValue)
        {
            var payrollMonth = (short)filter.PayrollMonth.Value;
            query = query.Where(x => x.Summary.PayrollMonth == payrollMonth);
        }

        if(filter.PayrollYear.HasValue)
        {
            var payrollYear = (short)filter.PayrollYear.Value;
            query = query.Where(x => x.Summary.PayrollYear == payrollYear);
        }

        if(filter.IsLocked.HasValue)
        {
            query = query.Where(x => x.Summary.IsLocked == filter.IsLocked.Value);
        }

        if(string.IsNullOrWhiteSpace(normalizedSearchText))
        {
            return query;
        }

        var searchPattern = $"%{normalizedSearchText}%";
        return query.Where(x =>
            (x.Employee != null && EF.Functions.ILike(x.Employee.EmployeeCode, searchPattern))
            || (x.Employee != null && EF.Functions.ILike(x.Employee.FirstName, searchPattern))
            || (x.Employee != null && EF.Functions.ILike(x.Employee.LastName, searchPattern))
            || (x.Department != null && EF.Functions.ILike(x.Department.DepartmentOrWorkshopName, searchPattern))
            || (x.Department != null && x.Department.TeamName != null && EF.Functions.ILike(x.Department.TeamName, searchPattern))
            || (x.Department != null && x.Department.GroupName != null && EF.Functions.ILike(x.Department.GroupName, searchPattern))
            || (x.Position != null && EF.Functions.ILike(x.Position.Name, searchPattern))
            || (x.Summary.Note != null && EF.Functions.ILike(x.Summary.Note, searchPattern)));
    }

    private IQueryable<PayrollAllowanceSummaryRecordRow> BuildDashboardPeriodQuery(PayrollPeriod period) =>
        dbContext.PayrollAllowanceSummaryRecords
            .AsNoTracking()
            .Where(row => row.PayrollYear == period.Year && row.PayrollMonth == period.Month);

    public Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceBreakdownDto>> GetAllowanceBreakdownAsync(
        PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default)
    {
        PayrollAllowanceDashboardPeriodPolicy.Validate(filter);
        return GetAllowanceBreakdownAsync(
            BuildDashboardPeriodQuery(new PayrollPeriod((short)filter.PayrollYear, (short)filter.PayrollMonth)),
            cancellationToken);
    }

    public Task<IReadOnlyList<PayrollAllowanceDashboardTrendPointDto>> GetTrendAsync(
        PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default)
    {
        PayrollAllowanceDashboardPeriodPolicy.Validate(filter);
        return GetDashboardTrendAsync(new PayrollPeriod((short)filter.PayrollYear, (short)filter.PayrollMonth), cancellationToken);
    }

    public Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceComparisonDto>> GetAllowanceMonthlyComparisonAsync(
        PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default)
    {
        PayrollAllowanceDashboardPeriodPolicy.Validate(filter);
        return GetAllowanceMonthlyComparisonAsync(new PayrollPeriod((short)filter.PayrollYear, (short)filter.PayrollMonth), cancellationToken);
    }

    public Task<IReadOnlyList<PayrollAllowanceDashboardDepartmentTreeNodeDto>> GetDepartmentMonthlyComparisonAsync(
        PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default)
    {
        PayrollAllowanceDashboardPeriodPolicy.Validate(filter);
        return GetDepartmentMonthlyComparisonAsync(new PayrollPeriod((short)filter.PayrollYear, (short)filter.PayrollMonth), cancellationToken);
    }

    private static async Task<PayrollAllowanceDashboardOverviewDto> GetDashboardOverviewAsync(
        IQueryable<PayrollAllowanceSummaryRecordRow> query,
        CancellationToken cancellationToken)
    {
        var result = await query
            .GroupBy(_ => 1)
            .Select(group => new PayrollAllowanceDashboardOverviewDto(
                group.Count(),
                group.Sum(row => row.IsLocked ? 0 : 1),
                group.Sum(row => row.IsLocked ? 1 : 0),
                group.Sum(row =>
                    row.ResponsibilityAllowanceAmount
                    + row.ResponsibilityOtherAllowanceAmount
                    + row.SeniorityAllowanceAmount
                    + row.AttendanceAllowanceAmount
                    + row.MealAllowanceAmount
                    + row.HazardAllowanceAmount
                    + row.OtherAllowanceAmount
                    + row.LeaveHolidayAllowanceAmount)))
            .SingleOrDefaultAsync(cancellationToken);

        return result ?? new PayrollAllowanceDashboardOverviewDto(0, 0, 0, 0);
    }

    private static async Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceBreakdownDto>> GetAllowanceBreakdownAsync(
        IQueryable<PayrollAllowanceSummaryRecordRow> query,
        CancellationToken cancellationToken)
    {
        var totals = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Responsibility = group.Sum(row => row.ResponsibilityAllowanceAmount),
                ResponsibilityOther = group.Sum(row => row.ResponsibilityOtherAllowanceAmount),
                Seniority = group.Sum(row => row.SeniorityAllowanceAmount),
                Attendance = group.Sum(row => row.AttendanceAllowanceAmount),
                Meal = group.Sum(row => row.MealAllowanceAmount),
                Hazard = group.Sum(row => row.HazardAllowanceAmount),
                Other = group.Sum(row => row.OtherAllowanceAmount),
                LeaveHoliday = group.Sum(row => row.LeaveHolidayAllowanceAmount)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return
        [
            new("Trách nhiệm", totals?.Responsibility ?? 0m),
            new("Trách nhiệm khác", totals?.ResponsibilityOther ?? 0m),
            new("Thâm niên", totals?.Seniority ?? 0m),
            new("Chuyên cần", totals?.Attendance ?? 0m),
            new("Cơm", totals?.Meal ?? 0m),
            new("Độc hại", totals?.Hazard ?? 0m),
            new("Khác", totals?.Other ?? 0m),
            new("Phép/lễ", totals?.LeaveHoliday ?? 0m)
        ];
    }

    private async Task<IReadOnlyList<PayrollAllowanceDashboardTrendPointDto>> GetDashboardTrendAsync(
        PayrollPeriod currentPeriod,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.PayrollAllowanceSummaryRecords
            .AsNoTracking()
            .Where(row => row.PayrollYear == currentPeriod.Year && row.PayrollMonth <= currentPeriod.Month)
            .GroupBy(row => new { row.PayrollYear, row.PayrollMonth })
            .Select(group => new PayrollAllowanceDashboardTrendPointDto(
                group.Key.PayrollMonth,
                group.Key.PayrollYear,
                group.Count(),
                group.Sum(row =>
                    row.ResponsibilityAllowanceAmount
                    + row.ResponsibilityOtherAllowanceAmount
                    + row.SeniorityAllowanceAmount
                    + row.AttendanceAllowanceAmount
                    + row.MealAllowanceAmount
                    + row.HazardAllowanceAmount
                    + row.OtherAllowanceAmount
                    + row.LeaveHolidayAllowanceAmount)))
            .ToListAsync(cancellationToken);
        var rowsByPeriod = rows.ToDictionary(row => (row.PayrollYear, row.PayrollMonth));
        var periods = GetDashboardPeriods(currentPeriod);

        return periods
            .Select(period => rowsByPeriod.GetValueOrDefault(
                (period.Year, period.Month),
                new PayrollAllowanceDashboardTrendPointDto(period.Month, period.Year, 0, 0m)))
            .ToArray();
    }

    private async Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceComparisonDto>> GetAllowanceMonthlyComparisonAsync(
        PayrollPeriod currentPeriod,
        CancellationToken cancellationToken)
    {
        var rawTotals = await dbContext.PayrollAllowanceSummaryRecords
            .AsNoTracking()
            .Where(row => row.PayrollYear == currentPeriod.Year && row.PayrollMonth <= currentPeriod.Month)
            .GroupBy(row => row.PayrollMonth)
            .Select(group => new
            {
                PayrollMonth = (int)group.Key,
                Responsibility = group.Sum(row => row.ResponsibilityAllowanceAmount),
                ResponsibilityOther = group.Sum(row => row.ResponsibilityOtherAllowanceAmount),
                Seniority = group.Sum(row => row.SeniorityAllowanceAmount),
                Attendance = group.Sum(row => row.AttendanceAllowanceAmount),
                Meal = group.Sum(row => row.MealAllowanceAmount),
                Hazard = group.Sum(row => row.HazardAllowanceAmount),
                Other = group.Sum(row => row.OtherAllowanceAmount),
                LeaveHoliday = group.Sum(row => row.LeaveHolidayAllowanceAmount)
            })
            .ToListAsync(cancellationToken);
        var totalsByMonth = rawTotals.ToDictionary(
            row => row.PayrollMonth,
            row => new AllowanceMonthlyTotals(
                row.Responsibility,
                row.ResponsibilityOther,
                row.Seniority,
                row.Attendance,
                row.Meal,
                row.Hazard,
                row.Other,
                row.LeaveHoliday));

        IReadOnlyList<PayrollAllowanceDashboardAllowanceMonthDto> BuildMonths(
            Func<AllowanceMonthlyTotals, decimal> amountSelector)
        {
            return Enumerable.Range(1, currentPeriod.Month)
                .Select(month => new PayrollAllowanceDashboardAllowanceMonthDto(
                    month,
                    totalsByMonth.TryGetValue(month, out var totals)
                        ? amountSelector(totals)
                        : 0m))
                .ToArray();
        }

        return
        [
            new("Trách nhiệm", BuildMonths(totals => totals.Responsibility)),
            new("Trách nhiệm khác", BuildMonths(totals => totals.ResponsibilityOther)),
            new("Thâm niên", BuildMonths(totals => totals.Seniority)),
            new("Chuyên cần", BuildMonths(totals => totals.Attendance)),
            new("Cơm", BuildMonths(totals => totals.Meal)),
            new("Độc hại", BuildMonths(totals => totals.Hazard)),
            new("Khác", BuildMonths(totals => totals.Other)),
            new("Phép/lễ", BuildMonths(totals => totals.LeaveHoliday))
        ];
    }

    private async Task<IReadOnlyList<PayrollAllowanceDashboardDepartmentTreeNodeDto>> GetDepartmentMonthlyComparisonAsync(
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
            from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            where summary.PayrollYear == currentPeriod.Year
                && summary.PayrollMonth <= currentPeriod.Month
            group summary by new
            {
                DepartmentId = employee == null ? (Guid?)null : employee.DepartmentId,
                summary.PayrollMonth
            } into departmentMonthGroup
            select new
            {
                departmentMonthGroup.Key.DepartmentId,
                PayrollMonth = (int)departmentMonthGroup.Key.PayrollMonth,
                Amount = departmentMonthGroup.Sum(row =>
                    row.ResponsibilityAllowanceAmount
                    + row.ResponsibilityOtherAllowanceAmount
                    + row.SeniorityAllowanceAmount
                    + row.AttendanceAllowanceAmount
                    + row.MealAllowanceAmount
                    + row.HazardAllowanceAmount
                    + row.OtherAllowanceAmount
                    + row.LeaveHolidayAllowanceAmount)
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
            var blockName = NormalizeDepartmentNodeName(department.CenterName, "Chưa phân khối")!;
            var departmentName = NormalizeDepartmentNodeName(department.DepartmentOrWorkshopName, "Chưa phân phòng ban")!;
            var teamName = NormalizeDepartmentNodeName(department.TeamName, null);
            var groupName = NormalizeDepartmentNodeName(department.GroupName, null);

            var blockNode = GetOrCreateDepartmentTreeNode(
                nodes,
                nodeIndex,
                BuildDepartmentTreeNodeId("block", blockName),
                parentId: null,
                blockName,
                hierarchyLevel: 0);
            var departmentNode = GetOrCreateDepartmentTreeNode(
                nodes,
                nodeIndex,
                BuildDepartmentTreeNodeId("department", blockName, departmentName),
                blockNode.Id,
                departmentName,
                hierarchyLevel: 1);
            var ancestors = new List<DepartmentTreeNodeAccumulator> { blockNode, departmentNode };
            var parentNode = departmentNode;

            if(teamName is not null)
            {
                var teamNode = GetOrCreateDepartmentTreeNode(
                    nodes,
                    nodeIndex,
                    BuildDepartmentTreeNodeId("team", blockName, departmentName, teamName),
                    parentNode.Id,
                    teamName,
                    hierarchyLevel: 2);
                ancestors.Add(teamNode);
                parentNode = teamNode;
            }

            if(groupName is not null)
            {
                var groupNode = GetOrCreateDepartmentTreeNode(
                    nodes,
                    nodeIndex,
                    BuildDepartmentTreeNodeId("group", blockName, departmentName, teamName, groupName),
                    parentNode.Id,
                    groupName,
                    hierarchyLevel: 3);
                ancestors.Add(groupNode);
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

        var unassignedAmounts = monthlyTotals.Where(row => !row.DepartmentId.HasValue).ToArray();
        if(unassignedAmounts.Length > 0)
        {
            var unassignedNode = GetOrCreateDepartmentTreeNode(
                nodes,
                nodeIndex,
                "unassigned",
                parentId: null,
                "Chưa phân phòng ban",
                hierarchyLevel: 0);
            foreach(var amountByMonth in unassignedAmounts)
            {
                unassignedNode.AmountByMonth[amountByMonth.PayrollMonth] =
                    unassignedNode.AmountByMonth.GetValueOrDefault(amountByMonth.PayrollMonth) + amountByMonth.Amount;
            }
        }

        return nodes
            .OrderBy(node => node.HierarchyLevel)
            .ThenBy(node => node.DepartmentName, StringComparer.CurrentCulture)
            .Select(node => new PayrollAllowanceDashboardDepartmentTreeNodeDto(
                node.DepartmentName,
                Enumerable.Range(1, currentPeriod.Month)
                    .Select(month => new PayrollAllowanceDashboardAllowanceMonthDto(
                        month,
                        node.AmountByMonth.GetValueOrDefault(month)))
                    .ToArray(),
                node.Id,
                node.ParentId,
                node.HierarchyLevel))
            .ToArray();
    }

    private static DepartmentTreeNodeAccumulator GetOrCreateDepartmentTreeNode(
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

    private static string BuildDepartmentTreeNodeId(string prefix, params string?[] values) =>
        $"{prefix}:{string.Join("|", values.Select(value => NormalizeDepartmentNodeName(value, string.Empty)!.ToUpperInvariant()))}";

    private static string? NormalizeDepartmentNodeName(string? value, string? fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private sealed class DepartmentTreeNodeAccumulator(
        string id,
        string? parentId,
        string departmentName,
        int hierarchyLevel)
    {
        public string Id { get; } = id;
        public string? ParentId { get; } = parentId;
        public string DepartmentName { get; } = departmentName;
        public int HierarchyLevel { get; } = hierarchyLevel;
        public Dictionary<int, decimal> AmountByMonth { get; } = [];
    }

    #endregion

    #region Đồng bộ từ kỳ trước

    /// <summary>
    /// Lấy tập nhân viên có chấm công trong kỳ đích làm nguồn chuẩn, rồi sao chép snapshot từ kỳ trước.
    /// Các dòng đã khóa được giữ nguyên; các dòng không còn chấm công cùng dữ liệu phụ thuộc sẽ bị loại bỏ.
    /// </summary>
    public async Task<SyncPayrollAllowanceSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncPayrollAllowanceSummaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await SyncFromPreviousMonthCoreAsync(request, cancellationToken);
        }
        catch(Exception ex) when(ex is not OperationCanceledException)
        {
            logger?.LogError(
                ex,
                "Allowance summary previous-month sync failed for target {TargetPayrollMonth}/{TargetPayrollYear}. Inspect the exception stack trace for the failing persistence operation.",
                request.TargetPayrollMonth,
                request.TargetPayrollYear);
            throw;
        }
    }

    private async Task<SyncPayrollAllowanceSummaryFromPreviousMonthResult> SyncFromPreviousMonthCoreAsync(
        SyncPayrollAllowanceSummaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredPeriod(request.TargetPayrollYear, request.TargetPayrollMonth);

        var actor = NormalizeActor(request.Actor);
        var targetPayrollMonth = (short)request.TargetPayrollMonth;
        var targetPayrollYear = (short)request.TargetPayrollYear;
        var sourcePeriod = GetPreviousPayrollPeriod(targetPayrollYear, targetPayrollMonth);

        var targetPeriodStart = new DateOnly(targetPayrollYear, targetPayrollMonth, 1);
        var targetPeriodEnd = new DateOnly(
            targetPayrollYear,
            targetPayrollMonth,
            DateTime.DaysInMonth(targetPayrollYear, targetPayrollMonth));
        var attendanceEmployeeIds = await dbContext.AttendanceWorkdaySummaries
            .AsNoTracking()
            .Where(row => row.WorkDate >= targetPeriodStart && row.WorkDate <= targetPeriodEnd)
            .Select(row => row.EmployeeId)
            .Distinct()
            .OrderBy(id => id)
            .ToArrayAsync(cancellationToken);
        var attendanceEmployeeIdSet = attendanceEmployeeIds.ToHashSet();

        var sourceRows = await dbContext.PayrollAllowanceSummaryRecords
            .AsNoTracking()
            .Where(row => row.PayrollYear == sourcePeriod.Year && row.PayrollMonth == sourcePeriod.Month)
            .OrderBy(row => row.EmployeeId)
            .ToListAsync(cancellationToken);
        var sourceRowsByEmployeeId = sourceRows
            .GroupBy(row => row.EmployeeId)
            .ToDictionary(group => group.Key, group => group.First());

        var targetRows = await dbContext.PayrollAllowanceSummaryRecords
            .Where(row =>
                row.PayrollYear == targetPayrollYear
                && row.PayrollMonth == targetPayrollMonth)
            .ToListAsync(cancellationToken);
        var now = GetDatabaseNow();
        var createdCount = 0;
        var updatedCount = 0;
        var skippedLockedCount = 0;
        var removedCount = 0;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Tập nhân viên có chấm công là nguồn chuẩn cho kỳ đích, không phải snapshot của kỳ trước.
        var obsoleteRows = targetRows
            .Where(row => !attendanceEmployeeIdSet.Contains(row.EmployeeId))
            .ToArray();
        if(obsoleteRows.Length > 0)
        {
            await RemoveDependentAllowanceRowsAsync(
                obsoleteRows.Select(row => row.Id).ToArray(),
                cancellationToken);
            dbContext.PayrollAllowanceSummaryRecords.RemoveRange(obsoleteRows);
            targetRows = targetRows.Except(obsoleteRows).ToList();
            removedCount += obsoleteRows.Length;
        }

        var targetRowsByEmployeeId = targetRows.ToDictionary(row => row.EmployeeId);
        foreach(var employeeId in attendanceEmployeeIds)
        {
            sourceRowsByEmployeeId.TryGetValue(employeeId, out var sourceRow);
            if(targetRowsByEmployeeId.TryGetValue(employeeId, out var targetRow))
            {
                if(targetRow.IsLocked)
                {
                    skippedLockedCount++;
                    continue;
                }

                if(sourceRow is not null)
                {
                    ApplySummaryValues(sourceRow, targetRow, targetPayrollMonth, targetPayrollYear, actor, now);
                    updatedCount++;
                }

                continue;
            }

            var newRow = sourceRow is null
                ? CreateEmptySummaryRow(employeeId, targetPayrollYear, targetPayrollMonth, actor, now)
                : CreateSummaryRowFromSource(sourceRow, targetPayrollMonth, targetPayrollYear, actor, now);
            dbContext.PayrollAllowanceSummaryRecords.Add(newRow);
            targetRows.Add(newRow);
            targetRowsByEmployeeId[employeeId] = newRow;
            createdCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureDependentAllowanceRowsAsync(
            targetRows,
            sourceRowsByEmployeeId,
            targetPayrollYear,
            targetPayrollMonth,
            actor,
            now,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SyncPayrollAllowanceSummaryFromPreviousMonthResult(
            sourcePeriod.Month,
            sourcePeriod.Year,
            targetPayrollMonth,
            targetPayrollYear,
            sourceRowsByEmployeeId.Keys.Count(attendanceEmployeeIdSet.Contains),
            createdCount,
            updatedCount,
            skippedLockedCount,
            attendanceEmployeeIds.Length,
            removedCount);
    }

    #endregion

    #region Làm mới từ các khoản phụ cấp nguồn

    /// <summary>
    /// Cộng lại từng nguồn phụ cấp theo nhân viên và cập nhật snapshot chưa khóa.
    /// Thao tác có thể giới hạn ở một dòng để phục vụ lệnh làm mới tại grid.
    /// </summary>
    public async Task<RefreshPayrollAllowanceSummaryResult> RefreshAsync(
        RefreshPayrollAllowanceSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredPeriod(request.TargetPayrollYear, request.TargetPayrollMonth);

        var actor = NormalizeActor(request.Actor);
        var targetPayrollMonth = (short)request.TargetPayrollMonth;
        var targetPayrollYear = (short)request.TargetPayrollYear;

        var responsibilitySourceByEmployeeId = await (
                from abc in dbContext.PayrollResponsibilityAllowanceAbcRows.AsNoTracking()
                join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                    on abc.PayrollAllowanceSummaryRecordId equals summary.Id
                where abc.Year == targetPayrollYear
                    && abc.Month == targetPayrollMonth
                    && summary.PayrollYear == targetPayrollYear
                    && summary.PayrollMonth == targetPayrollMonth
                group abc by summary.EmployeeId
                into employeeGroup
                select new
                {
                    EmployeeId = employeeGroup.Key,
                    Amount = employeeGroup.Sum(row => row.ActualResponsibilityAllowanceAmount)
                })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Amount, cancellationToken);

        var senioritySourceByEmployeeId = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                join detail in dbContext.PayrollEmployeeSeniorityAllowances.AsNoTracking()
                    on summary.Id equals detail.PayrollAllowanceSummaryRecordId
                where summary.PayrollYear == targetPayrollYear
                    && summary.PayrollMonth == targetPayrollMonth
                group detail by summary.EmployeeId
                into employeeGroup
                select new
                {
                    EmployeeId = employeeGroup.Key,
                    Amount = employeeGroup.Sum(row => row.AllowanceAmount)
                })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Amount, cancellationToken);

        var attendanceSourceByEmployeeId = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                join detail in dbContext.PayrollAttendanceAllowanceRecords.AsNoTracking()
                    on summary.Id equals detail.PayrollAllowanceSummaryRecordId
                where summary.PayrollYear == targetPayrollYear
                    && summary.PayrollMonth == targetPayrollMonth
                group detail by summary.EmployeeId
                into employeeGroup
                select new
                {
                    EmployeeId = employeeGroup.Key,
                    Amount = employeeGroup.Sum(row => row.AllowanceAmount)
                })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Amount, cancellationToken);

        var mealSourceByEmployeeId = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                join detail in dbContext.PayrollMealAllowanceRecords.AsNoTracking()
                    on summary.Id equals detail.PayrollAllowanceSummaryRecordId
                where summary.PayrollYear == targetPayrollYear
                    && summary.PayrollMonth == targetPayrollMonth
                group detail by summary.EmployeeId
                into employeeGroup
                select new
                {
                    EmployeeId = employeeGroup.Key,
                    Amount = employeeGroup.Sum(row => row.MealAllowanceAmount)
                })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Amount, cancellationToken);

        var hazardSourceByEmployeeId = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                join detail in dbContext.PayrollHazardAllowanceRecords.AsNoTracking()
                    on summary.Id equals detail.PayrollAllowanceSummaryRecordId
                where summary.PayrollYear == targetPayrollYear
                    && summary.PayrollMonth == targetPayrollMonth
                group detail by summary.EmployeeId
                into employeeGroup
                select new
                {
                    EmployeeId = employeeGroup.Key,
                    Amount = employeeGroup.Sum(row => row.HazardAllowanceAmount)
                })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Amount, cancellationToken);

        var otherResponsibilitySourceByEmployeeId = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                join detail in dbContext.PayrollAllowanceOtherResponsibilityRecords.AsNoTracking()
                    on summary.Id equals detail.PayrollAllowanceSummaryRecordId
                where summary.PayrollYear == targetPayrollYear
                    && summary.PayrollMonth == targetPayrollMonth
                group detail by summary.EmployeeId
                into employeeGroup
                select new
                {
                    EmployeeId = employeeGroup.Key,
                    Amount = employeeGroup.Sum(row => row.ActualResponsibilityAllowanceAmount)
                })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Amount, cancellationToken);

        var otherAllowanceSourceByEmployeeId = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                join detail in dbContext.PayrollOtherAllowanceRecords.AsNoTracking()
                    on summary.Id equals detail.PayrollAllowanceSummaryRecordId
                where summary.PayrollYear == targetPayrollYear
                    && summary.PayrollMonth == targetPayrollMonth
                group detail by summary.EmployeeId
                into employeeGroup
                select new
                {
                    EmployeeId = employeeGroup.Key,
                    Amount = employeeGroup.Sum(row => row.AllowanceAmount)
                })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Amount, cancellationToken);

        var leaveHolidaySourceByEmployeeId = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                join detail in dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.AsNoTracking()
                    on summary.Id equals detail.PayrollAllowanceSummaryRecordId
                where summary.PayrollYear == targetPayrollYear
                    && summary.PayrollMonth == targetPayrollMonth
                group detail by summary.EmployeeId
                into employeeGroup
                select new
                {
                    EmployeeId = employeeGroup.Key,
                    Amount = employeeGroup.Sum(row => row.LeaveHolidayAllowanceAmount)
                })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Amount, cancellationToken);

        var sourceEmployeeIds = responsibilitySourceByEmployeeId.Keys
            .Concat(senioritySourceByEmployeeId.Keys)
            .Concat(attendanceSourceByEmployeeId.Keys)
            .Concat(mealSourceByEmployeeId.Keys)
            .Concat(hazardSourceByEmployeeId.Keys)
            .Concat(otherAllowanceSourceByEmployeeId.Keys)
            .Concat(otherResponsibilitySourceByEmployeeId.Keys)
            .Concat(leaveHolidaySourceByEmployeeId.Keys)
            .Distinct()
            .ToArray();

        var targetRowsQuery = dbContext.PayrollAllowanceSummaryRecords
            .Where(row => row.PayrollYear == targetPayrollYear && row.PayrollMonth == targetPayrollMonth);

        if(request.PayrollAllowanceSummaryRecordId.HasValue)
        {
            targetRowsQuery = targetRowsQuery.Where(row => row.Id == request.PayrollAllowanceSummaryRecordId.Value);
        }

        var targetRows = await targetRowsQuery.ToListAsync(cancellationToken);
        if(request.PayrollAllowanceSummaryRecordId.HasValue && targetRows.Count == 0)
        {
            throw new InvalidOperationException("Dòng tổng hợp phụ cấp không thuộc kỳ lương cần làm mới.");
        }

        var targetRowsByEmployeeId = targetRows.ToDictionary(row => row.EmployeeId);
        var employeeIdsToProcess = request.PayrollAllowanceSummaryRecordId.HasValue
            ? targetRows.Select(row => row.EmployeeId).ToArray()
            : sourceEmployeeIds
                .Concat(targetRows.Select(row => row.EmployeeId))
                .Distinct()
                .ToArray();

        var now = GetDatabaseNow();
        var createdCount = 0;
        var updatedCount = 0;
        var skippedLockedCount = 0;

        foreach(var employeeId in employeeIdsToProcess)
        {
            var sourceAmounts = new PayrollAllowanceSummaryAllowanceAmounts(
                Responsibility: responsibilitySourceByEmployeeId.GetValueOrDefault(employeeId),
                ResponsibilityOther: otherResponsibilitySourceByEmployeeId.GetValueOrDefault(employeeId),
                Seniority: senioritySourceByEmployeeId.GetValueOrDefault(employeeId),
                Attendance: attendanceSourceByEmployeeId.GetValueOrDefault(employeeId),
                Meal: mealSourceByEmployeeId.GetValueOrDefault(employeeId),
                Hazard: hazardSourceByEmployeeId.GetValueOrDefault(employeeId),
                Other: otherAllowanceSourceByEmployeeId.GetValueOrDefault(employeeId),
                LeaveHoliday: leaveHolidaySourceByEmployeeId.GetValueOrDefault(employeeId));

            if(targetRowsByEmployeeId.TryGetValue(employeeId, out var existingRow))
            {
                var refreshDecision = PayrollAllowanceSummaryRefreshPolicy.Decide(
                    new PayrollAllowanceSummaryRefreshInput(
                        existingRow.IsLocked
                            ? PayrollAllowanceSummaryLockState.Locked
                            : PayrollAllowanceSummaryLockState.Open,
                        GetAllowanceAmounts(existingRow),
                        sourceAmounts,
                        existingRow.Note));

                if(refreshDecision.Disposition is PayrollAllowanceSummaryRefreshDisposition.SkippedBecauseLocked)
                {
                    skippedLockedCount++;
                    continue;
                }

                if(refreshDecision.Disposition is PayrollAllowanceSummaryRefreshDisposition.NoAllowanceChanges)
                {
                    continue;
                }

                ApplyAllowanceAmounts(existingRow, refreshDecision.ResultingAmounts);
                existingRow.UpdatedAtUtc = now;
                existingRow.UpdatedBy = actor;
                updatedCount++;
                continue;
            }

            var newRow = CreateRefreshedSummaryRow(
                employeeId,
                targetPayrollYear,
                targetPayrollMonth,
                sourceAmounts,
                actor,
                now);

            dbContext.PayrollAllowanceSummaryRecords.Add(newRow);
            targetRowsByEmployeeId[employeeId] = newRow;
            createdCount++;
        }

        var currentTargetRows = targetRowsByEmployeeId.Values.ToArray();
        var currentTargetSummaryIds = currentTargetRows.Select(row => row.Id).ToArray();
        var existingMealSummaryIds = currentTargetSummaryIds.Length == 0
            ? []
            : await dbContext.PayrollMealAllowanceRecords
                .Where(row => currentTargetSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
                .Select(row => row.PayrollAllowanceSummaryRecordId)
                .ToHashSetAsync(cancellationToken);
        var createdMealDetailCount = 0;
        foreach(var targetSummaryRow in currentTargetRows.Where(row => !row.IsLocked && !existingMealSummaryIds.Contains(row.Id)))
        {
            var mealRow = CreateMealRow(targetSummaryRow, null, actor, now);
            mealRow.IsLocked = targetSummaryRow.IsLocked;
            dbContext.PayrollMealAllowanceRecords.Add(mealRow);
            createdMealDetailCount++;
        }

        if(createdCount > 0 || updatedCount > 0 || createdMealDetailCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new RefreshPayrollAllowanceSummaryResult(
            targetPayrollMonth,
            targetPayrollYear,
            sourceEmployeeIds.Length,
            responsibilitySourceByEmployeeId.Count,
            senioritySourceByEmployeeId.Count,
            attendanceSourceByEmployeeId.Count,
            mealSourceByEmployeeId.Count,
            hazardSourceByEmployeeId.Count,
            otherAllowanceSourceByEmployeeId.Count,
            otherResponsibilitySourceByEmployeeId.Count,
            leaveHolidaySourceByEmployeeId.Count,
            createdCount,
            updatedCount,
            skippedLockedCount);
    }

    #endregion

    #region Thay đổi dữ liệu và trạng thái khóa

    /// <summary>
    /// Xóa snapshot chưa khóa sau khi kiểm tra phiên bản và dọn các dòng phụ thuộc không được cascade tự động.
    /// </summary>
    public async Task DeleteAsync(
        DeletePayrollAllowanceSummariesRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if(request.Items is null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("Thiếu danh sách dòng tổng hợp phụ cấp cần xóa.");
        }

        var requestedItemsById = request.Items
            .Where(item => item.Id != Guid.Empty)
            .GroupBy(item => item.Id)
            .ToDictionary(group => group.Key, group => group.First());
        var normalizedIds = requestedItemsById.Keys
            .ToArray();

        if(normalizedIds.Length == 0)
        {
            throw new InvalidOperationException("Danh sách dòng tổng hợp phụ cấp cần xóa không hợp lệ.");
        }

        DetachTrackedSummaryRows(normalizedIds);
        var rows = await dbContext.PayrollAllowanceSummaryRecords
            .Where(row => normalizedIds.Contains(row.Id))
            .ToListAsync(cancellationToken);

        if(rows.Count != normalizedIds.Length)
        {
            throw new InvalidOperationException("Có dòng tổng hợp phụ cấp không còn tồn tại hoặc đã bị thay đổi.");
        }

        foreach(var row in rows)
        {
            EnsureCurrentVersion(row, requestedItemsById[row.Id].OriginalUpdatedAtUtc);
        }

        var lockedRows = rows
            .Where(row => row.IsLocked)
            .ToArray();

        if(lockedRows.Length > 0)
        {
            throw new InvalidOperationException(
                lockedRows.Length == 1
                    ? "Dòng tổng hợp phụ cấp đã khóa, hãy mở khóa trước khi xóa."
                    : $"Có {lockedRows.Length} dòng tổng hợp phụ cấp đã khóa. Hãy mở khóa trước khi xóa.");
        }

        await RemoveDependentAllowanceRowsAsync(normalizedIds, cancellationToken);

        dbContext.PayrollAllowanceSummaryRecords.RemoveRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PayrollAllowanceSummaryListItemDto> SetLockStateAsync(
        SetPayrollAllowanceSummaryLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if(request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Thiếu định danh dòng phụ cấp tổng hợp cần cập nhật.");
        }

        var actor = NormalizeActor(request.Actor);
        DetachTrackedSummaryRows([request.Id]);
        var row = await dbContext.PayrollAllowanceSummaryRecords
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng phụ cấp tổng hợp cần cập nhật.");

        EnsureCurrentVersion(row, request.OriginalUpdatedAtUtc);

        row.IsLocked = request.IsLocked;
        row.UpdatedAtUtc = GetDatabaseNow();
        row.UpdatedBy = actor;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(row.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại dòng phụ cấp tổng hợp vừa cập nhật.");
    }

    public async Task<SetPayrollAllowanceSummaryBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollAllowanceSummaryBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredPeriod(request.PayrollYear, request.PayrollMonth);

        var hasExplicitTargets = request.PayrollAllowanceSummaryRecordIds is not null;
        var normalizedIds = request.PayrollAllowanceSummaryRecordIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if(hasExplicitTargets && (normalizedIds is null || normalizedIds.Length == 0))
        {
            throw new InvalidOperationException("Hãy chọn ít nhất một dòng tổng hợp phụ cấp hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
        }

        var concurrencyTokensById = request.ConcurrencyTokens?
            .Where(token => token.Id != Guid.Empty)
            .GroupBy(token => token.Id)
            .ToDictionary(group => group.Key, group => group.First());
        if(hasExplicitTargets
           && (concurrencyTokensById is null
               || concurrencyTokensById.Count != normalizedIds!.Length
               || normalizedIds.Any(id => !concurrencyTokensById.ContainsKey(id))))
        {
            throw new InvalidOperationException("Dữ liệu các dòng được chọn đã không còn đồng bộ. Hãy tải lại dữ liệu trước khi thao tác.");
        }

        var actor = NormalizeActor(request.Actor);
        var requestAuditCommand = auditScope.Current;
        var command = requestAuditCommand ?? new AuditCommand(
            Guid.NewGuid(),
            AuditActions.AllowanceSummary.BatchLockStateChanged,
            new AuditActor(actor, actor, AuditActorKind.System, AuditSource.Worker),
            Guid.NewGuid().ToString("N"),
            AuditCaptureMode.OperationOnly,
            Metadata: new Dictionary<string, string>
            {
                ["auditScope"] = "system-fallback"
            });

        return await auditedMutation.ExecuteAsync(
            command with { ActionIntent = AuditActions.AllowanceSummary.BatchLockStateChanged },
            async token =>
            {
                DetachTrackedSummaryRows(request.PayrollYear, request.PayrollMonth, normalizedIds);
                var targetQuery = dbContext.PayrollAllowanceSummaryRecords
                    .Where(row => row.PayrollYear == request.PayrollYear && row.PayrollMonth == request.PayrollMonth);

                PayrollAllowanceSummaryRecordRow[] targetRows;
                if(hasExplicitTargets)
                {
                    targetRows = await targetQuery
                        .Where(row => normalizedIds!.Contains(row.Id))
                        .ToArrayAsync(token);
                    if(targetRows.Length != normalizedIds!.Length)
                    {
                        throw new InvalidOperationException("Có dòng tổng hợp phụ cấp không tồn tại hoặc không thuộc kỳ lương đang áp dụng. Hãy tải lại dữ liệu trước khi thao tác.");
                    }

                    foreach(var row in targetRows)
                    {
                        EnsureCurrentVersion(row, concurrencyTokensById![row.Id].OriginalUpdatedAtUtc);
                    }
                }
                else
                {
                    targetRows = await targetQuery.ToArrayAsync(token);
                }

                var targetRowCount = targetRows.Length;
                var now = GetDatabaseNow();
                var updatedRows = targetRows
                    .Where(row => row.IsLocked != request.IsLocked)
                    .ToArray();
                foreach(var row in updatedRows)
                {
                    row.IsLocked = request.IsLocked;
                    row.UpdatedAtUtc = now;
                    row.UpdatedBy = actor;
                }

                return new SetPayrollAllowanceSummaryBatchLockStateResult(
                    request.PayrollYear,
                    request.PayrollMonth,
                    targetRowCount,
                    updatedRows.Length);
            },
            result => new AuditOperationEvent(
                AuditActions.AllowanceSummary.BatchLockStateChanged,
                AuditEntityTypes.AllowanceSummary,
                EntityDisplayName: $"{result.PayrollMonth:00}/{result.PayrollYear}",
                Outcome: result.UpdatedCount == 0
                    ? AuditOperationOutcome.NoChanges
                    : AuditOperationOutcome.Succeeded,
                Metadata: new Dictionary<string, string>
                {
                    ["isLocked"] = request.IsLocked.ToString(),
                    ["scope"] = hasExplicitTargets ? "selectedRows" : "wholePeriod",
                    ["targetRowCount"] = result.TargetRowCount.ToString(),
                    ["updatedCount"] = result.UpdatedCount.ToString(),
                    ["auditScope"] = requestAuditCommand is null ? "system-fallback" : "request"
                }),
            cancellationToken);
    }

    public async Task<PayrollAllowanceSummaryListItemDto> UpdateManualValuesAsync(
        UpdatePayrollAllowanceSummaryManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedNote = PayrollAllowanceSummaryManualAdjustmentPolicy.ValidateAndNormalize(request);

        var actor = NormalizeActor(request.Actor);
        DetachTrackedSummaryRows([request.Id]);
        var row = await dbContext.PayrollAllowanceSummaryRecords
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng phụ cấp tổng hợp cần cập nhật.");

        EnsureCurrentVersion(row, request.OriginalUpdatedAtUtc);

        if(row.IsLocked)
        {
            throw new InvalidOperationException("Dòng phụ cấp tổng hợp đã khóa, không thể cập nhật giá trị nhập tay.");
        }

        PayrollAllowanceSummaryManualAdjustmentPolicy.EnsureAttendanceProjectionIsNotOverridden(
            request.AttendanceAllowanceAmount,
            row.AttendanceAllowanceAmount);

        row.ResponsibilityAllowanceAmount = request.ResponsibilityAllowanceAmount;
        row.ResponsibilityOtherAllowanceAmount = request.ResponsibilityOtherAllowanceAmount;
        row.SeniorityAllowanceAmount = request.SeniorityAllowanceAmount;
        row.MealAllowanceAmount = request.MealAllowanceAmount;
        row.HazardAllowanceAmount = request.HazardAllowanceAmount;
        row.OtherAllowanceAmount = request.OtherAllowanceAmount;
        row.LeaveHolidayAllowanceAmount = request.LeaveHolidayAllowanceAmount;
        row.Note = normalizedNote;
        row.IsLocked = request.IsLocked;
        row.UpdatedAtUtc = GetDatabaseNow();
        row.UpdatedBy = actor;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(row.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại dòng phụ cấp tổng hợp vừa cập nhật.");
    }

    public async Task<PayrollAllowanceSummaryListItemDto> UpdateManualValuesAsync(
        UpdatePayrollAllowanceSummaryManualNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var current = await dbContext.PayrollAllowanceSummaryRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng tổng hợp phụ cấp cần cập nhật.");

        return await UpdateManualValuesAsync(
            new UpdatePayrollAllowanceSummaryManualValuesRequest(
                request.Id,
                current.ResponsibilityAllowanceAmount,
                current.ResponsibilityOtherAllowanceAmount,
                current.SeniorityAllowanceAmount,
                null,
                current.MealAllowanceAmount,
                current.HazardAllowanceAmount,
                current.OtherAllowanceAmount,
                current.LeaveHolidayAllowanceAmount,
                request.Note,
                current.IsLocked,
                request.OriginalUpdatedAtUtc,
                request.Actor),
            cancellationToken);
    }
    #endregion

    #region Trợ giúp ánh xạ, tạo snapshot và kiểm tra dữ liệu

    /// <summary>Đọc lại dòng vừa ghi với dữ liệu nhân sự để trả về đúng contract danh sách.</summary>
    private async Task<PayrollAllowanceSummaryListItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query =
            from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
            where summary.Id == id
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select MapToDto(summary, employee, department, position);

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private static PayrollAllowanceSummaryListItemDto MapToDto(
        PayrollAllowanceSummaryRecordRow summary,
        AttendanceGatewayEmployeeRow? employee,
        AttendanceDepartmentRow? department,
        AttendanceGatewayPositionRow? position) =>
        new(
            summary.Id,
            summary.EmployeeId,
            employee?.EmployeeCode,
            employee is null ? null : BuildEmployeeName(employee),
            department is null ? null : BuildDepartmentName(department),
            position?.Name,
            summary.PayrollMonth,
            summary.PayrollYear,
            summary.ResponsibilityAllowanceAmount,
            summary.ResponsibilityOtherAllowanceAmount,
            summary.SeniorityAllowanceAmount,
            summary.AttendanceAllowanceAmount,
            summary.MealAllowanceAmount,
            summary.HazardAllowanceAmount,
            summary.OtherAllowanceAmount,
            summary.LeaveHolidayAllowanceAmount,
            summary.IsLocked,
            summary.Note,
            summary.CreatedAtUtc,
            summary.CreatedBy,
            summary.UpdatedAtUtc,
            summary.UpdatedBy);

    private static PayrollAllowanceSummaryExportRowDto MapToExportRow(
        PayrollAllowanceSummaryListItemDto source) =>
        new(
            SanitizeExportText(source.EmployeeCode),
            SanitizeExportText(source.EmployeeName),
            SanitizeExportText(source.DepartmentName),
            SanitizeExportText(source.PositionName),
            source.PayrollMonth,
            source.PayrollYear,
            source.ResponsibilityAllowanceAmount,
            source.ResponsibilityOtherAllowanceAmount,
            source.SeniorityAllowanceAmount,
            source.AttendanceAllowanceAmount,
            source.MealAllowanceAmount,
            source.HazardAllowanceAmount,
            source.OtherAllowanceAmount,
            source.LeaveHolidayAllowanceAmount,
            source.ResponsibilityAllowanceAmount
            + source.ResponsibilityOtherAllowanceAmount
            + source.SeniorityAllowanceAmount
            + source.AttendanceAllowanceAmount
            + source.MealAllowanceAmount
            + source.HazardAllowanceAmount
            + source.OtherAllowanceAmount
            + source.LeaveHolidayAllowanceAmount,
            source.IsLocked,
            SanitizeExportText(source.Note));

    /// <summary>
    /// Copies only values owned by the summary workflow. Attendance is a derived projection owned
    /// by Phụ cấp chuyên cần, so a previous-period snapshot must never overwrite it.
    /// </summary>
    private static void ApplySummaryValues(
        PayrollAllowanceSummaryRecordRow sourceRow,
        PayrollAllowanceSummaryRecordRow targetRow,
        short targetPayrollMonth,
        short targetPayrollYear,
        string actor,
        DateTime now)
    {
        targetRow.EmployeeId = sourceRow.EmployeeId;
        targetRow.PayrollMonth = targetPayrollMonth;
        targetRow.PayrollYear = targetPayrollYear;
        targetRow.ResponsibilityAllowanceAmount = sourceRow.ResponsibilityAllowanceAmount;
        targetRow.ResponsibilityOtherAllowanceAmount = sourceRow.ResponsibilityOtherAllowanceAmount;
        targetRow.SeniorityAllowanceAmount = sourceRow.SeniorityAllowanceAmount;
        targetRow.MealAllowanceAmount = sourceRow.MealAllowanceAmount;
        targetRow.HazardAllowanceAmount = sourceRow.HazardAllowanceAmount;
        targetRow.OtherAllowanceAmount = sourceRow.OtherAllowanceAmount;
        targetRow.LeaveHolidayAllowanceAmount = sourceRow.LeaveHolidayAllowanceAmount;
        targetRow.IsLocked = false;
        targetRow.Note = sourceRow.Note;
        targetRow.UpdatedAtUtc = now;
        targetRow.UpdatedBy = actor;
    }

    private static PayrollAllowanceSummaryRecordRow CreateSummaryRowFromSource(
        PayrollAllowanceSummaryRecordRow sourceRow,
        short targetPayrollMonth,
        short targetPayrollYear,
        string actor,
        DateTime now)
    {
        var targetRow = new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = now,
            CreatedBy = actor
        };
        ApplySummaryValues(sourceRow, targetRow, targetPayrollMonth, targetPayrollYear, actor, now);
        // A new period has no authoritative attendance calculation yet. The attendance feature
        // will populate this projection from the target period's workday data on refresh.
        targetRow.AttendanceAllowanceAmount = 0m;
        targetRow.UpdatedAtUtc = null;
        targetRow.UpdatedBy = null;
        return targetRow;
    }

    private static PayrollAllowanceSummaryRecordRow CreateEmptySummaryRow(
        Guid employeeId,
        short targetPayrollYear,
        short targetPayrollMonth,
        string actor,
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayrollMonth = targetPayrollMonth,
            PayrollYear = targetPayrollYear,
            ResponsibilityAllowanceAmount = 0m,
            ResponsibilityOtherAllowanceAmount = 0m,
            SeniorityAllowanceAmount = 0m,
            AttendanceAllowanceAmount = 0m,
            MealAllowanceAmount = 0m,
            HazardAllowanceAmount = 0m,
            OtherAllowanceAmount = 0m,
            LeaveHolidayAllowanceAmount = 0m,
            IsLocked = false,
            CreatedAtUtc = now,
            CreatedBy = actor
        };

    private static PayrollAllowanceSummaryRecordRow CreateRefreshedSummaryRow(
        Guid employeeId,
        short targetPayrollYear,
        short targetPayrollMonth,
        PayrollAllowanceSummaryAllowanceAmounts sourceAmounts,
        string actor,
        DateTime now)
    {
        var refreshDecision = PayrollAllowanceSummaryRefreshPolicy.Decide(
            new PayrollAllowanceSummaryRefreshInput(
                PayrollAllowanceSummaryLockState.Open,
                PayrollAllowanceSummaryAllowanceAmounts.Empty,
                sourceAmounts,
                PreservedManualNote: null));
        var row = new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayrollMonth = targetPayrollMonth,
            PayrollYear = targetPayrollYear,
            IsLocked = false,
            Note = refreshDecision.PreservedManualNote,
            CreatedAtUtc = now,
            CreatedBy = actor,
            UpdatedAtUtc = null,
            UpdatedBy = null
        };
        ApplyAllowanceAmounts(row, refreshDecision.ResultingAmounts);
        return row;
    }

    private async Task RemoveDependentAllowanceRowsAsync(
        IReadOnlyCollection<Guid> summaryIds,
        CancellationToken cancellationToken)
    {
        if(summaryIds.Count == 0)
        {
            return;
        }

        var responsibilityRows = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToListAsync(cancellationToken);
        var responsibilityAssignmentRows = await dbContext.PayrollResponsibilityAllowanceEmployeeAssignments
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToListAsync(cancellationToken);
        var attendanceRows = await dbContext.PayrollAttendanceAllowanceRecords
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToListAsync(cancellationToken);
        var hazardRows = await dbContext.PayrollHazardAllowanceRecords
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToListAsync(cancellationToken);
        var seniorityRows = await dbContext.PayrollEmployeeSeniorityAllowances
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToListAsync(cancellationToken);
        var otherResponsibilityRows = await dbContext.PayrollAllowanceOtherResponsibilityRecords
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToListAsync(cancellationToken);
        var leaveHolidayRows = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToListAsync(cancellationToken);
        var mealRows = await dbContext.PayrollMealAllowanceRecords
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToListAsync(cancellationToken);
        var otherAllowanceRows = await dbContext.PayrollOtherAllowanceRecords
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToListAsync(cancellationToken);

        dbContext.PayrollResponsibilityAllowanceAbcRows.RemoveRange(responsibilityRows);
        dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.RemoveRange(responsibilityAssignmentRows);
        dbContext.PayrollAttendanceAllowanceRecords.RemoveRange(attendanceRows);
        dbContext.PayrollHazardAllowanceRecords.RemoveRange(hazardRows);
        dbContext.PayrollEmployeeSeniorityAllowances.RemoveRange(seniorityRows);
        dbContext.PayrollAllowanceOtherResponsibilityRecords.RemoveRange(otherResponsibilityRows);
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.RemoveRange(leaveHolidayRows);
        dbContext.PayrollMealAllowanceRecords.RemoveRange(mealRows);
        dbContext.PayrollOtherAllowanceRecords.RemoveRange(otherAllowanceRows);
    }

    private async Task EnsureDependentAllowanceRowsAsync(
        IReadOnlyList<PayrollAllowanceSummaryRecordRow> targetSummaryRows,
        IReadOnlyDictionary<Guid, PayrollAllowanceSummaryRecordRow> sourceSummariesByEmployeeId,
        short targetPayrollYear,
        short targetPayrollMonth,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if(targetSummaryRows.Count == 0)
        {
            return;
        }

        var targetSummaryIds = targetSummaryRows.Select(row => row.Id).ToArray();
        var sourceSummaryIds = sourceSummariesByEmployeeId.Values.Select(row => row.Id).Distinct().ToArray();

        var sourceResponsibilityBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollResponsibilityAllowanceAbcRows
                .Where(row => sourceSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            sourceSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var targetResponsibilityBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollResponsibilityAllowanceAbcRows
                .Where(row => targetSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            targetSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var targetAttendanceBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollAttendanceAllowanceRecords
                .Where(row => targetSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            targetSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var sourceHazardBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollHazardAllowanceRecords
                .Where(row => sourceSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            sourceSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var targetHazardBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollHazardAllowanceRecords
                .Where(row => targetSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            targetSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var sourceSeniorityBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollEmployeeSeniorityAllowances
                .Where(row => sourceSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            sourceSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var targetSeniorityBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollEmployeeSeniorityAllowances
                .Where(row => targetSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            targetSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var sourceOtherResponsibilityBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollAllowanceOtherResponsibilityRecords
                .Where(row => sourceSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            sourceSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var targetOtherResponsibilityBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollAllowanceOtherResponsibilityRecords
                .Where(row => targetSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            targetSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var sourceLeaveHolidayBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollAllowanceSummaryLeaveHolidayRecords
                .Where(row => sourceSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            sourceSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var targetLeaveHolidayBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollAllowanceSummaryLeaveHolidayRecords
                .Where(row => targetSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            targetSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);

        var sourceMealBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollMealAllowanceRecords
                .AsNoTracking()
                .Where(row => sourceSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            sourceSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);
        var targetMealBySummaryId = await LoadBySummaryIdAsync(
            dbContext.PayrollMealAllowanceRecords
                .Where(row => targetSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId)),
            targetSummaryIds,
            row => row.PayrollAllowanceSummaryRecordId,
            cancellationToken);

        foreach(var targetSummary in targetSummaryRows)
        {
            sourceSummariesByEmployeeId.TryGetValue(targetSummary.EmployeeId, out var sourceSummary);
            var sourceSummaryId = sourceSummary?.Id;

            if(!targetResponsibilityBySummaryId.ContainsKey(targetSummary.Id))
            {
                sourceResponsibilityBySummaryId.TryGetValue(sourceSummaryId ?? Guid.Empty, out var sourceRow);
                dbContext.PayrollResponsibilityAllowanceAbcRows.Add(
                    CreateResponsibilityRow(targetSummary, sourceRow, targetPayrollYear, targetPayrollMonth, now));
            }

            if(!targetAttendanceBySummaryId.ContainsKey(targetSummary.Id))
            {
                dbContext.PayrollAttendanceAllowanceRecords.Add(
                    CreateUnresolvedAttendanceRow(targetSummary, actor, now));
            }

            if(!targetHazardBySummaryId.ContainsKey(targetSummary.Id))
            {
                sourceHazardBySummaryId.TryGetValue(sourceSummaryId ?? Guid.Empty, out var sourceRow);
                dbContext.PayrollHazardAllowanceRecords.Add(CreateHazardRow(targetSummary, sourceRow, actor, now));
            }

            if(!targetSeniorityBySummaryId.ContainsKey(targetSummary.Id))
            {
                sourceSeniorityBySummaryId.TryGetValue(sourceSummaryId ?? Guid.Empty, out var sourceRow);
                dbContext.PayrollEmployeeSeniorityAllowances.Add(CreateSeniorityRow(targetSummary, sourceRow, actor, now));
            }

            if(!targetOtherResponsibilityBySummaryId.ContainsKey(targetSummary.Id))
            {
                sourceOtherResponsibilityBySummaryId.TryGetValue(sourceSummaryId ?? Guid.Empty, out var sourceRow);
                dbContext.PayrollAllowanceOtherResponsibilityRecords.Add(
                    CreateOtherResponsibilityRow(targetSummary, sourceRow, actor, now));
            }

            if(!targetLeaveHolidayBySummaryId.ContainsKey(targetSummary.Id))
            {
                sourceLeaveHolidayBySummaryId.TryGetValue(sourceSummaryId ?? Guid.Empty, out var sourceRow);
                dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(CreateLeaveHolidayRow(targetSummary, sourceRow, actor, now));
            }

            if(!targetMealBySummaryId.ContainsKey(targetSummary.Id))
            {
                sourceMealBySummaryId.TryGetValue(sourceSummaryId ?? Guid.Empty, out var sourceRow);
                dbContext.PayrollMealAllowanceRecords.Add(
                    CreateMealRow(targetSummary, sourceRow, actor, now));
            }
        }
    }

    private static async Task<Dictionary<Guid, TRow>> LoadBySummaryIdAsync<TRow>(
        IQueryable<TRow> query,
        IReadOnlyCollection<Guid> summaryIds,
        Func<TRow, Guid> summaryIdSelector,
        CancellationToken cancellationToken)
        where TRow : class
    {
        if(summaryIds.Count == 0)
        {
            return [];
        }

        var rows = await query.ToListAsync(cancellationToken);
        return rows
            .Where(row => summaryIds.Contains(summaryIdSelector(row)))
            .GroupBy(summaryIdSelector)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static PayrollResponsibilityAllowanceAbcRow CreateResponsibilityRow(
        PayrollAllowanceSummaryRecordRow targetSummary,
        PayrollResponsibilityAllowanceAbcRow? sourceRow,
        short targetPayrollYear,
        short targetPayrollMonth,
        DateTime now) =>
        new()
        {
            // The ABC snapshot key is configured as ValueGeneratedNever. Each
            // copied detail row therefore needs its own client-generated key;
            // otherwise multiple source employees are all added as Guid.Empty
            // and EF Core rejects the second tracked instance.
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = targetSummary.Id,
            // The copied ABC snapshot belongs to the target summary's employee,
            // not to an optional source snapshot. This is required by the ABC
            // table's EmployeeId foreign key.
            EmployeeId = targetSummary.EmployeeId,
            GradeId = sourceRow?.GradeId,
            GradeCode = sourceRow?.GradeCode,
            GradeName = sourceRow?.GradeName ?? string.Empty,
            Year = targetPayrollYear,
            Month = targetPayrollMonth,
            ActualWorkDays = sourceRow?.ActualWorkDays ?? 0m,
            StandardWorkDays = sourceRow?.StandardWorkDays ?? 0m,
            AbcRating = sourceRow?.AbcRating ?? string.Empty,
            MonthlyPerformanceBonusAmount = sourceRow?.MonthlyPerformanceBonusAmount ?? 0m,
            IsPerformanceBonusExcluded = sourceRow?.IsPerformanceBonusExcluded ?? false,
            StandardResponsibilityAllowanceAmount = sourceRow?.StandardResponsibilityAllowanceAmount ?? 0m,
            ActualResponsibilityAllowanceAmount = sourceRow?.ActualResponsibilityAllowanceAmount ?? 0m,
            IsLocked = false,
            CreatedAtUtc = now,
            // PostgreSQL stores this feature's business timestamps as
            // `timestamp without time zone`. A source entity can still carry
            // Kind=Utc when it was produced by another workflow or is already
            // tracked by the current circuit; writing it directly makes Npgsql
            // reject the whole SaveChanges batch.
            CalculatedAtUtc = NormalizeDatabaseTimestamp(sourceRow?.CalculatedAtUtc),
            CalculatedBy = sourceRow?.CalculatedBy,
            Note = sourceRow?.Note
        };

    /// <summary>
    /// Creates a neutral attendance snapshot for a new summary row. It intentionally does not
    /// copy any previous-period calculation because standard workdays, KP violations and other
    /// inputs are specific to the target period and are owned by Phụ cấp chuyên cần.
    /// </summary>
    private static PayrollAttendanceAllowanceRecordRow CreateUnresolvedAttendanceRow(
        PayrollAllowanceSummaryRecordRow targetSummary,
        string actor,
        DateTime now) =>
        new()
        {
            PayrollAllowanceSummaryRecordId = targetSummary.Id,
            StandardAllowanceAmount = 0m,
            StandardWorkdayCount = 0m,
            ActualWorkdayCount = 0m,
            AdministrativeWorkdayCount = 0m,
            LateEarlyDeductionDays = 0m,
            AttendanceRate = 0m,
            AllowanceAmount = 0m,
            AppliedRuleKey = null,
            AttendanceClass = null,
            CtlWorkdayCount = null,
            LateEarlyMinutes = null,
            Kqcc = null,
            HasKpViolation = false,
            Note = null,
            IsLocked = false,
            RefreshedAtUtc = null,
            RefreshedBy = null,
            CreatedAtUtc = now,
            CreatedBy = actor
        };

    private static PayrollMealAllowanceRecordRow CreateMealRow(
        PayrollAllowanceSummaryRecordRow targetSummary,
        PayrollMealAllowanceRecordRow? sourceRow,
        string actor,
        DateTime now) =>
        new()
        {
            PayrollAllowanceSummaryRecordId = targetSummary.Id,
            QualifiedMealDays = sourceRow?.QualifiedMealDays ?? 0,
            Overtime1900Days = sourceRow?.Overtime1900Days ?? 0,
            MealAllowancePerQualifiedDay = sourceRow?.MealAllowancePerQualifiedDay ?? 0m,
            MealAllowanceAmount = sourceRow?.MealAllowanceAmount ?? 0m,
            RuleCode = sourceRow?.RuleCode ?? "seed",
            RuleVersion = sourceRow?.RuleVersion,
            Note = sourceRow?.Note,
            IsLocked = false,
            CalculatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = actor
        };

    private static PayrollHazardAllowanceRecordRow CreateHazardRow(
        PayrollAllowanceSummaryRecordRow targetSummary,
        PayrollHazardAllowanceRecordRow? sourceRow,
        string actor,
        DateTime now) =>
        new()
        {
            PayrollAllowanceSummaryRecordId = targetSummary.Id,
            QualifiedWorkdayCount = sourceRow?.QualifiedWorkdayCount ?? 0m,
            LateEarlyDeductionDays = sourceRow?.LateEarlyDeductionDays ?? 0m,
            PayableWorkdayCount = sourceRow?.PayableWorkdayCount ?? 0m,
            HazardAllowancePerDay = sourceRow?.HazardAllowancePerDay ?? 0m,
            HazardAllowanceAmount = sourceRow?.HazardAllowanceAmount ?? 0m,
            IsEligibleDepartment = sourceRow?.IsEligibleDepartment ?? false,
            ExclusionReason = sourceRow?.ExclusionReason,
            CreatedAtUtc = now,
            CreatedBy = actor
        };

    private static PayrollEmployeeSeniorityAllowanceRow CreateSeniorityRow(
        PayrollAllowanceSummaryRecordRow targetSummary,
        PayrollEmployeeSeniorityAllowanceRow? sourceRow,
        string actor,
        DateTime now) =>
        new()
        {
            PayrollAllowanceSummaryRecordId = targetSummary.Id,
            EmploymentStartDate = sourceRow?.EmploymentStartDate,
            CompletedSeniorityYears = sourceRow?.CompletedSeniorityYears,
            CompletedSeniorityMonths = sourceRow?.CompletedSeniorityMonths,
            AdministrativeWorkDays = sourceRow?.AdministrativeWorkDays,
            LateEarlyLeaveWorkDays = sourceRow?.LateEarlyLeaveWorkDays,
            SalaryWorkDays = sourceRow?.SalaryWorkDays,
            AppliedRuleKey = sourceRow?.AppliedRuleKey,
            AllowanceAmount = sourceRow?.AllowanceAmount ?? 0m,
            Note = sourceRow?.Note,
            IsLocked = false,
            RefreshedAtUtc = sourceRow is null ? null : now,
            RefreshedBy = sourceRow is null ? null : actor,
            CreatedAtUtc = now,
            CreatedBy = actor
        };

    private static PayrollAllowanceOtherResponsibilityRecordRow CreateOtherResponsibilityRow(
        PayrollAllowanceSummaryRecordRow targetSummary,
        PayrollAllowanceOtherResponsibilityRecordRow? sourceRow,
        string actor,
        DateTime now) =>
        new()
        {
            PayrollAllowanceSummaryRecordId = targetSummary.Id,
            AllowanceWorkdayCount = sourceRow?.AllowanceWorkdayCount ?? 0m,
            StandardResponsibilityAllowanceAmount = sourceRow?.StandardResponsibilityAllowanceAmount ?? 0m,
            ActualResponsibilityAllowanceAmount = sourceRow?.ActualResponsibilityAllowanceAmount ?? 0m,
            Note = sourceRow?.Note,
            IsLocked = false,
            RefreshedAtUtc = sourceRow is null ? null : now,
            RefreshedBy = sourceRow is null ? null : actor,
            CreatedAtUtc = now,
            CreatedBy = actor
        };

    private static PayrollAllowanceSummaryLeaveHolidayRecordRow CreateLeaveHolidayRow(
        PayrollAllowanceSummaryRecordRow targetSummary,
        PayrollAllowanceSummaryLeaveHolidayRecordRow? sourceRow,
        string actor,
        DateTime now) =>
        new()
        {
            PayrollAllowanceSummaryRecordId = targetSummary.Id,
            DailyWageAmount = sourceRow?.DailyWageAmount ?? 0m,
            LeaveDayCount = sourceRow?.LeaveDayCount ?? 0m,
            HolidayDayCount = sourceRow?.HolidayDayCount ?? 0m,
            LeaveHolidayAllowanceAmount = sourceRow?.LeaveHolidayAllowanceAmount ?? 0m,
            Note = sourceRow?.Note,
            CreatedAtUtc = now,
            CreatedBy = actor
        };

    private static PayrollAllowanceSummaryAllowanceAmounts GetAllowanceAmounts(
        PayrollAllowanceSummaryRecordRow summary) =>
        new(
            Responsibility: summary.ResponsibilityAllowanceAmount,
            ResponsibilityOther: summary.ResponsibilityOtherAllowanceAmount,
            Seniority: summary.SeniorityAllowanceAmount,
            Attendance: summary.AttendanceAllowanceAmount,
            Meal: summary.MealAllowanceAmount,
            Hazard: summary.HazardAllowanceAmount,
            Other: summary.OtherAllowanceAmount,
            LeaveHoliday: summary.LeaveHolidayAllowanceAmount);

    private static void ApplyAllowanceAmounts(
        PayrollAllowanceSummaryRecordRow targetRow,
        PayrollAllowanceSummaryAllowanceAmounts amounts)
    {
        targetRow.ResponsibilityAllowanceAmount = amounts.Responsibility;
        targetRow.ResponsibilityOtherAllowanceAmount = amounts.ResponsibilityOther;
        targetRow.SeniorityAllowanceAmount = amounts.Seniority;
        targetRow.AttendanceAllowanceAmount = amounts.Attendance;
        targetRow.MealAllowanceAmount = amounts.Meal;
        targetRow.HazardAllowanceAmount = amounts.Hazard;
        targetRow.OtherAllowanceAmount = amounts.Other;
        targetRow.LeaveHolidayAllowanceAmount = amounts.LeaveHoliday;
    }

    private static string BuildEmployeeName(AttendanceGatewayEmployeeRow employee)
    {
        var parts = new[] { employee.LastName, employee.FirstName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part.Trim());

        return string.Join(" ", parts);
    }

    private static string BuildDepartmentName(AttendanceDepartmentRow department) =>
        NormalizeOptional(department.GroupName)
        ?? NormalizeOptional(department.TeamName)
        ?? NormalizeOptional(department.DepartmentOrWorkshopName)
        ?? NormalizeOptional(department.CenterName)
        ?? string.Empty;

    private static string NormalizeActor(string? actor)
    {
        var normalizedActor = NormalizeOptional(actor);
        if(string.IsNullOrWhiteSpace(normalizedActor))
        {
            return "system";
        }

        return normalizedActor.Length <= 128
            ? normalizedActor
            : normalizedActor[..128];
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? SanitizeExportText(string? value)
    {
        var normalizedValue = NormalizeOptional(value);
        if(string.IsNullOrEmpty(normalizedValue))
        {
            return normalizedValue;
        }

        return normalizedValue[0] is '=' or '+' or '-' or '@'
            ? $"'{normalizedValue}"
            : normalizedValue;
    }

    private static void EnsureCurrentVersion(
        PayrollAllowanceSummaryRecordRow row,
        DateTime? originalUpdatedAtUtc)
    {
        if(row.UpdatedAtUtc != originalUpdatedAtUtc)
        {
            throw new InvalidOperationException(
                "Dòng tổng hợp phụ cấp đã được cập nhật bởi phiên khác. Hãy tải lại dữ liệu trước khi thao tác tiếp.");
        }
    }

    /// <summary>
    /// Interactive Server keeps a scoped DbContext for the circuit. Detach only the
    /// target rows before a version-sensitive query, otherwise EF can return a row
    /// cached by a previous command instead of its current database version.
    /// </summary>
    private void DetachTrackedSummaryRows(IEnumerable<Guid> ids)
    {
        var targetIds = ids.ToHashSet();
        if(targetIds.Count == 0)
        {
            return;
        }

        foreach(var entry in dbContext.ChangeTracker.Entries<PayrollAllowanceSummaryRecordRow>()
                    .Where(entry => targetIds.Contains(entry.Entity.Id))
                    .ToArray())
        {
            entry.State = EntityState.Detached;
        }
    }

    private void DetachTrackedSummaryRows(
        int payrollYear,
        int payrollMonth,
        IReadOnlyCollection<Guid>? ids)
    {
        var targetIds = ids?.ToHashSet();
        foreach(var entry in dbContext.ChangeTracker.Entries<PayrollAllowanceSummaryRecordRow>()
                    .Where(entry => entry.Entity.PayrollYear == payrollYear
                        && entry.Entity.PayrollMonth == payrollMonth
                        && (targetIds is null || targetIds.Contains(entry.Entity.Id)))
                    .ToArray())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static int NormalizePageSize(int take) =>
        take <= 0
            ? DefaultPageSize
            : Math.Min(take, MaximumPageSize);

    private static DateTime GetDatabaseNow() =>
        PostgreSqlTimestamp.ToTimestampWithoutTimeZone(DateTime.UtcNow.AddHours(7));

    private static DateTime? NormalizeDatabaseTimestamp(DateTime? value) =>
        PostgreSqlTimestamp.ToTimestampWithoutTimeZone(value);

    private static PayrollPeriod GetPreviousPayrollPeriod(short year, short month) =>
        month == 1
            ? new PayrollPeriod((short)(year - 1), 12)
            : new PayrollPeriod(year, (short)(month - 1));

    private static IReadOnlyList<PayrollPeriod> GetDashboardPeriods(PayrollPeriod currentPeriod) =>
        Enumerable.Range(1, currentPeriod.Month)
            .Select(month => new PayrollPeriod(currentPeriod.Year, (short)month))
            .ToArray();

    private static void ValidateSearchPeriod(int? year, int? month)
    {
        if(year.HasValue && (year.Value < MinimumSupportedYear || year.Value > MaximumSupportedYear))
        {
            throw new InvalidOperationException(
                $"Năm dữ liệu phải nằm trong khoảng {MinimumSupportedYear} đến {MaximumSupportedYear}.");
        }

        if(month.HasValue && (month.Value < 1 || month.Value > 12))
        {
            throw new InvalidOperationException("Tháng dữ liệu phải nằm trong khoảng 1 đến 12.");
        }

        if(year == MinimumSupportedYear && month.HasValue && month.Value < MinimumSupportedMonth)
        {
            throw new InvalidOperationException(
                $"Mốc dữ liệu tổng hợp phụ cấp bắt đầu từ {MinimumSupportedMonth:00}/{MinimumSupportedYear}.");
        }
    }

    private static void ValidateRequiredPeriod(int year, int month)
    {
        if(year < MinimumSupportedYear || year > MaximumSupportedYear)
        {
            throw new InvalidOperationException(
                $"Năm dữ liệu phải nằm trong khoảng {MinimumSupportedYear} đến {MaximumSupportedYear}.");
        }

        if(month is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng dữ liệu phải nằm trong khoảng 1 đến 12.");
        }

        if(year == MinimumSupportedYear && month < MinimumSupportedMonth)
        {
            throw new InvalidOperationException(
                $"Mốc dữ liệu tổng hợp phụ cấp bắt đầu từ {MinimumSupportedMonth:00}/{MinimumSupportedYear}.");
        }
    }

    private static void ValidateExportFormat(PayrollAllowanceSummaryExportFormat format)
    {
        if(!Enum.IsDefined(format))
        {
            throw new InvalidOperationException("Định dạng xuất dữ liệu không hợp lệ.");
        }
    }

    private readonly record struct PayrollPeriod(short Year, short Month);

    private readonly record struct AllowanceMonthlyTotals(
        decimal Responsibility,
        decimal ResponsibilityOther,
        decimal Seniority,
        decimal Attendance,
        decimal Meal,
        decimal Hazard,
        decimal Other,
        decimal LeaveHoliday);

    private sealed class PayrollAllowanceSummarySearchProjection
    {
        public PayrollAllowanceSummaryRecordRow Summary { get; init; } = default!;

        public AttendanceGatewayEmployeeRow? Employee { get; init; }

        public AttendanceDepartmentRow? Department { get; init; }

        public AttendanceGatewayPositionRow? Position { get; init; }
    }

    #endregion
}
