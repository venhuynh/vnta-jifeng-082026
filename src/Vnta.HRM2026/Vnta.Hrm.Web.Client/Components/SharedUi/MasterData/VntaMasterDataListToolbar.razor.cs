using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.SharedUi.MasterData;

public partial class VntaMasterDataListToolbar
{
    [Parameter] public string? BreadcrumbText { get; set; }
    [Parameter] public RenderFragment? TitleTemplate { get; set; }
    [Parameter] public string AddText { get; set; } = "Mới";
    [Parameter] public string TitleTooltip { get; set; } = "Tiêu đề danh mục";
    [Parameter] public string AddTooltip { get; set; } = "Tạo bản ghi danh mục mới";
    [Parameter] public string EditText { get; set; } = "Điều chỉnh";
    [Parameter] public string EditTooltip { get; set; } = "Điều chỉnh bản ghi danh mục đã chọn";
    [Parameter] public string ResetText { get; set; } = "Reset";
    [Parameter] public string ResetTooltip { get; set; } = "Đặt lại bộ lọc danh mục";
    [Parameter] public string DeleteText { get; set; } = "Xóa";
    [Parameter] public string DeleteTooltip { get; set; } = "Xóa bản ghi danh mục đã chọn";
    [Parameter] public string RefreshTooltip { get; set; } = "Làm mới danh sách danh mục";
    [Parameter] public string ExportText { get; set; } = "Xuất dữ liệu";
    [Parameter] public string ExportTooltip { get; set; } = "Xuất dữ liệu danh mục";
    [Parameter] public string ExportExcelText { get; set; } = "Xuất Excel";
    [Parameter] public string ExportExcelTooltip { get; set; } = "Xuất toàn bộ dữ liệu danh mục ra Excel";
    [Parameter] public string ExportPdfText { get; set; } = "Xuất PDF";
    [Parameter] public string ExportPdfTooltip { get; set; } = "Xuất toàn bộ dữ liệu danh mục ra PDF";
    [Parameter] public string ExportSelectedExcelText { get; set; } = "Xuất dòng đã chọn ra Excel";
    [Parameter] public string ExportSelectedExcelTooltip { get; set; } = "Xuất các dòng danh mục đã chọn ra Excel";
    [Parameter] public string ExportSelectedPdfText { get; set; } = "Xuất dòng đã chọn ra PDF";
    [Parameter] public string ExportSelectedPdfTooltip { get; set; } = "Xuất các dòng danh mục đã chọn ra PDF";
    [Parameter] public string ColumnChooserTooltip { get; set; } = "Chọn cột hiển thị trong danh mục";
    [Parameter] public string SearchTooltip { get; set; } = "Tìm kiếm trong danh mục";
    [Parameter] public string SearchPlaceholder { get; set; } = "Tìm kiếm";
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public bool AddEnabled { get; set; } = true;
    [Parameter] public bool EditEnabled { get; set; } = true;
    [Parameter] public bool ResetEnabled { get; set; } = true;
    [Parameter] public bool DeleteEnabled { get; set; } = true;
    [Parameter] public bool RefreshEnabled { get; set; } = true;
    [Parameter] public bool ExportEnabled { get; set; } = true;
    [Parameter] public bool ExportSelectedEnabled { get; set; } = true;
    [Parameter] public bool ColumnChooserEnabled { get; set; } = true;
    [Parameter] public bool SearchEnabled { get; set; } = true;
    [Parameter] public bool ShowAdd { get; set; }
    [Parameter] public bool ShowEdit { get; set; }
    [Parameter] public bool ShowReset { get; set; } = true;
    [Parameter] public bool ShowDelete { get; set; } = true;
    [Parameter] public bool ShowRefresh { get; set; } = true;
    [Parameter] public bool ShowExport { get; set; }
    [Parameter] public bool ShowColumnChooser { get; set; }
    [Parameter] public bool ShowSearch { get; set; } = true;
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
    [Parameter] public EventCallback OnAddClick { get; set; }
    [Parameter] public EventCallback OnEditClick { get; set; }
    [Parameter] public EventCallback OnResetClick { get; set; }
    [Parameter] public EventCallback OnDeleteClick { get; set; }
    [Parameter] public EventCallback OnRefreshClick { get; set; }
    [Parameter] public EventCallback OnExportAllDataToExcelClick { get; set; }
    [Parameter] public EventCallback OnExportAllDataToPdfClick { get; set; }
    [Parameter] public EventCallback OnExportSelectedRowsToExcelClick { get; set; }
    [Parameter] public EventCallback OnExportSelectedRowsToPdfClick { get; set; }
    [Parameter] public EventCallback OnColumnChooserClick { get; set; }

    private Task HandleSearchTextChanged(string? value) => SearchTextChanged.InvokeAsync(value);
}
