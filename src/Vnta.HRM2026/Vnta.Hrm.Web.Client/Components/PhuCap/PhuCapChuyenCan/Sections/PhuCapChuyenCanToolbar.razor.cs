using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Sections;

/// <summary>Thanh công cụ thuần trình bày của màn hình phụ cấp chuyên cần.</summary>
public partial class PhuCapChuyenCanToolbar
{
    [Parameter] public int Month { get; set; }
    [Parameter] public int Year { get; set; }
    [Parameter] public int MinimumYear { get; set; }
    [Parameter] public int MaximumYear { get; set; }
    [Parameter] public IReadOnlyList<MonthOption> AvailableMonths { get; set; } = [];
    [Parameter] public string AppliedPeriodLabel { get; set; } = string.Empty;
    [Parameter] public bool CanChangeFilters { get; set; }
    [Parameter] public bool CanView { get; set; }
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public bool CanOperate { get; set; }
    [Parameter] public bool CanRecalculate { get; set; }
    [Parameter] public bool CanLock { get; set; }
    [Parameter] public bool CanUnlock { get; set; }
    [Parameter] public bool CanExport { get; set; }
    [Parameter] public EventCallback<int> MonthChanged { get; set; }
    [Parameter] public EventCallback<int> YearChanged { get; set; }
    [Parameter] public EventCallback ViewRequested { get; set; }
    [Parameter] public EventCallback RecalculateRequested { get; set; }
    [Parameter] public EventCallback LockRequested { get; set; }
    [Parameter] public EventCallback UnlockRequested { get; set; }
    [Parameter] public EventCallback RulesRequested { get; set; }
    [Parameter] public EventCallback ExportExcelRequested { get; set; }
    [Parameter] public EventCallback ExportPdfRequested { get; set; }
    [Parameter] public EventCallback ColumnChooserRequested { get; set; }
}
