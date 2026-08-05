using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

public sealed class AttendanceGatewayInboundService(
    AdmsMonitorMemoryStore monitorStore,
    IAdmsMonitorRuntimeState runtimeState,
    IAdmsMonitorEventPublisher eventPublisher,
    ILogger<AttendanceGatewayInboundService> logger)
    : IAttendanceGatewayInboundService
{
    private const string RealtimeRawBodyRedacted = "Raw payload redacted from HRM monitor; consult restricted gateway logs when approved.";

    public async Task<AttendanceGatewayIngestionResult> IngestAttendanceAsync(
        string deviceSn,
        IReadOnlyCollection<AttendanceGatewayAttendanceLogDto> logs,
        CancellationToken cancellationToken)
    {
        if(logs.Count == 0) {
            return new AttendanceGatewayIngestionResult(0, 0, 0);
        }

        logger.LogInformation(
            "HRM monitor ignored attendance payload because ADMS integration is view-only. DeviceSn={DeviceSn}, Received={ReceivedCount}",
            NormalizeRequired(deviceSn),
            logs.Count);

        return await Task.FromResult(new AttendanceGatewayIngestionResult(logs.Count, 0, 0));
    }

    public async Task<AttendanceGatewayIngestionResult> IngestSystemLogAsync(
        AttendanceGatewaySystemLogDto log,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "HRM monitor ignored system log payload because ADMS integration is view-only. DeviceSn={DeviceSn}, EventType={EventType}",
            NormalizeRequired(log.DeviceSn),
            NormalizeRequired(log.EventType));

        return await Task.FromResult(new AttendanceGatewayIngestionResult(1, 0, 0));
    }

    public async Task<AttendanceGatewayIngestionResult> IngestRealtimeEventAsync(
        AttendanceGatewayRealtimeEventDto eventDto,
        CancellationToken cancellationToken)
    {
        if(!runtimeState.HasActiveSessions) {
            logger.LogDebug(
                "Skipped realtime ADMS event because no active /Adms viewer is connected. EventType={EventType}, DeviceSn={DeviceSn}",
                eventDto.EventType,
                eventDto.Sn);

            return new AttendanceGatewayIngestionResult(1, 0, 0);
        }

        var sanitizedEventDto = SanitizeRealtimeEventDto(eventDto);
        var storeResult = monitorStore.TryStoreRealtimeEvent(sanitizedEventDto);
        var bufferedCount = storeResult.IsBufferedInPanel ? 1 : 0;
        var duplicateCount = storeResult.IsDuplicate ? 1 : 0;
        var filteredCount = !storeResult.IsDuplicate && !storeResult.IsBufferedInPanel ? 1 : 0;
        var deviceState = monitorStore.UpsertDeviceStateFromRealtimeEvent(sanitizedEventDto);

        if(storeResult.IsBufferedInPanel) {
            await eventPublisher.PublishRealtimeEventAsync(sanitizedEventDto, cancellationToken);
        }

        if(deviceState is not null) {
            await eventPublisher.PublishDeviceConnectionStateAsync(deviceState, cancellationToken);
        }

        logger.LogInformation(
            "Buffered realtime ADMS event in memory only for monitor view. EventType={EventType}, DeviceSn={DeviceSn}, Buffered={BufferedCount}, Duplicates={DuplicateCount}, Filtered={FilteredCount}",
            sanitizedEventDto.EventType,
            sanitizedEventDto.Sn,
            bufferedCount,
            duplicateCount,
            filteredCount);

        return new AttendanceGatewayIngestionResult(1, bufferedCount, duplicateCount);
    }

    private static string NormalizeRequired(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();

    private static AttendanceGatewayRealtimeEventDto SanitizeRealtimeEventDto(
        AttendanceGatewayRealtimeEventDto eventDto) =>
        eventDto with {
            RawBody = string.IsNullOrWhiteSpace(eventDto.RawBody)
                ? string.Empty
                : RealtimeRawBodyRedacted
        };
}
