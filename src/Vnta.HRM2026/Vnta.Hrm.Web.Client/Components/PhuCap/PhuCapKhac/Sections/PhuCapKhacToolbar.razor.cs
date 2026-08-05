using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.State;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Sections;

public partial class PhuCapKhacToolbar
{
    [Parameter, EditorRequired] public OtherAllowanceToolbarState State { get; set; } = default!;
    [Parameter] public EventCallback<int> MonthChanged { get; set; }
    [Parameter] public EventCallback<int> YearChanged { get; set; }
    [Parameter] public EventCallback ViewRequested { get; set; }
    [Parameter] public EventCallback CreateRequested { get; set; }
    [Parameter] public EventCallback SyncFromPreviousMonthRequested { get; set; }
    [Parameter] public EventCallback RulesRequested { get; set; }
    [Parameter] public EventCallback LockRequested { get; set; }
    [Parameter] public EventCallback UnlockRequested { get; set; }
    [Parameter] public bool CanOperate { get; set; }
    [Parameter] public bool CanSyncFromPreviousMonth { get; set; }
    [Parameter] public bool CanLock { get; set; }
    [Parameter] public bool CanUnlock { get; set; }
}
