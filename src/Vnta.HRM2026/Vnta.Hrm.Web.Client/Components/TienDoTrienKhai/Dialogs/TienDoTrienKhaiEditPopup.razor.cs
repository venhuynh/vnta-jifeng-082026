using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Dialogs;

/// <summary>Popup thuần trình bày để thêm hoặc cập nhật hạng mục triển khai.</summary>
public partial class TienDoTrienKhaiEditPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public ProjectImplementationProgressEditModel Model { get; set; } = default!;
    [Parameter] public EditContext EditContext { get; set; } = default!;
    [Parameter] public IReadOnlyList<ProjectImplementationProgressStatusDefinition> StatusOptions { get; set; } = [];
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) =>
        !visible && IsSaving ? Task.CompletedTask : VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() => IsSaving ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);

    private Task SaveAsync() => SaveRequested.InvokeAsync();
}
