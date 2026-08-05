using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLeGrid
{
    private IGrid? grid;

        [Parameter, EditorRequired] public IReadOnlyList<LeaveHolidayAllowanceRecord> Rows { get; set; } = [];
        [Parameter] public IReadOnlyList<object> SelectedItems { get; set; } = [];
        [Parameter] public int PageIndex { get; set; }
        [Parameter] public int PageSize { get; set; }
        [Parameter] public bool CanOperateOnCurrentDataset { get; set; }
        [Parameter, EditorRequired] public Func<LeaveHolidayAllowanceRecord, bool> CanEdit { get; set; } = _ => false;
        [Parameter, EditorRequired] public Func<LeaveHolidayAllowanceRecord, bool> CanRefresh { get; set; } = _ => false;
        [Parameter, EditorRequired] public Func<LeaveHolidayAllowanceRecord, bool> CanToggleLock { get; set; } = _ => false;
        [Parameter, EditorRequired] public Func<LeaveHolidayAllowanceRecord, bool> CanViewMonthlyWork { get; set; } = _ => false;
        [Parameter, EditorRequired] public Func<string?, MarkupString> HighlightText { get; set; } = _ => new MarkupString(string.Empty);
        [Parameter, EditorRequired] public Func<decimal, string> FormatMoney { get; set; } = _ => string.Empty;
        [Parameter, EditorRequired] public Func<decimal, string> FormatQuantity { get; set; } = _ => string.Empty;
        [Parameter, EditorRequired] public Func<bool, string> GetLockStatusCssClass { get; set; } = _ => string.Empty;
        [Parameter, EditorRequired] public Func<string?, string> GetNoteCssClass { get; set; } = _ => string.Empty;
        [Parameter, EditorRequired] public Func<string?, string> GetNotePreview { get; set; } = _ => string.Empty;
        [Parameter] public string EmptyStateTitle { get; set; } = string.Empty;
        [Parameter] public string EmptyStateMessage { get; set; } = string.Empty;
        [Parameter] public string EmptyStateActionText { get; set; } = string.Empty;
        [Parameter] public EventCallback<IReadOnlyList<object>> SelectedItemsChanged { get; set; }
        [Parameter] public EventCallback<LeaveHolidayAllowanceRecord> ManualEditRequested { get; set; }
        [Parameter] public EventCallback<LeaveHolidayAllowanceRecord> RefreshRequested { get; set; }
        [Parameter] public EventCallback<LeaveHolidayAllowanceRecord> LockToggleRequested { get; set; }
        [Parameter] public EventCallback<LeaveHolidayAllowanceRecord> MonthlyWorkRequested { get; set; }
        [Parameter] public EventCallback EmptyStateActionRequested { get; set; }

        public async Task ClearSelectionAsync()
        {
            if (grid is null) return;
            await grid.DeselectAllAsync();
            grid.SetFocusedRowIndex(-1);
        }

        public void ShowColumnChooser() => grid?.ShowColumnChooser();

        public Task ExportSelectedToExcelAsync() => grid?.ExportToXlsxAsync("leave-holiday-allowances-selected", new GridXlExportOptions { ExportSelectedRowsOnly = true }) ?? Task.CompletedTask;
        public Task ExportSelectedToPdfAsync() => grid?.ExportToPdfAsync("leave-holiday-allowances-selected", new GridPdfExportOptions { ExportSelectedRowsOnly = true }) ?? Task.CompletedTask;
}
