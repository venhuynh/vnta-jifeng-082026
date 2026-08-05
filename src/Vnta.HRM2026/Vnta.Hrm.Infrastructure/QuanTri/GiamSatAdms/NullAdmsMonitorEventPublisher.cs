using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.QuanTri.GiamSatAdms;

public sealed class NullAdmsMonitorEventPublisher : IAdmsMonitorEventPublisher
{
    public Task PublishDeviceConnectionStateAsync(
        AdmsMonitorDeviceStateDto deviceState,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PublishRealtimeEventAsync(
        AttendanceGatewayRealtimeEventDto eventDto,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
