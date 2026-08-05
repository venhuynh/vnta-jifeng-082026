using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN.Dialogs;

public partial class KhauTruThueTNCNEditPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = "Điều chỉnh Thuế TNCN";
    [Parameter] public KhauTruThueTNCNEditModel Model { get; set; } = new();
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }
}
