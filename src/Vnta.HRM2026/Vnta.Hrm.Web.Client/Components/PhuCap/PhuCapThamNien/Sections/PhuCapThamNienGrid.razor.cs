using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

public partial class PhuCapThamNienGrid
{
    private IGrid? Grid { get; set; }

    [Parameter, EditorRequired] public IReadOnlyList<PhuCapThamNienRecord> Records { get; set; } = [];
    [Parameter] public IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    [Parameter] public int CurrentPageIndex { get; set; }
    [Parameter] public int PageSize { get; set; }
    [Parameter] public string EmptyStateTitle { get; set; } = string.Empty;
    [Parameter] public string EmptyStateMessage { get; set; } = string.Empty;
    [Parameter] public string EmptyStateActionText { get; set; } = string.Empty;
    [Parameter] public bool CanOperate { get; set; }
    [Parameter, EditorRequired] public Func<PhuCapThamNienRecord, bool> CanEdit { get; set; } = default!;
    [Parameter, EditorRequired] public Func<PhuCapThamNienRecord, bool> CanRefresh { get; set; } = default!;
    [Parameter, EditorRequired] public Func<PhuCapThamNienRecord, bool> CanToggleLock { get; set; } = default!;
    [Parameter, EditorRequired] public Func<PhuCapThamNienRecord, bool> CanViewMonthlyWork { get; set; } = default!;
    [Parameter, EditorRequired] public Func<string?, string> GetRuleStatusCssClass { get; set; } = default!;
    [Parameter, EditorRequired] public Func<bool, string> GetLockStatusCssClass { get; set; } = default!;
    [Parameter, EditorRequired] public Func<decimal, string> FormatCurrency { get; set; } = default!;
    [Parameter, EditorRequired] public Func<decimal?, string> FormatAdministrativeWorkDays { get; set; } = default!;
    [Parameter, EditorRequired] public Func<decimal?, string> FormatWorkDays { get; set; } = default!;
    [Parameter, EditorRequired] public Func<string?, MarkupString> HighlightSearchText { get; set; } = default!;
    [Parameter] public EventCallback<IReadOnlyList<object>> SelectedDataItemsChanged { get; set; }
    [Parameter] public EventCallback EmptyStateActionRequested { get; set; }
    [Parameter] public EventCallback<PhuCapThamNienRecord> EditRequested { get; set; }
    [Parameter] public EventCallback<PhuCapThamNienRecord> RefreshRequested { get; set; }
    [Parameter] public EventCallback<PhuCapThamNienRecord> LockToggleRequested { get; set; }
    [Parameter] public EventCallback<PhuCapThamNienRecord> MonthlyWorkRequested { get; set; }

    public Task ClearSelectionAsync()
    {
        Grid?.SetFocusedRowIndex(-1);
        return Grid?.DeselectAllAsync() ?? Task.CompletedTask;
    }

    public void ShowColumnChooser() => Grid?.ShowColumnChooser();
}
