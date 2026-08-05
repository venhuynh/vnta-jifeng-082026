using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLeManualEditPopup
{
    [Parameter] public bool Visible { get; set; }
        [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
        [Parameter] public string Title { get; set; } = string.Empty;
        [Parameter] public LeaveHolidayManualEditModel? Model { get; set; }
        [Parameter] public EditContext? EditContext { get; set; }
        [Parameter] public string? ErrorMessage { get; set; }
        [Parameter] public bool IsSaving { get; set; }
        [Parameter] public string SaveButtonText { get; set; } = string.Empty;
        [Parameter] public bool CanSave { get; set; }
        [Parameter] public EventCallback CancelRequested { get; set; }
        [Parameter] public EventCallback SaveRequested { get; set; }

        private Task OnVisibleChangedAsync(bool value) => VisibleChanged.InvokeAsync(value);
}
