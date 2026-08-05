using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Sections;

/// <summary>Chỉ trình bày dữ liệu tiến độ và phát sự kiện thao tác trên từng dòng.</summary>
public partial class TienDoTrienKhaiGrid
{
    private IGrid? Grid { get; set; }

    [Parameter] public IReadOnlyList<ProjectImplementationProgressItem> Records { get; set; } = [];
    [Parameter] public int StartRecordIndex { get; set; }
    [Parameter] public bool CanOperate { get; set; }
    [Parameter] public int PageSize { get; set; }
    [Parameter] public string EmptyStateTitle { get; set; } = string.Empty;
    [Parameter] public string EmptyStateMessage { get; set; } = string.Empty;
    [Parameter] public string EmptyStateActionText { get; set; } = string.Empty;
    [Parameter] public Func<ProjectImplementationProgressStatus, string> StatusLabel { get; set; } = _ => string.Empty;
    [Parameter] public Func<ProjectImplementationProgressStatus, string> StatusCssClass { get; set; } = _ => string.Empty;
    [Parameter] public Func<int, string> ProgressValueCssClass { get; set; } = _ => string.Empty;
    [Parameter] public EventCallback<ProjectImplementationProgressItem> EditRequested { get; set; }
    [Parameter] public EventCallback EmptyStateActionRequested { get; set; }

    public void ShowColumnChooser() => Grid?.ShowColumnChooser();
}
