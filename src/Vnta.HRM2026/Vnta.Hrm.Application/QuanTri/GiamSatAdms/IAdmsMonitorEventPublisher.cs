namespace Vnta.Hrm.Application.QuanTri.GiamSatAdms;

public interface IAdmsMonitorEventPublisher
{
    Task PublishDeviceConnectionStateAsync(
        AdmsMonitorDeviceStateDto deviceState,
        CancellationToken cancellationToken);

    Task PublishRealtimeEventAsync(
        AttendanceGatewayRealtimeEventDto eventDto,
        CancellationToken cancellationToken);
}
