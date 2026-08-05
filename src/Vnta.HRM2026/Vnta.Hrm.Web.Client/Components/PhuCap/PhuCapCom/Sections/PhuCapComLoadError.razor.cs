using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

public partial class PhuCapComLoadError
{
    [Parameter] public string? Message { get; set; }
    [Parameter] public EventCallback RetryRequested { get; set; }
}
