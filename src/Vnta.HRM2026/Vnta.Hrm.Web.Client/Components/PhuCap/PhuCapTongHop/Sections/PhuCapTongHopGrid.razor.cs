using Microsoft.AspNetCore.Components;
using DevExpress.Blazor;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Grid trình bày dữ liệu và phát sự kiện thao tác trên từng dòng.</summary>
public partial class PhuCapTongHopGrid
{
    private IGrid? grid;
    private IGrid? attachedGrid;

    [Parameter] public IReadOnlyList<PayrollAllowanceSummaryRecord> Records { get; set; } = [];
    [Parameter] public IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    [Parameter] public int CurrentPageIndex { get; set; }
    [Parameter] public int PageSize { get; set; }
    [Parameter] public bool CanOperate { get; set; }
    [Parameter] public string EmptyStateTitle { get; set; } = string.Empty;
    [Parameter] public string EmptyStateMessage { get; set; } = string.Empty;
    [Parameter] public string EmptyStateActionText { get; set; } = string.Empty;
    [Parameter] public Func<PayrollAllowanceSummaryRecord, bool> CanEditRow { get; set; } = _ => false;
    [Parameter] public Func<PayrollAllowanceSummaryRecord, bool> CanRefreshRow { get; set; } = _ => false;
    [Parameter] public Func<PayrollAllowanceSummaryRecord, bool> CanToggleLock { get; set; } = _ => false;
    [Parameter] public Func<PayrollAllowanceSummaryRecord, bool> CanViewMonthlyWork { get; set; } = _ => false;
    [Parameter] public Func<PayrollAllowanceSummaryRecord, string> LockActionText { get; set; } = _ => string.Empty;
    [Parameter] public Func<string?, MarkupString> HighlightSearchText { get; set; } = _ => new MarkupString(string.Empty);
    [Parameter] public Func<decimal, string> FormatMoney { get; set; } = _ => string.Empty;
    [Parameter] public Func<bool, string> LockBadgeCssClass { get; set; } = _ => string.Empty;
    [Parameter] public EventCallback<IReadOnlyList<object>> SelectedDataItemsChanged { get; set; }
    [Parameter] public EventCallback<GridFilterCriteriaChangedEventArgs> FilterCriteriaChanged { get; set; }
    [Parameter] public EventCallback EmptyStateActionRequested { get; set; }
    [Parameter] public EventCallback<PayrollAllowanceSummaryRecord> EditRequested { get; set; }
    [Parameter] public EventCallback<PayrollAllowanceSummaryRecord> RefreshRequested { get; set; }
    [Parameter] public EventCallback<PayrollAllowanceSummaryRecord> ToggleLockRequested { get; set; }
    [Parameter] public EventCallback<PayrollAllowanceSummaryRecord> MonthlyWorkRequested { get; set; }
    [Parameter] public Action<IGrid?> GridAttached { get; set; } = _ => { };

    protected override void OnAfterRender(bool firstRender)
    {
        if (grid is not null && !ReferenceEquals(grid, attachedGrid))
        {
            attachedGrid = grid;
            GridAttached(grid);
        }
    }

    public async Task ClearSelectionAsync()
    {
        if (grid is null)
        {
            return;
        }

        await grid.DeselectAllAsync();
        grid.SetFocusedRowIndex(-1);
    }

    public void ShowColumnChooser() => grid?.ShowColumnChooser();
}
