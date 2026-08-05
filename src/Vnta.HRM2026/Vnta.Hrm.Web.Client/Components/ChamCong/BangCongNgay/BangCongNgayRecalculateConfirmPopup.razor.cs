using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.ChamCong.BangCongNgay;

/// <summary>Hiển thị xác nhận trước khi tính lại bảng công của một ngày đã tải.</summary>
public partial class BangCongNgayRecalculateConfirmPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsRebuilding { get; set; }
    [Parameter] public string WorkDateLabel { get; set; } = string.Empty;
    [Parameter] public EventCallback ConfirmRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    private Task ConfirmAsync() => ConfirmRequested.InvokeAsync();
}
