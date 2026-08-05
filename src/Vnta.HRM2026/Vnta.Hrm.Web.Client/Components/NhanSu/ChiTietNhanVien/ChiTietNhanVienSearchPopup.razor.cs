using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models.NhanSu.ChiTietNhanVien;

namespace Vnta.Hrm.Web.Client.Components.NhanSu.ChiTietNhanVien;

public partial class ChiTietNhanVienSearchPopup
{
    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public IReadOnlyList<ChiTietNhanVienRecord> Items { get; set; } = [];

    [Parameter]
    public string? SearchText { get; set; }

    [Parameter]
    public EventCallback<string?> SearchTextChanged { get; set; }

    [Parameter]
    public bool IsBusy { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public EventCallback<ChiTietNhanVienRecord> EmployeeSelected { get; set; }

    private string EmptyTitle => string.IsNullOrWhiteSpace(SearchText)
        ? "Chưa có nhân viên"
        : "Không tìm thấy nhân viên";

    private string EmptyMessage => string.IsNullOrWhiteSpace(SearchText)
        ? "Danh sách nhân viên sẽ hiển thị tại đây."
        : "Hãy thử từ khóa khác hoặc xóa nội dung tìm kiếm.";

    private async Task OnVisibleChangedAsync(bool visible)
    {
        if(visible || !IsBusy)
        {
            await VisibleChanged.InvokeAsync(visible);
        }
    }

    private async Task OnRowClickAsync(GridRowClickEventArgs args)
    {
        if(args.Grid.GetDataItem(args.VisibleIndex) is ChiTietNhanVienRecord employee)
        {
            await EmployeeSelected.InvokeAsync(employee);
        }
    }
}
