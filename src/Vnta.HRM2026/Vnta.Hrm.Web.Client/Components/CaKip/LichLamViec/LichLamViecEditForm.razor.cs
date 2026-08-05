using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Components.CaKip.LichLamViec;

public partial class LichLamViecEditForm
{
    [Parameter]
    public AttendanceWorkCalendarDayRecord? Model { get; set; }

    [Parameter]
    public EditContext? EditContext { get; set; }

    [Parameter]
    public IReadOnlyList<WorkCalendarDayTypeOption> DayTypeOptions { get; set; } = [];

    [Parameter]
    public DateTime MinDate { get; set; }

    [Parameter]
    public DateTime MaxDate { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }
}
