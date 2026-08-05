using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Sections;

public partial class PhuCapTrachNhiemKhacLoadError
{
    [Parameter, EditorRequired]
    public global::Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.OtherResponsibilityAllowanceLoadErrorState State { get; set; } = default!;

    [Parameter]
    public EventCallback RetryRequested { get; set; }
}
