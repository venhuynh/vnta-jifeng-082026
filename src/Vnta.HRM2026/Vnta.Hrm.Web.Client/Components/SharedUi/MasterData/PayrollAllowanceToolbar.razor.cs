using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.SharedUi.MasterData;

public partial class PayrollAllowanceToolbar
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public int Month { get; set; }

    [Parameter]
    public EventCallback<int> MonthChanged { get; set; }

    [Parameter]
    public int Year { get; set; }

    [Parameter]
    public EventCallback<int> YearChanged { get; set; }

    [Parameter]
    public int MinimumYear { get; set; } = 2000;

    [Parameter]
    public int MaximumYear { get; set; } = 2100;

    [Parameter]
    public bool CanChangePeriod { get; set; } = true;

    [Parameter]
    public string MonthTooltip { get; set; } = "Chọn tháng kỳ lương";

    [Parameter]
    public string YearTooltip { get; set; } = "Chọn năm kỳ lương";

    [Parameter]
    public RenderFragment? PeriodTemplate { get; set; }

    [Parameter]
    public RenderFragment? AdditionalBusinessActionsTemplate { get; set; }

    [Parameter]
    public string CreateText { get; set; } = "Thêm";

    [Parameter]
    public string EditText { get; set; } = "Điều chỉnh";

    [Parameter]
    public string DeleteText { get; set; } = "Xóa";

    [Parameter]
    public string RefreshText { get; set; } = "Làm mới";

    [Parameter]
    public string ExportText { get; set; } = "Xuất dữ liệu";

    [Parameter]
    public string ExportExcelText { get; set; } = "Xuất Excel";

    [Parameter]
    public string ExportPdfText { get; set; } = "Xuất PDF";

    [Parameter]
    public string ExportSelectedExcelText { get; set; } = "Xuất dòng đã chọn ra Excel";

    [Parameter]
    public string ExportSelectedPdfText { get; set; } = "Xuất dòng đã chọn ra PDF";

    [Parameter]
    public bool ShowCreateAction { get; set; } = true;

    [Parameter]
    public bool ShowEditAction { get; set; } = true;

    [Parameter]
    public bool ShowDeleteAction { get; set; } = true;

    [Parameter]
    public bool ShowRefreshAction { get; set; } = true;

    [Parameter]
    public bool ShowExportAction { get; set; } = true;

    [Parameter]
    public bool ShowColumnChooserAction { get; set; } = true;

    [Parameter]
    public bool ShowSearchBox { get; set; } = true;

    [Parameter]
    public bool ShowViewAction { get; set; } = true;

    [Parameter]
    public bool ShowSyncPreviousMonthAction { get; set; } = true;

    [Parameter]
    public bool CreateEnabled { get; set; }

    [Parameter]
    public bool EditEnabled { get; set; }

    [Parameter]
    public bool DeleteEnabled { get; set; }

    [Parameter]
    public bool RefreshEnabled { get; set; }

    [Parameter]
    public bool ExportEnabled { get; set; }

    [Parameter]
    public bool ExportSelectedEnabled { get; set; }

    [Parameter]
    public bool ColumnChooserEnabled { get; set; }

    [Parameter]
    public bool SearchEnabled { get; set; } = true;

    [Parameter]
    public bool ViewEnabled { get; set; }

    [Parameter]
    public bool SyncPreviousMonthEnabled { get; set; }

    [Parameter]
    public string ViewText { get; set; } = "Xem";

    [Parameter]
    public string SyncPreviousMonthText { get; set; } = "Lấy từ tháng trước";

    [Parameter]
    public string? SearchText { get; set; }

    [Parameter]
    public EventCallback<string?> SearchTextChanged { get; set; }

    [Parameter]
    public string SearchPlaceholder { get; set; } = "Tìm kiếm";

    [Parameter]
    public string? CreateTooltip { get; set; }

    [Parameter]
    public string? EditTooltip { get; set; }

    [Parameter]
    public string? DeleteTooltip { get; set; }

    [Parameter]
    public string? RefreshTooltip { get; set; }

    [Parameter]
    public string? ExportTooltip { get; set; }

    [Parameter]
    public string? ExportExcelTooltip { get; set; }

    [Parameter]
    public string? ExportPdfTooltip { get; set; }

    [Parameter]
    public string? ExportSelectedExcelTooltip { get; set; }

    [Parameter]
    public string? ExportSelectedPdfTooltip { get; set; }

    [Parameter]
    public string? ColumnChooserTooltip { get; set; }

    [Parameter]
    public string? SearchTooltip { get; set; }

    [Parameter]
    public string? ViewTooltip { get; set; }

    [Parameter]
    public string? SyncPreviousMonthTooltip { get; set; }

    [Parameter]
    public EventCallback CreateRequested { get; set; }

    [Parameter]
    public EventCallback EditRequested { get; set; }

    [Parameter]
    public EventCallback DeleteRequested { get; set; }

    [Parameter]
    public EventCallback RefreshRequested { get; set; }

    [Parameter]
    public EventCallback ExportAllExcelRequested { get; set; }

    [Parameter]
    public EventCallback ExportAllPdfRequested { get; set; }

    [Parameter]
    public EventCallback ExportSelectedExcelRequested { get; set; }

    [Parameter]
    public EventCallback ExportSelectedPdfRequested { get; set; }

    [Parameter]
    public EventCallback ColumnChooserRequested { get; set; }

    [Parameter]
    public EventCallback ViewRequested { get; set; }

    [Parameter]
    public EventCallback SyncPreviousMonthRequested { get; set; }

    private Task HandleMonthChanged(int value) => MonthChanged.InvokeAsync(value);

    private Task HandleYearChanged(int value) => YearChanged.InvokeAsync(value);
}
