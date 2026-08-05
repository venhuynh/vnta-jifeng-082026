using Microsoft.AspNetCore.SignalR;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Hubs;

public sealed class AdmsMonitorHub(
    IAdmsMonitorReadService monitorReadService,
    IAdmsMonitorRuntimeState runtimeState) : Hub
{
    public const string DeviceAdministrationGroup = "device-administration";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, DeviceAdministrationGroup);
        runtimeState.ActivateSession();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DeviceAdministrationGroup);
        runtimeState.DeactivateSession();
        await base.OnDisconnectedAsync(exception);
    }

    public Task<AdmsMonitorSnapshotDto> GetMonitorSnapshotAsync(
        int activityLimit,
        int rawLimit) =>
        monitorReadService.GetSnapshotAsync(activityLimit, rawLimit);
}
