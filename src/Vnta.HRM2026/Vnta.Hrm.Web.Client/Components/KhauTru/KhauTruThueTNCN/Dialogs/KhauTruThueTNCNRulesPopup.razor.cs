using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN.Dialogs;

/// <summary>Popup chỉ đọc mô tả phạm vi dữ liệu và các nội dung nghiệp vụ Thuế TNCN đang chờ chốt.</summary>
public partial class KhauTruThueTNCNRulesPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }

    /// <summary>Đồng bộ trạng thái hiển thị popup với màn hình cha.</summary>
    private Task OnVisibleChanged(bool visible) => VisibleChanged.InvokeAsync(visible);

    /// <summary>Đóng popup quy tắc.</summary>
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
}
