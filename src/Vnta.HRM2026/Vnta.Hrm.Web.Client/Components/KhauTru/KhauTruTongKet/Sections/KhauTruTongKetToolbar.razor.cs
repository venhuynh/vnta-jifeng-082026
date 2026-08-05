using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Sections;

/// <summary>
/// Toolbar thuần trình bày của màn hình tổng kết khấu trừ.
/// Chủ sở hữu truyền trạng thái hiển thị và xử lý toàn bộ hành động qua callback.
/// </summary>
public partial class KhauTruTongKetToolbar<TMonthOption>
{
    [Parameter] public int Month { get; set; }
    [Parameter] public int Year { get; set; }
    [Parameter] public int MinimumYear { get; set; }
    [Parameter] public int MaximumYear { get; set; }
    [Parameter] public IReadOnlyList<TMonthOption> AvailableMonths { get; set; } = [];
    [Parameter] public string ValueFieldName { get; set; } = "Value";
    [Parameter] public string TextFieldName { get; set; } = "Text";

    [Parameter] public bool CanChangeFilters { get; set; }
    [Parameter] public bool CanView { get; set; }
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public bool CanOperateOnCurrentDataset { get; set; }
    [Parameter] public bool CanSyncFromPreviousMonth { get; set; }
    [Parameter] public bool CanRecalculate { get; set; }
    [Parameter] public bool CanOpenLockAction { get; set; }
    [Parameter] public bool CanOpenUnlockAction { get; set; }
    [Parameter] public bool CanExport { get; set; }
    [Parameter] public bool CanExportSelected { get; set; }

    [Parameter] public EventCallback<int> MonthChanged { get; set; }
    [Parameter] public EventCallback<int> YearChanged { get; set; }
    [Parameter] public EventCallback ViewRequested { get; set; }
    [Parameter] public EventCallback SyncFromPreviousMonthRequested { get; set; }
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
