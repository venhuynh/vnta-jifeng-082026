using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

public partial class PhuCapTrachNhiemLoadError
{
    [Parameter] public string? Message { get; set; }
    [Parameter] public EventCallback RetryRequested { get; set; }
}
