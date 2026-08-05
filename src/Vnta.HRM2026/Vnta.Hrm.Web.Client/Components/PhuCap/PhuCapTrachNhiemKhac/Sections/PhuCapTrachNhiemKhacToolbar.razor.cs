using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Sections;

public partial class PhuCapTrachNhiemKhacToolbar
{
    [Parameter, EditorRequired]
    public global::Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.OtherResponsibilityAllowanceToolbarState State { get; set; } = default!;

    [Parameter] public EventCallback<int> MonthChanged { get; set; }
    [Parameter] public EventCallback<int> YearChanged { get; set; }
    [Parameter] public EventCallback ViewRequested { get; set; }
    [Parameter] public EventCallback RecalculateRequested { get; set; }
    [Parameter] public EventCallback LockRequested { get; set; }
    [Parameter] public EventCallback UnlockRequested { get; set; }
    [Parameter] public EventCallback RulesRequested { get; set; }
    [Parameter] public EventCallback ExportExcelRequested { get; set; }
    [Parameter] public EventCallback ExportPdfRequested { get; set; }
    [Parameter] public EventCallback ExportSelectedExcelRequested { get; set; }
    [Parameter] public EventCallback ExportSelectedPdfRequested { get; set; }
    [Parameter] public EventCallback ColumnChooserRequested { get; set; }

    private int Month => State.Month;
    private int Year => State.Year;
    private int MinimumYear => State.MinimumYear;
    private int MaximumYear => State.MaximumYear;
    private IReadOnlyList<OtherResponsibilityAllowanceMonthOption> AvailableMonths => State.AvailableMonths;
    private bool CanChangeFilters => State.CanChangeFilters;
    private bool CanView => State.CanView;
    private bool CanOpenActionsMenu => State.CanOpenActionsMenu;
    private bool CanUseAppliedPeriodActions => State.CanUseAppliedPeriodActions;
    private bool CanOpenRules => State.CanOpenRules;
    private bool CanExport => State.CanExport;
    private bool CanExportSelected => State.CanExportSelected;
}
