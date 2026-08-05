using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

/// <summary>Sở hữu toàn bộ dữ liệu local-only và trạng thái xem của màn hình tiến độ triển khai.</summary>
internal sealed class ProjectImplementationProgressSessionState
{
    private const int AllPageSize = 5000;

    private static readonly IReadOnlyList<ProjectImplementationProgressPageSizeOption> DefaultPageSizeOptions =
    [
        new(20, "20"),
        new(50, "50"),
        new(100, "100"),
        new(AllPageSize, "Tất cả")
    ];

    private readonly TimeProvider timeProvider;
    private readonly ProjectImplementationProgressFilterState filterState = new();
    private List<ProjectImplementationProgressItem> items = [];
    private int pageSize = DefaultPageSizeOptions[0].Value;
    private int currentPageIndex;

    internal ProjectImplementationProgressSessionState()
        : this(TimeProvider.System)
    {
    }

    internal ProjectImplementationProgressSessionState(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
        Reset();
    }

    internal IReadOnlyList<ProjectImplementationProgressItem> Records => GetFilteredItems()
        .Skip(CurrentPageIndex * PageSize)
        .Take(PageSize)
        .ToArray();

    internal IReadOnlyList<ProjectImplementationProgressSummaryBadge> SummaryBadges => BuildSummaryBadges();

    internal IReadOnlyList<ProjectImplementationProgressStatusDefinition> StatusOptions =>
        ProjectImplementationProgressStatusCatalog.Definitions;

    internal IReadOnlyList<ProjectImplementationProgressPageSizeOption> PageSizeOptions => DefaultPageSizeOptions;

    internal string ActiveSummaryBadgeKey => filterState.ActiveSummaryBadgeKey;

    internal string? SearchText => filterState.SearchText;

    internal int PageSize => pageSize;

    internal int CurrentPageIndex => currentPageIndex;

    internal int TotalRecordCount => GetFilteredItems().Count;

    internal int TotalPageCount => TotalRecordCount <= 0
        ? 1
        : (int)Math.Ceiling(TotalRecordCount / (double)PageSize);

    internal int CurrentPageStartRecord => TotalRecordCount == 0
        ? 0
        : CurrentPageIndex * PageSize + 1;

    internal int CurrentPageEndRecord => TotalRecordCount == 0
        ? 0
        : Math.Min(TotalRecordCount, CurrentPageIndex * PageSize + Records.Count);

    internal bool CanBrowsePages => TotalRecordCount > 1;

    internal bool IsShowingAllRows => PageSize == AllPageSize;

    internal string PageSizeDescription => IsShowingAllRows ? "tất cả dòng" : "dòng/trang";

    internal string PagerSummaryText => TotalRecordCount == 0
        ? "Chưa có dữ liệu để hiển thị"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalRecordCount:N0} dòng";

    internal decimal AverageProgress => Records.Count == 0
        ? 0m
        : Records.Average(item => (decimal)item.ProgressPercent);

    internal void SetSearchText(string? value)
    {
        filterState.SetSearchText(value);
        currentPageIndex = 0;
    }

    internal void SelectSummaryBadge(string key)
    {
        filterState.SelectSummaryBadge(key);
        currentPageIndex = 0;
    }

    internal void ResetFilters()
    {
        filterState.Reset();
        currentPageIndex = 0;
    }

    internal void SetPageSize(int value)
    {
        var normalizedValue = DefaultPageSizeOptions.Any(option => option.Value == value)
            ? value
            : DefaultPageSizeOptions[0].Value;
        if(normalizedValue == pageSize)
        {
            return;
        }

        var firstVisibleRecordIndex = currentPageIndex * pageSize;
        pageSize = normalizedValue;
        currentPageIndex = firstVisibleRecordIndex / pageSize;
        ClampCurrentPageIndex();
    }

    internal void SetCurrentPageIndex(int value)
    {
        currentPageIndex = Math.Clamp(value, 0, Math.Max(0, TotalPageCount - 1));
    }

    internal ProjectImplementationProgressEditModel CreateNewEditModel()
    {
        var today = timeProvider.GetLocalNow().DateTime.Date;
        return new ProjectImplementationProgressEditModel
        {
            Id = Guid.NewGuid(),
            IsNew = true,
            Code = $"TD-{items.Count + 1:00}",
            StartDate = today,
            DueDate = today.AddDays(14),
            ProgressPercent = 0,
            Status = ProjectImplementationProgressStatus.NotStarted
        };
    }

    internal bool Save(ProjectImplementationProgressEditModel model)
    {
        var item = model.ToItem();
        if(model.IsNew)
        {
            items.Add(item);
        }
        else
        {
            var itemIndex = items.FindIndex(existingItem => existingItem.Id == item.Id);
            if(itemIndex < 0)
            {
                return false;
            }

            items[itemIndex] = item;
        }

        ClampCurrentPageIndex();
        return true;
    }

    internal void Reset()
    {
        items = CreateSeedItems();
        filterState.Reset();
        pageSize = DefaultPageSizeOptions[0].Value;
        currentPageIndex = 0;
    }

    private IReadOnlyList<ProjectImplementationProgressItem> GetFilteredItems() =>
        items
            .Where(MatchesSummaryFilter)
            .Where(MatchesSearchText)
            .OrderBy(item => item.DueDate)
            .ThenBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<ProjectImplementationProgressSummaryBadge> BuildSummaryBadges()
    {
        var badges = new List<ProjectImplementationProgressSummaryBadge>
        {
            new(
                ProjectImplementationProgressStatusCatalog.AllKey,
                "Hiển thị tất cả hạng mục",
                "Tất cả",
                items.Count,
                ProjectImplementationProgressTone.Info)
        };

        badges.AddRange(ProjectImplementationProgressStatusCatalog.Definitions
            .Where(definition => definition.IncludeInSummary)
            .Select(definition => new ProjectImplementationProgressSummaryBadge(
                definition.Key,
                definition.SummaryLabel,
                definition.SummaryShortLabel,
                items.Count(item => item.Status == definition.Value),
                definition.Tone)));

        badges.Add(new ProjectImplementationProgressSummaryBadge(
            ProjectImplementationProgressStatusCatalog.OverdueKey,
            "Hạng mục chưa hoàn tất đã quá hạn",
            "Quá hạn",
            items.Count(IsOverdue),
            ProjectImplementationProgressTone.Danger));

        return badges;
    }

    private bool MatchesSummaryFilter(ProjectImplementationProgressItem item)
    {
        var activeKey = filterState.ActiveSummaryBadgeKey;
        if(string.Equals(activeKey, ProjectImplementationProgressStatusCatalog.AllKey, StringComparison.Ordinal))
        {
            return true;
        }

        if(string.Equals(activeKey, ProjectImplementationProgressStatusCatalog.OverdueKey, StringComparison.Ordinal))
        {
            return IsOverdue(item);
        }

        return ProjectImplementationProgressStatusCatalog.TryGetByKey(activeKey, out var definition)
            && item.Status == definition.Value;
    }

    private bool MatchesSearchText(ProjectImplementationProgressItem item)
    {
        var searchText = filterState.SearchText;
        if(string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return item.Code.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || item.WorkItem.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || item.Module.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || item.Owner.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || item.Note.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || ProjectImplementationProgressStatusCatalog.Get(item.Status).Label.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsOverdue(ProjectImplementationProgressItem item) =>
        !ProjectImplementationProgressStatusCatalog.Get(item.Status).IsCompleted
        && item.DueDate.Date < timeProvider.GetLocalNow().DateTime.Date;

    private void ClampCurrentPageIndex() =>
        currentPageIndex = Math.Clamp(currentPageIndex, 0, Math.Max(0, TotalPageCount - 1));

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
            Status = ProjectImplementationProgressStatus.Completed,
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
            Status = ProjectImplementationProgressStatus.InProgress,
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
            Status = ProjectImplementationProgressStatus.WaitingAcceptance,
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
            Status = ProjectImplementationProgressStatus.InProgress,
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
            Status = ProjectImplementationProgressStatus.NotStarted,
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
            Status = ProjectImplementationProgressStatus.Paused,
            Note = "Cần xác nhận lại phạm vi nghiệm thu."
        }
    ];
}
