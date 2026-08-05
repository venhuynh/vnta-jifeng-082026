using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.LenhMayChamCong;

public partial class LenhMayChamCongEditForm
{
    [Parameter] public LenhMayChamCongEditModel? Model { get; set; }

    [Parameter] public string? ErrorMessage { get; set; }
}
