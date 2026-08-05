using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Presentation-only toolbar for the allowance-summary page.</summary>
public partial class PhuCapTongHopToolbar
{
    [Parameter] public int Month { get; set; }
    [Parameter] public int Year { get; set; }
    [Parameter] public int MinimumYear { get; set; }
    [Parameter] public int MaximumYear { get; set; }
    [Parameter] public IReadOnlyList<MonthOption> AvailableMonths { get; set; } = [];
    [Parameter] public bool CanChangeFilters { get; set; }
    [Parameter] public bool CanView { get; set; }
    [Parameter] public bool CanOperate { get; set; }
    [Parameter] public bool CanSync { get; set; }
    [Parameter] public bool CanRefresh { get; set; }
    [Parameter] public bool CanLock { get; set; }
    [Parameter] public bool CanUnlock { get; set; }
    [Parameter] public bool CanExport { get; set; }
    [Parameter] public string ExportTooltip { get; set; } = string.Empty;
    [Parameter] public string ExportExcelTooltip { get; set; } = string.Empty;
    [Parameter] public string ExportPdfTooltip { get; set; } = string.Empty;
    [Parameter] public EventCallback<int> YearChanged { get; set; }
    [Parameter] public EventCallback<int> MonthChanged { get; set; }
    [Parameter] public EventCallback ViewRequested { get; set; }
    [Parameter] public EventCallback SyncRequested { get; set; }
    [Parameter] public EventCallback RefreshRequested { get; set; }
    [Parameter] public EventCallback LockRequested { get; set; }
    [Parameter] public EventCallback UnlockRequested { get; set; }
    [Parameter] public EventCallback ExportExcelRequested { get; set; }
    [Parameter] public EventCallback ExportPdfRequested { get; set; }
    [Parameter] public EventCallback ColumnChooserRequested { get; set; }
}
