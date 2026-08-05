using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Sections;

public partial class PhuCapKhacLoadError
{
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback RetryRequested { get; set; }
}
