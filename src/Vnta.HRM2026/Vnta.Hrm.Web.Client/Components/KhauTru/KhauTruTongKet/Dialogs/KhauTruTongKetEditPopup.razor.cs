using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop;

public partial class KhauTruTongKetEditPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = "Điều chỉnh khoản khấu trừ khác";
    [Parameter, EditorRequired] public KhauTruTongKetEditModel Model { get; set; } = default!;
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    private Task SaveAsync() => SaveRequested.InvokeAsync();
}
