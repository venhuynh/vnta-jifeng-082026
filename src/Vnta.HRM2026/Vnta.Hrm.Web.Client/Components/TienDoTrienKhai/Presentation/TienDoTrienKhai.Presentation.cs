using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai;

/// <summary>Chuyển trạng thái local-only thành dữ liệu thuần trình bày cho các section.</summary>
public partial class TienDoTrienKhai
{
    private IReadOnlyList<ProjectImplementationProgressItem> Records => SessionState.Records;

    private IReadOnlyList<ProjectImplementationProgressSummaryBadge> SummaryBadges => SessionState.SummaryBadges;

    private IReadOnlyList<ProjectImplementationProgressStatusDefinition> StatusOptions => SessionState.StatusOptions;

    private IReadOnlyList<ProjectImplementationProgressPageSizeOption> PageSizeOptions => SessionState.PageSizeOptions;

    private string ActiveSummaryBadgeKey => SessionState.ActiveSummaryBadgeKey;

    private string? SearchText => SessionState.SearchText;

    private decimal AverageProgress => SessionState.AverageProgress;

    private int PageSize => SessionState.PageSize;

    private int CurrentPageIndex => SessionState.CurrentPageIndex;

    private int CurrentPageStartRecord => SessionState.CurrentPageStartRecord;

    private int TotalPageCount => SessionState.TotalPageCount;

    private bool CanBrowsePages => SessionState.CanBrowsePages;

    private string PageSizeDescription => SessionState.PageSizeDescription;

    private string PagerSummaryText => SessionState.PagerSummaryText;

    private bool CanInteract => !IsSavingEdit;

    private bool CanOperate => CanInteract;

    private bool CanChangeFilters => CanInteract;

    private bool CanEditFields => !IsSavingEdit;

    private bool CanSaveEdit =>
        IsEditPopupVisible
        && !IsSavingEdit
        && EditModel.Id != Guid.Empty;

    private string EditPopupTitle => EditModel.IsNew
        ? "Thêm hạng mục triển khai"
        : "Cập nhật hạng mục triển khai";

    private string EmptyStateTitle => "Không có hạng mục phù hợp";

    private string EmptyStateMessage => "Thử chọn lại trạng thái, xoá từ khoá tìm kiếm hoặc thêm một hạng mục mới.";

    private string EmptyStateActionText => "Hiển thị tất cả";

    private static string GetStatusLabel(ProjectImplementationProgressStatus status) =>
        ProjectImplementationProgressStatusCatalog.Get(status).Label;

    private static string GetStatusCssClass(ProjectImplementationProgressStatus status) =>
        $"implementation-progress-status implementation-progress-status-{ProjectImplementationProgressStatusCatalog.GetToneCssKey(ProjectImplementationProgressStatusCatalog.Get(status).Tone)}";

    private static string GetProgressValueCssClass(int progressPercent) => progressPercent switch
    {
        >= 100 => "implementation-progress-value-completed",
        >= 70 => "implementation-progress-value-on-track",
        >= 40 => "implementation-progress-value-at-risk",
        _ => "implementation-progress-value-delayed"
    };
}
