using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Sections;

/// <summary>Trình bày các chỉ số tổng hợp và bộ lọc tìm kiếm của màn hình.</summary>
public partial class TienDoTrienKhaiSummary
{
    [Parameter] public IReadOnlyList<ProjectImplementationProgressSummaryBadge> SummaryBadges { get; set; } = [];
    [Parameter] public string ActiveSummaryBadgeKey { get; set; } = string.Empty;
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public decimal AverageProgress { get; set; }
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public EventCallback<string> SummaryBadgeSelected { get; set; }
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
}
