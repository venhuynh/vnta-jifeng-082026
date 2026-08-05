using DevExpress.Blazor;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai;

/// <summary>Màn hình theo dõi tiến độ triển khai với dữ liệu chỉ tồn tại trong phiên UI.</summary>
public partial class TienDoTrienKhai
{
    private const string SummaryAllKey = "all";
    private const string SummaryNotStartedKey = "not-started";
    private const string SummaryInProgressKey = "in-progress";
    private const string SummaryWaitingAcceptanceKey = "waiting-acceptance";
    private const string SummaryCompletedKey = "completed";
    private const string SummaryOverdueKey = "overdue";
    private const string NotStartedStatus = "Chưa bắt đầu";
    private const string InProgressStatus = "Đang triển khai";
    private const string WaitingAcceptanceStatus = "Chờ nghiệm thu";
    private const string CompletedStatus = "Hoàn tất";
    private const string PausedStatus = "Tạm dừng";

    private static readonly IReadOnlyList<string> StatusOptions =
    [
        NotStartedStatus,
        InProgressStatus,
        WaitingAcceptanceStatus,
        CompletedStatus,
        PausedStatus
    ];

    private IGrid? Grid { get; set; }

    private List<ProjectImplementationProgressItem> Items { get; set; } = CreateSeedItems();

    private string ActiveSummaryBadgeKey { get; set; } = SummaryAllKey;

    private string? SearchText { get; set; }

    private IReadOnlyList<ProjectImplementationProgressItem> FilteredItems =>
        Items
            .Where(MatchesSummaryFilter)
            .Where(MatchesSearchText)
            .OrderBy(item => item.DueDate)
            .ThenBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<ProgressSummaryBadge> SummaryBadges =>
    [
        new(SummaryAllKey, "Tất cả", "Hiển thị tất cả hạng mục", Items.Count),
        new(SummaryNotStartedKey, "Chưa bắt đầu", "Hạng mục chưa bắt đầu", Items.Count(item => item.Status == NotStartedStatus)),
        new(SummaryInProgressKey, "Đang làm", "Hạng mục đang triển khai", Items.Count(item => item.Status == InProgressStatus)),
        new(SummaryWaitingAcceptanceKey, "Chờ nghiệm thu", "Hạng mục chờ nghiệm thu", Items.Count(item => item.Status == WaitingAcceptanceStatus)),
        new(SummaryCompletedKey, "Hoàn tất", "Hạng mục đã hoàn tất", Items.Count(item => item.Status == CompletedStatus)),
        new(SummaryOverdueKey, "Quá hạn", "Hạng mục chưa hoàn tất đã quá hạn", Items.Count(IsOverdue))
    ];

    private decimal AverageProgress => FilteredItems.Count == 0
        ? 0m
        : FilteredItems.Average(item => (decimal)item.ProgressPercent);

    private Task AddItemAsync() => Grid?.StartEditNewRowAsync() ?? Task.CompletedTask;

    private void ResetItems()
    {
        Items = CreateSeedItems();
        ActiveSummaryBadgeKey = SummaryAllKey;
        SearchText = null;
    }

    private void ShowColumnChooser() => Grid?.ShowColumnChooser();

    private void SelectSummary(string key) => ActiveSummaryBadgeKey = key;

    private void ShowAllItems()
    {
        ActiveSummaryBadgeKey = SummaryAllKey;
        SearchText = null;
    }

    private void OnCustomizeEditModel(GridCustomizeEditModelEventArgs e)
    {
        if(!e.IsNew)
        {
            return;
        }

        var item = (ProjectImplementationProgressItem)e.EditModel;
        item.Id = Guid.NewGuid();
        item.Code = $"TD-{Items.Count + 1:00}";
        item.StartDate = DateTime.Today;
        item.DueDate = DateTime.Today.AddDays(14);
        item.ProgressPercent = 0;
        item.Status = NotStartedStatus;
    }

    private void OnEditModelSaving(GridEditModelSavingEventArgs e)
    {
        if(e.IsNew)
        {
            Items = [.. Items, (ProjectImplementationProgressItem)e.EditModel];
        }
        else
        {
            e.CopyChangesToDataItem();
            Items = [.. Items];
        }

        e.Reload = false;
    }

    private bool MatchesSummaryFilter(ProjectImplementationProgressItem item) => ActiveSummaryBadgeKey switch
    {
        SummaryNotStartedKey => item.Status == NotStartedStatus,
        SummaryInProgressKey => item.Status == InProgressStatus,
        SummaryWaitingAcceptanceKey => item.Status == WaitingAcceptanceStatus,
        SummaryCompletedKey => item.Status == CompletedStatus,
        SummaryOverdueKey => IsOverdue(item),
        _ => true
    };

    private bool MatchesSearchText(ProjectImplementationProgressItem item)
    {
        if(string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var searchText = SearchText.Trim();
        return item.Code.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || item.WorkItem.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || item.Module.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || item.Owner.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || item.Note.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || item.Status.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOverdue(ProjectImplementationProgressItem item) =>
        item.Status != CompletedStatus && item.DueDate.Date < DateTime.Today;

    private static string GetProgressValueCssClass(int progressPercent) => progressPercent switch
    {
        >= 100 => "implementation-progress-value-completed",
        >= 70 => "implementation-progress-value-on-track",
        >= 40 => "implementation-progress-value-at-risk",
        _ => "implementation-progress-value-delayed"
    };

    private static string GetStatusCssClass(string status) => status switch
    {
        CompletedStatus => "implementation-progress-status implementation-progress-status-completed",
        InProgressStatus => "implementation-progress-status implementation-progress-status-in-progress",
        WaitingAcceptanceStatus => "implementation-progress-status implementation-progress-status-waiting",
        PausedStatus => "implementation-progress-status implementation-progress-status-paused",
        _ => "implementation-progress-status implementation-progress-status-not-started"
    };

    private static List<ProjectImplementationProgressItem> CreateSeedItems() =>
    [
        new()
        {
            Id = Guid.Parse("0b5ff2d9-9e97-4bbd-b1aa-0bfbcb517903"),
            Code = "TD-01",
            WorkItem = "Chuẩn hoá danh mục phòng ban và chức vụ",
            Module = "Nhân sự",
            Owner = "Nhóm HRM",
            StartDate = new DateTime(2026, 7, 6),
            DueDate = new DateTime(2026, 7, 24),
            ProgressPercent = 100,
            Status = CompletedStatus,
            Note = "Đã bàn giao dữ liệu mẫu."
        },
        new()
        {
            Id = Guid.Parse("8a1b8db7-0ed9-497e-aeae-5b6b27efc209"),
            Code = "TD-02",
            WorkItem = "Hoàn thiện màn hình bảng công tháng",
            Module = "Chấm công",
            Owner = "Nhóm Chấm công",
            StartDate = new DateTime(2026, 7, 15),
            DueDate = new DateTime(2026, 8, 9),
            ProgressPercent = 78,
            Status = InProgressStatus,
            Note = "Đang rà soát nghiệp vụ và các trường hợp biên."
        },
        new()
        {
            Id = Guid.Parse("3d9b61b7-e8e5-45c6-8ed7-3c35d641fdab"),
            Code = "TD-03",
            WorkItem = "Đối soát phụ cấp chuyên cần",
            Module = "Tính lương",
            Owner = "Nhóm Payroll",
            StartDate = new DateTime(2026, 7, 20),
            DueDate = new DateTime(2026, 8, 7),
            ProgressPercent = 92,
            Status = WaitingAcceptanceStatus,
            Note = "Chờ người dùng nghiệp vụ nghiệm thu."
        },
        new()
        {
            Id = Guid.Parse("dcb5c748-ea3e-4a6d-b749-278fde706b6f"),
            Code = "TD-04",
            WorkItem = "Tích hợp đồng bộ máy chấm công",
            Module = "Thiết bị ADMS",
            Owner = "Nhóm Tích hợp",
            StartDate = new DateTime(2026, 7, 29),
            DueDate = new DateTime(2026, 8, 18),
            ProgressPercent = 55,
            Status = InProgressStatus,
            Note = "Đang kiểm thử kết nối và cơ chế tự kết nối lại."
        },
        new()
        {
            Id = Guid.Parse("856d1aa0-9c7e-49eb-91b7-2ef56b2ae494"),
            Code = "TD-05",
            WorkItem = "Chuẩn bị hướng dẫn sử dụng và đào tạo",
            Module = "Triển khai",
            Owner = "Nhóm Dự án",
            StartDate = new DateTime(2026, 8, 4),
            DueDate = new DateTime(2026, 8, 22),
            ProgressPercent = 20,
            Status = NotStartedStatus,
            Note = "Đợi chốt lịch đào tạo với đơn vị sử dụng."
        },
        new()
        {
            Id = Guid.Parse("0e4b395a-c93b-49cf-8581-febbcbacb943"),
            Code = "TD-06",
            WorkItem = "Chốt quy trình nghiệm thu giai đoạn 1",
            Module = "Quản lý dự án",
            Owner = "Ban dự án",
            StartDate = new DateTime(2026, 7, 10),
            DueDate = new DateTime(2026, 7, 31),
            ProgressPercent = 45,
            Status = PausedStatus,
            Note = "Cần xác nhận lại phạm vi nghiệm thu."
        }
    ];

    private sealed record ProgressSummaryBadge(string Key, string ShortLabel, string Label, int Count);
}
