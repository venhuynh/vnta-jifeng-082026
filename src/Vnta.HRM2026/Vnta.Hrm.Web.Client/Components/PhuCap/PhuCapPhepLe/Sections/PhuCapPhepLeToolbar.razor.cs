using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLeToolbar
{
    [Parameter] public int Month { get; set; }
        [Parameter] public int Year { get; set; }
        [Parameter] public int MinimumYear { get; set; }
        [Parameter] public int MaximumYear { get; set; }
        [Parameter] public IReadOnlyList<LeaveHolidayAllowanceMonthOption> AvailableMonths { get; set; } = [];
        [Parameter] public bool CanChangeFilters { get; set; }
        [Parameter] public bool CanView { get; set; }
        [Parameter] public bool CanInteract { get; set; }
        [Parameter] public bool CanOperateOnCurrentDataset { get; set; }
        [Parameter] public bool CanRecalculate { get; set; }
        [Parameter] public bool CanOpenLockAction { get; set; }
        [Parameter] public bool CanOpenUnlockAction { get; set; }
        [Parameter] public bool CanExport { get; set; }
        [Parameter] public bool CanExportSelected { get; set; }
        [Parameter] public EventCallback<int> MonthChanged { get; set; }
        [Parameter] public EventCallback<int> YearChanged { get; set; }
        [Parameter] public EventCallback ViewRequested { get; set; }
        [Parameter] public EventCallback RecalculateRequested { get; set; }
        [Parameter] public EventCallback LockRequested { get; set; }
        [Parameter] public EventCallback UnlockRequested { get; set; }
        [Parameter] public EventCallback RulesRequested { get; set; }
        [Parameter] public EventCallback ExportAllExcelRequested { get; set; }
        [Parameter] public EventCallback ExportAllPdfRequested { get; set; }
        [Parameter] public EventCallback ExportSelectedExcelRequested { get; set; }
        [Parameter] public EventCallback ExportSelectedPdfRequested { get; set; }
        [Parameter] public EventCallback ColumnChooserRequested { get; set; }
}
