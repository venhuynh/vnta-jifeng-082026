using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Sections;

/// <summary>Chỉ trình bày dữ liệu và phát sự kiện thao tác trên từng dòng.</summary>
public partial class PhuCapChuyenCanGrid
{
    private IGrid? Grid { get; set; }

    [Parameter] public IReadOnlyList<AttendanceAllowanceResultRecord> Records { get; set; } = [];
    [Parameter] public IReadOnlyList<object> SelectedItems { get; set; } = [];
    [Parameter] public bool CanOperate { get; set; }
    [Parameter] public int PageSize { get; set; }
    [Parameter] public string EmptyStateTitle { get; set; } = string.Empty;
    [Parameter] public string EmptyStateMessage { get; set; } = string.Empty;
    [Parameter] public string EmptyStateActionText { get; set; } = string.Empty;
    [Parameter] public Func<AttendanceAllowanceResultRecord, bool> CanEditRow { get; set; } = _ => false;
    [Parameter] public Func<AttendanceAllowanceResultRecord, bool> CanRefreshRow { get; set; } = _ => false;
    [Parameter] public Func<AttendanceAllowanceResultRecord, bool> CanToggleLock { get; set; } = _ => false;
    [Parameter] public Func<AttendanceAllowanceResultRecord, bool> CanViewMonthlyWork { get; set; } = _ => false;
    [Parameter] public Func<string?, MarkupString> HighlightText { get; set; } = _ => new MarkupString(string.Empty);
    [Parameter] public Func<AttendanceAllowanceResultRecord, string> RuleSummary { get; set; } = _ => string.Empty;
    [Parameter] public Func<decimal, string> FormatMoney { get; set; } = _ => string.Empty;
    [Parameter] public Func<bool, string> LockBadgeCssClass { get; set; } = _ => string.Empty;
    [Parameter] public EventCallback<IReadOnlyList<object>> SelectedItemsChanged { get; set; }
    [Parameter] public EventCallback EmptyStateActionRequested { get; set; }
    [Parameter] public EventCallback<AttendanceAllowanceResultRecord> EditRequested { get; set; }
    [Parameter] public EventCallback<AttendanceAllowanceResultRecord> RefreshRequested { get; set; }
    [Parameter] public EventCallback<AttendanceAllowanceResultRecord> LockToggleRequested { get; set; }
    [Parameter] public EventCallback<AttendanceAllowanceResultRecord> MonthlyWorkRequested { get; set; }

    public Task ClearSelectionAsync()
    {
        Grid?.SetFocusedRowIndex(-1);
        return Grid?.DeselectAllAsync() ?? Task.CompletedTask;
    }

    public void ShowColumnChooser() => Grid?.ShowColumnChooser();
}
