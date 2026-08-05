using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruKhac;

public partial class KhauTruKhacEditPopup
{
    private KhauTruKhacEditModel? observedModel;
    private EditContext editContext = new(new KhauTruKhacEditModel());
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = "Sửa khấu trừ khác";
    [Parameter] public KhauTruKhacEditModel Model { get; set; } = new();
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    protected override void OnParametersSet()
    {
        if(ReferenceEquals(observedModel, Model))
        {
            return;
        }

        observedModel = Model;
        editContext = new EditContext(Model);
    }

    private Task SaveAsync()
    {
        return editContext.Validate() ? SaveRequested.InvokeAsync() : Task.CompletedTask;
    }
}
