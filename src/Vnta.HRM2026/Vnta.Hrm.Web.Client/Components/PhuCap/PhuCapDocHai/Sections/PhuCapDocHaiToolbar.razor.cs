using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Sections;

public partial class PhuCapDocHaiToolbar
{
    [Parameter] public int Month { get; set; }
    [Parameter] public int Year { get; set; }
    [Parameter] public int MinimumYear { get; set; }
    [Parameter] public int MaximumYear { get; set; }
    [Parameter] public IReadOnlyList<PhuCapDocHaiMonthOption> AvailableMonths { get; set; } = [];
    [Parameter] public bool CanChangeFilters { get; set; }
    [Parameter] public bool CanView { get; set; }
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public bool CanOperateOnCurrentDataset { get; set; }
    [Parameter] public bool CanRecalculate { get; set; }
    [Parameter] public bool CanOpenLockAction { get; set; }
    [Parameter] public bool CanOpenUnlockAction { get; set; }
    [Parameter] public bool CanSetSelectedEntitlement { get; set; }
    [Parameter] public bool CanExport { get; set; }
    [Parameter] public bool CanExportSelected { get; set; }
    [Parameter] public EventCallback<int> MonthChanged { get; set; }
    [Parameter] public EventCallback<int> YearChanged { get; set; }
    [Parameter] public EventCallback ViewRequested { get; set; }
    [Parameter] public EventCallback RecalculateRequested { get; set; }
    [Parameter] public EventCallback ExcludeSelectedRequested { get; set; }
    [Parameter] public EventCallback IncludeSelectedRequested { get; set; }
    [Parameter] public EventCallback LockRequested { get; set; }
    [Parameter] public EventCallback UnlockRequested { get; set; }
    [Parameter] public EventCallback RulesRequested { get; set; }
    [Parameter] public EventCallback ExportExcelRequested { get; set; }
    [Parameter] public EventCallback ExportPdfRequested { get; set; }
    [Parameter] public EventCallback BackgroundCsvRequested { get; set; }
    [Parameter] public EventCallback ExportSelectedExcelRequested { get; set; }
    [Parameter] public EventCallback ExportSelectedPdfRequested { get; set; }
    [Parameter] public EventCallback ColumnChooserRequested { get; set; }
}
