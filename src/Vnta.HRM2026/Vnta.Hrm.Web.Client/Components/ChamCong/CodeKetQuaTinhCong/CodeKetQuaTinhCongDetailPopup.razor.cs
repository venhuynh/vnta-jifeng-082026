using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Components.ChamCong.CodeKetQuaTinhCong;

public partial class CodeKetQuaTinhCongDetailPopup
{
    [Parameter]
    public AttendanceStatusCodeRecord? Record { get; set; }

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    private Task OnVisibleChanged(bool visible) => VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() => OnVisibleChanged(false);

    private static string BuildDetailSubtitle(AttendanceStatusCodeRecord record) =>
        $"{record.Code} | {AttendanceStatusCodePresentation.GetKindText(record.Kind)}";
}
