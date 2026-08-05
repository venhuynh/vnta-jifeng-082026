using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.TongQuan.ChamCongHangNgay;

public partial class ChamCongHangNgay : IDisposable
{
    private const int AutoRefreshIntervalSeconds = 30;
    private const int AttendanceSummaryTake = 5000;
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly Dictionary<string, object> DecorativeIconButtonAttributes = new()
    {
        ["aria-hidden"] = "true",
        ["tabindex"] = "-1"
    };

    private readonly CancellationTokenSource disposalTokenSource = new();

    // Phụ thuộc
    [Inject]
    private AttendanceDepartmentDataProvider DepartmentDataProvider { get; set; } = default!;

    [Inject]
    private IAttendanceDailySummaryReadService AttendanceDailySummaryReadService { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    // Trạng thái
    private IReadOnlyList<DailyAttendanceDepartmentNode> DepartmentAttendanceNodes { get; set; } = [];
    private DailyAttendanceOverview Overview { get; set; } = DailyAttendanceOverview.Empty;
    private string? LoadErrorMessage { get; set; }
    private DateOnly WorkDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    private DateTime? LastUpdatedAt { get; set; }
    private int RefreshCountdownSeconds { get; set; } = AutoRefreshIntervalSeconds;
    private bool IsLoading { get; set; }

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool CanRefresh => !IsLoading;
    private string RefreshCountdownText => $"Tự làm mới sau {RefreshCountdownSeconds} giây";
    private string WorkDateDisplayText => WorkDate.ToString("dd/MM/yyyy", DisplayCulture);
    private string LastUpdatedDisplayText => LastUpdatedAt.HasValue
        ? LastUpdatedAt.Value.ToString("HH:mm:ss dd/MM/yyyy", DisplayCulture)
        : "--";

    private IReadOnlyList<DailyAttendanceSummaryCard> SummaryCards =>
    [
        new(
            "Tổng nhân sự",
            Overview.TotalEmployees,
            "Đang làm việc",
            "Theo danh mục phòng ban",
            VntaDevExpressIcons.SummaryTotalEmployees,
            ButtonRenderStyle.Primary,
            "summary-card summary-card-total",
            0,
            false),
        new(
            "Đã vào ca",
            Overview.CheckedInEmployees,
            "Có mặt hôm nay",
            $"{Overview.CheckedInEmployees:N0} / {Overview.TotalEmployees:N0}",
            VntaDevExpressIcons.SummaryCheckedIn,
            ButtonRenderStyle.Success,
            "summary-card summary-card-success",
            Overview.AttendanceRate,
            true),
        new(
            "Chưa vào ca",
            Overview.NotCheckedInEmployees,
            "Chưa check-in",
            $"{Overview.NotCheckedInEmployees:N0} / {Overview.TotalEmployees:N0}",
            VntaDevExpressIcons.SummaryNotCheckedIn,
            ButtonRenderStyle.Danger,
            "summary-card summary-card-danger",
            Overview.NotCheckedInRate,
            true),
        new(
            "Đi trễ",
            Overview.LateEmployees,
            "Chưa có quy tắc",
            $"{Overview.LateEmployees:N0} / {Overview.TotalEmployees:N0}",
            VntaDevExpressIcons.SummaryLate,
            ButtonRenderStyle.Warning,
            "summary-card summary-card-warning",
            Overview.LateRate,
            true),
        new(
            "Nghỉ phép",
            Overview.LeaveEmployees,
            "Chưa có nguồn phép",
            $"{Overview.LeaveEmployees:N0} / {Overview.TotalEmployees:N0}",
            VntaDevExpressIcons.SummaryLeave,
            ButtonRenderStyle.Info,
            "summary-card summary-card-purple",
            Overview.LeaveRate,
            true)
    ];

    // Vòng đời
    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
        _ = RunCountdownAsync(disposalTokenSource.Token);
        await base.OnInitializedAsync();
    }

    // Luồng tải dữ liệu
    private async Task ReloadAsync()
    {
        if(disposalTokenSource.IsCancellationRequested || IsLoading)
        {
            return;
        }

        LoadErrorMessage = null;
        IsLoading = true;

        try
        {
            WorkDate = DateOnly.FromDateTime(DateTime.Today);
            var departments = await DepartmentDataProvider.GetAsync(disposalTokenSource.Token);
            var dailySummaries = await AttendanceDailySummaryReadService.SearchAsync(
                new AttendanceDailySummaryFilter(WorkDate, WorkDate, null, AttendanceSummaryTake),
                disposalTokenSource.Token);

            DepartmentAttendanceNodes = BuildDepartmentAttendanceNodes(departments, dailySummaries);
            Overview = BuildOverview(DepartmentAttendanceNodes);
            LastUpdatedAt = DateTime.Now;
            RefreshCountdownSeconds = AutoRefreshIntervalSeconds;
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            DepartmentAttendanceNodes = [];
            Overview = DailyAttendanceOverview.Empty;
            LoadErrorMessage = "Có lỗi khi tải dữ liệu chấm công hằng ngày. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải dữ liệu chấm công hằng ngày.");
        }
        finally
        {
            RefreshCountdownSeconds = AutoRefreshIntervalSeconds;
            IsLoading = false;
        }
    }

    private async Task RunCountdownAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while(await timer.WaitForNextTickAsync(cancellationToken))
            {
                await InvokeAsync(async () =>
                {
                    if(cancellationToken.IsCancellationRequested || IsLoading)
                    {
                        return;
                    }

                    if(RefreshCountdownSeconds > 1)
                    {
                        RefreshCountdownSeconds--;
                        StateHasChanged();
                        return;
                    }

                    await ReloadAsync();
                    StateHasChanged();
                });
            }
        }
        catch(OperationCanceledException)
        {
            // Component đang dispose thì vòng tự làm mới dừng yên lặng.
        }
    }

    // Dựng dữ liệu TreeList
    private static IReadOnlyList<DailyAttendanceDepartmentNode> BuildDepartmentAttendanceNodes(
        IReadOnlyList<AttendanceDepartmentRecord> departments,
        IReadOnlyList<AttendanceDailySummaryListItemDto> dailySummaries)
    {
        var nodes = new List<DailyAttendanceDepartmentNode>();
        var nodeIndex = new Dictionary<string, DailyAttendanceDepartmentNode>(StringComparer.OrdinalIgnoreCase);
        var checkedAllocationByDepartmentId = AllocateCheckedEmployees(departments, dailySummaries);

        foreach(var department in departments)
        {
            var employeeCount = Math.Max(0, department.EmployeeCount);
            var checkedInCount = checkedAllocationByDepartmentId.GetValueOrDefault(department.Id);
            var blockName = NormalizeNullable(department.CenterName) ?? "(Chưa có khối)";
            var departmentName = NormalizeNullable(department.DepartmentOrWorkshopName)
                ?? NormalizeNullable(department.Name)
                ?? "(Chưa có phòng ban)";
            var teamName = NormalizeNullable(department.TeamName);
            var groupName = NormalizeNullable(department.GroupName);

            var blockNode = GetOrCreateNode(
                nodes,
                nodeIndex,
                BuildNodeId("block", blockName),
                parentId: null,
                blockName,
                level: 0);

            var departmentNode = GetOrCreateNode(
                nodes,
                nodeIndex,
                BuildNodeId("department", blockName, departmentName),
                blockNode.Id,
                departmentName,
                level: 1);

            var ancestors = new List<DailyAttendanceDepartmentNode> { blockNode, departmentNode };
            var parentNode = departmentNode;

            if(!string.IsNullOrWhiteSpace(teamName))
            {
                var teamNode = GetOrCreateNode(
                    nodes,
                    nodeIndex,
                    BuildNodeId("team", blockName, departmentName, teamName),
                    parentNode.Id,
                    teamName,
                    level: 2);

                ancestors.Add(teamNode);
                parentNode = teamNode;
            }

            if(!string.IsNullOrWhiteSpace(groupName))
            {
                var groupNode = GetOrCreateNode(
                    nodes,
                    nodeIndex,
                    BuildNodeId("group", blockName, departmentName, teamName, groupName),
                    parentNode.Id,
                    groupName,
                    level: 3);

                ancestors.Add(groupNode);
            }

            foreach(var node in ancestors)
            {
                node.EmployeeCount += employeeCount;
                node.CheckedInCount += checkedInCount;
            }
        }

        return nodes;
    }

    private static Dictionary<Guid, int> AllocateCheckedEmployees(
        IReadOnlyList<AttendanceDepartmentRecord> departments,
        IReadOnlyList<AttendanceDailySummaryListItemDto> dailySummaries)
    {
        var checkedCountByDepartmentName = dailySummaries
            .Where(summary => summary.PunchCount > 0)
            .GroupBy(
                summary => NormalizeDepartmentKey(summary.DepartmentName),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => CountDistinctEmployees(group),
                StringComparer.OrdinalIgnoreCase);

        var allocation = new Dictionary<Guid, int>();

        foreach(var departmentGroup in departments.GroupBy(GetDepartmentAllocationKey, StringComparer.OrdinalIgnoreCase))
        {
            var remainingCheckedCount = checkedCountByDepartmentName.GetValueOrDefault(departmentGroup.Key);

            foreach(var department in departmentGroup.OrderBy(department => department.FullPath, StringComparer.OrdinalIgnoreCase))
            {
                if(remainingCheckedCount <= 0)
                {
                    allocation[department.Id] = 0;
                    continue;
                }

                var employeeCapacity = Math.Max(0, department.EmployeeCount);
                var checkedInCount = Math.Min(employeeCapacity, remainingCheckedCount);
                allocation[department.Id] = checkedInCount;
                remainingCheckedCount -= checkedInCount;
            }
        }

        return allocation;
    }

    private static DailyAttendanceDepartmentNode GetOrCreateNode(
        List<DailyAttendanceDepartmentNode> nodes,
        Dictionary<string, DailyAttendanceDepartmentNode> nodeIndex,
        string id,
        string? parentId,
        string departmentName,
        int level)
    {
        if(nodeIndex.TryGetValue(id, out var existingNode))
        {
            return existingNode;
        }

        var node = new DailyAttendanceDepartmentNode
        {
            Id = id,
            ParentId = parentId,
            DepartmentName = departmentName,
            Level = level
        };

        nodes.Add(node);
        nodeIndex[id] = node;

        return node;
    }

    private static DailyAttendanceOverview BuildOverview(IReadOnlyList<DailyAttendanceDepartmentNode> nodes)
    {
        var rootNodes = nodes.Where(node => node.ParentId is null).ToArray();
        var totalEmployees = rootNodes.Sum(node => node.EmployeeCount);
        var checkedInEmployees = Math.Min(totalEmployees, rootNodes.Sum(node => node.CheckedInCount));

        return new DailyAttendanceOverview(
            totalEmployees,
            checkedInEmployees,
            LateEmployees: 0,
            LeaveEmployees: 0);
    }

    private static int CountDistinctEmployees(IEnumerable<AttendanceDailySummaryListItemDto> summaries) =>
        summaries
            .Select(BuildEmployeeAttendanceKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    // Hàm hiển thị
    private static string FormatPercent(double value) => $"{value:N1}%";

    private static string GetDepartmentNameCssClass(DailyAttendanceDepartmentNode node) =>
        node.Level switch
        {
            0 => "department-name department-name-block",
            1 => "department-name department-name-department",
            _ => "department-name"
        };

    // Hàm hỗ trợ
    private static string BuildNodeId(string prefix, params string?[] values) =>
        $"{prefix}:{string.Join("|", values.Select(value => NormalizeNullable(value)?.ToUpperInvariant() ?? string.Empty))}";

    private static string GetDepartmentAllocationKey(AttendanceDepartmentRecord department) =>
        NormalizeDepartmentKey(
            NormalizeNullable(department.DepartmentOrWorkshopName)
            ?? NormalizeNullable(department.Name)
            ?? NormalizeNullable(department.FullPath));

    private static string NormalizeDepartmentKey(string? value) =>
        NormalizeNullable(value)?.ToUpperInvariant() ?? string.Empty;

    private static string BuildEmployeeAttendanceKey(AttendanceDailySummaryListItemDto summary)
    {
        if(summary.EmployeeId.HasValue)
        {
            return summary.EmployeeId.Value.ToString("N");
        }

        return NormalizeDepartmentKey(
            NormalizeNullable(summary.EmployeeCode)
            ?? NormalizeNullable(summary.EmployeeName)
            ?? summary.Id.ToString("N"));
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }

    private sealed record DailyAttendanceSummaryCard(
        string Title,
        int Value,
        string Subtitle,
        string FooterText,
        string DevExpressIconUrl,
        ButtonRenderStyle IconRenderStyle,
        string CssClass,
        double ProgressPercent,
        bool ShowProgress)
    {
        public string ValueText => Value.ToString("N0", DisplayCulture);
    }

    private sealed record DailyAttendanceOverview(
        int TotalEmployees,
        int CheckedInEmployees,
        int LateEmployees,
        int LeaveEmployees)
    {
        public static DailyAttendanceOverview Empty { get; } = new(0, 0, 0, 0);

        public int NotCheckedInEmployees => Math.Max(0, TotalEmployees - CheckedInEmployees);
        public double AttendanceRate => CalculateRate(CheckedInEmployees, TotalEmployees);
        public double NotCheckedInRate => CalculateRate(NotCheckedInEmployees, TotalEmployees);
        public double LateRate => CalculateRate(LateEmployees, TotalEmployees);
        public double LeaveRate => CalculateRate(LeaveEmployees, TotalEmployees);

        private static double CalculateRate(int value, int total) =>
            total <= 0 ? 0 : Math.Round(value * 100d / total, 1);
    }

    private sealed class DailyAttendanceDepartmentNode
    {
        public string Id { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public int CheckedInCount { get; set; }
        public int Level { get; set; }
        public int NotCheckedInCount => Math.Max(0, EmployeeCount - CheckedInCount);
        public double AttendanceRate => EmployeeCount <= 0 ? 0 : Math.Round(CheckedInCount * 100d / EmployeeCount, 1);
    }
}
