using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

public partial class PhuCapTongHopManualEditPopup
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    private static readonly Dictionary<string, object> HeaderIconAttributes =
        new Dictionary<string, object>
        {
            ["aria-hidden"] = "true",
            ["tabindex"] = "-1"
        };

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter, EditorRequired] public PhuCapTongHopManualEditModel Model { get; set; } = default!;
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }

    private EditContext? editContext;
    private PhuCapTongHopManualEditModel? configuredModel;

    protected override void OnParametersSet()
    {
        if(!ReferenceEquals(configuredModel, Model))
        {
            configuredModel = Model;
            editContext = new EditContext(Model);
        }
    }

    private Task OnVisibleChangedAsync(bool visible) =>
        !visible && IsSaving ? Task.CompletedTask : VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() =>
        IsSaving ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);

    private Task SaveAsync() =>
        IsSaving || !CanSave || editContext is null || !editContext.Validate()
            ? Task.CompletedTask
            : SaveRequested.InvokeAsync();

    private static string FormatMoney(decimal value) =>
        value == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", value);
}
