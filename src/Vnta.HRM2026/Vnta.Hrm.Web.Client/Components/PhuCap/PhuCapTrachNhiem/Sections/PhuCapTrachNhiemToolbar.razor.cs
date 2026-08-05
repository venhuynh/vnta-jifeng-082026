using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Toolbar thuần trình bày; mọi workflow do page coordinator sở hữu.</summary>
public partial class PhuCapTrachNhiemToolbar
{
    [Parameter, EditorRequired] public int Month { get; set; }
    [Parameter, EditorRequired] public int Year { get; set; }
    [Parameter, EditorRequired] public IReadOnlyList<MonthOption> AvailableMonths { get; set; } = [];
    [Parameter, EditorRequired] public int MinimumYear { get; set; }
    [Parameter, EditorRequired] public int MaximumYear { get; set; }
    [Parameter] public bool CanChangeFilters { get; set; }
    [Parameter] public bool CanView { get; set; }
    [Parameter] public bool CanOperate { get; set; }
    [Parameter] public bool CanRecalculate { get; set; }
    [Parameter] public bool CanLock { get; set; }
    [Parameter] public bool CanUnlock { get; set; }
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public bool CanExport { get; set; }
    [Parameter] public EventCallback<int> MonthChanged { get; set; }
    [Parameter] public EventCallback<int> YearChanged { get; set; }
    [Parameter] public EventCallback ViewRequested { get; set; }
    [Parameter] public EventCallback CalculateAbcRequested { get; set; }
    [Parameter] public EventCallback RecalculateRequested { get; set; }
    [Parameter] public EventCallback PerformanceBonusRequested { get; set; }
    [Parameter] public EventCallback LockRequested { get; set; }
    [Parameter] public EventCallback UnlockRequested { get; set; }
    [Parameter] public EventCallback RulesRequested { get; set; }
    [Parameter] public EventCallback ExportExcelRequested { get; set; }
    [Parameter] public EventCallback ExportPdfRequested { get; set; }
    [Parameter] public EventCallback ColumnChooserRequested { get; set; }
}
