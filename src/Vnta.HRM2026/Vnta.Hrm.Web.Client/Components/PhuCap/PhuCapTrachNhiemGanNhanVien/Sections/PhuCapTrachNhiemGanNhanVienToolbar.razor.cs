using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Sections;

public partial class PhuCapTrachNhiemGanNhanVienToolbar
{
    [Parameter] public int Month { get; set; }
    [Parameter] public int Year { get; set; }
    [Parameter] public int MinimumYear { get; set; }
    [Parameter] public int MaximumYear { get; set; }
    [Parameter] public IReadOnlyList<EmployeeAssignmentMonthOption> MonthOptions { get; set; } = [];
    [Parameter] public bool CanChangeFilters { get; set; }
    [Parameter] public bool CanLoad { get; set; }
    [Parameter] public bool CanLoadFromPreviousMonth { get; set; }
    [Parameter] public bool CanManageAssignments { get; set; }
    [Parameter] public bool CanExport { get; set; }
    [Parameter] public EventCallback<int> MonthChanged { get; set; }
    [Parameter] public EventCallback<int> YearChanged { get; set; }
    [Parameter] public EventCallback ViewRequested { get; set; }
    [Parameter] public EventCallback LoadFromPreviousMonthRequested { get; set; }
    [Parameter] public EventCallback ExportExcelRequested { get; set; }
    [Parameter] public EventCallback ExportPdfRequested { get; set; }
    [Parameter] public EventCallback ColumnChooserRequested { get; set; }
}
