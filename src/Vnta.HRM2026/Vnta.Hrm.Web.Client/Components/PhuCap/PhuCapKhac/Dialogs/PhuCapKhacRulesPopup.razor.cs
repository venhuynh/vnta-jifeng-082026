using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.State;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

/// <summary>Hiển thị quy tắc tĩnh hiện hành của màn hình Phụ cấp khác.</summary>
public partial class PhuCapKhacRulesPopup
{
    [Parameter, EditorRequired] public OtherAllowanceRulesDialogState State { get; set; } = default!;
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public string PeriodLabel { get; set; } = string.Empty;

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
}
