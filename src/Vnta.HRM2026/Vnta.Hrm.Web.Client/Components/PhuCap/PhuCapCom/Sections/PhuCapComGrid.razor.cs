using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

public partial class PhuCapComGrid
{
    private IGrid? grid;

    [Parameter] public IReadOnlyList<MealAllowanceRecord> Records { get; set; } = [];
    [Parameter] public IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    [Parameter] public int CurrentPageIndex { get; set; }
    [Parameter] public int PageSize { get; set; }
    [Parameter] public bool CanOperate { get; set; }
    [Parameter] public string EmptyStateTitle { get; set; } = string.Empty;
    [Parameter] public string EmptyStateMessage { get; set; } = string.Empty;
    [Parameter] public string EmptyStateActionText { get; set; } = string.Empty;
    [Parameter] public string EmptyStateActionIcon { get; set; } = string.Empty;
    [Parameter] public Func<MealAllowanceRecord, bool> CanEditRow { get; set; } = _ => false;
    [Parameter] public Func<MealAllowanceRecord, bool> CanRefreshRow { get; set; } = _ => false;
    [Parameter] public Func<MealAllowanceRecord, bool> CanToggleLock { get; set; } = _ => false;
    [Parameter] public Func<MealAllowanceRecord, bool> CanViewMonthlyWork { get; set; } = _ => false;
    [Parameter] public Func<string?, string, MarkupString> Highlight { get; set; } = (_, fallback) => new MarkupString(fallback);
    [Parameter] public Func<decimal, string> FormatMoney { get; set; } = value => value.ToString();
    [Parameter] public Func<int, string> FormatValue { get; set; } = value => value.ToString();
    [Parameter] public EventCallback<IReadOnlyList<object>> SelectedDataItemsChanged { get; set; }
    [Parameter] public EventCallback<GridFilterCriteriaChangedEventArgs> FilterCriteriaChanged { get; set; }
    [Parameter] public EventCallback<MealAllowanceRecord> EditRequested { get; set; }
    [Parameter] public EventCallback<MealAllowanceRecord> RefreshRequested { get; set; }
    [Parameter] public EventCallback<MealAllowanceRecord> ToggleLockRequested { get; set; }
    [Parameter] public EventCallback<MealAllowanceRecord> MonthlyWorkRequested { get; set; }
    [Parameter] public EventCallback EmptyStateActionRequested { get; set; }

    public void ShowColumnChooser() => grid?.ShowColumnChooser();

    public void SetFocusedRowIndex(int index) => grid?.SetFocusedRowIndex(index);

    private static string FormatOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
