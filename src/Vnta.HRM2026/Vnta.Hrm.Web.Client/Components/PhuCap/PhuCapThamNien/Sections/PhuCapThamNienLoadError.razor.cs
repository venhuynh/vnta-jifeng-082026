using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

public partial class PhuCapThamNienLoadError
{
    [Parameter] public string? Message { get; set; }
    [Parameter] public EventCallback RetryRequested { get; set; }
}
