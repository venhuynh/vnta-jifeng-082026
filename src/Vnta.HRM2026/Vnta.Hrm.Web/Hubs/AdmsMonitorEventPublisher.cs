using Microsoft.AspNetCore.SignalR;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Hubs;

public sealed class AdmsMonitorEventPublisher(IHubContext<AdmsMonitorHub> hubContext) : IAdmsMonitorEventPublisher
{
    public Task PublishDeviceConnectionStateAsync(
        AdmsMonitorDeviceStateDto deviceState,
        CancellationToken cancellationToken) =>
        hubContext.Clients.Group(AdmsMonitorHub.DeviceAdministrationGroup).SendAsync(
            AdmsMonitorSignalREvents.DeviceConnectionStateEvent,
            deviceState,
            cancellationToken);

    public Task PublishRealtimeEventAsync(
        AttendanceGatewayRealtimeEventDto eventDto,
        CancellationToken cancellationToken)
    {
        var eventName = eventDto.IsSemantic
            ? AdmsMonitorSignalREvents.GatewayActivityEvent
            : AdmsMonitorSignalREvents.GatewayRawLogEvent;

        return hubContext.Clients.Group(AdmsMonitorHub.DeviceAdministrationGroup)
            .SendAsync(eventName, eventDto, cancellationToken);
    }
}
