namespace Vnta.Hrm.Application.Integrations.AttendanceGateway;

public interface IAttendanceGatewayInboundService
{
    Task<AttendanceGatewayIngestionResult> IngestAttendanceAsync(
        string deviceSn,
        IReadOnlyCollection<AttendanceGatewayAttendanceLogDto> logs,
        CancellationToken cancellationToken);

    Task<AttendanceGatewayIngestionResult> IngestSystemLogAsync(
        AttendanceGatewaySystemLogDto log,
        CancellationToken cancellationToken);

    Task<AttendanceGatewayIngestionResult> IngestRealtimeEventAsync(
        AttendanceGatewayRealtimeEventDto eventDto,
        CancellationToken cancellationToken);
}
