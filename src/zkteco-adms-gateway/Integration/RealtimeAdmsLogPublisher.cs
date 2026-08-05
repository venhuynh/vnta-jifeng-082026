using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Vnta.AttendanceGateway.Configuration;
using Vnta.AttendanceGateway.Hubs;
using Vnta.AttendanceGateway.Integration.Models;

namespace Vnta.AttendanceGateway.Integration;

public sealed class RealtimeGatewayLogPublisher
{
    private const int HardRealtimeRawBodyMaxLength = AttendanceGatewayOptions.DefaultRealtimeRawBodyMaxLength;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly AdmsRealtimeEventQueue _realtimeEventQueue;
    private readonly AttendanceGatewayOptions _gatewayOptions;

    public RealtimeGatewayLogPublisher(
        IHubContext<DeviceHub> hubContext,
        AdmsRealtimeEventQueue realtimeEventQueue,
        IOptions<AttendanceGatewayOptions> gatewayOptions)
    {
        _hubContext = hubContext;
        _realtimeEventQueue = realtimeEventQueue;
        _gatewayOptions = gatewayOptions.Value;
    }

    public async Task PublishAsync(
        string? serialNumber,
        string? deviceName,
        string requestMethod,
        string requestUrl,
        string? rawBody,
        string logStatus,
        string? rejectionReason,
        CancellationToken cancellationToken = default,
        string direction = "event",
        string eventType = "gateway-event",
        string? flowId = null,
        string? connectionId = null,
        string? summaryText = null,
        bool isSemantic = false,
        string signalREventName = "GatewayRawLogEvent")
    {
        var rawBodyPreview = BuildRealtimeRawBodyPreview(rawBody, _gatewayOptions.RealtimeRawBodyMaxLength);

        var payload = new CoreApiAdmsRealtimeEventRequest(
            Guid.NewGuid().ToString("N"),
            flowId,
            connectionId,
            string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber,
            deviceName,
            requestMethod,
            requestUrl,
            direction,
            eventType,
            rawBodyPreview,
            logStatus,
            rejectionReason,
            DateTimeOffset.UtcNow,
            summaryText,
            isSemantic);

        var signalRTask = _hubContext.Clients.All.SendAsync(
            signalREventName,
            payload,
            cancellationToken);

        _realtimeEventQueue.TryEnqueue(payload);

        await signalRTask;
    }

    private static string BuildRealtimeRawBodyPreview(string? rawBody, int maxLength)
    {
        var normalizedBody = rawBody?.Trim() ?? string.Empty;
        var effectiveMaxLength = ResolveEffectiveMaxLength(maxLength);

        if (string.IsNullOrEmpty(normalizedBody) || normalizedBody.Length <= effectiveMaxLength)
        {
            return normalizedBody;
        }

        var suffix = $"\n\n...[RAW BODY TRUNCATED FOR ADMS REALTIME VIEW. OriginalLength={normalizedBody.Length} chars, MaxLength={effectiveMaxLength} chars]";
        if (suffix.Length >= effectiveMaxLength)
        {
            return suffix[..effectiveMaxLength];
        }

        var previewLength = effectiveMaxLength - suffix.Length;
        return normalizedBody[..previewLength] + suffix;
    }

    private static int ResolveEffectiveMaxLength(int configuredMaxLength)
    {
        if (configuredMaxLength <= 0)
        {
            return HardRealtimeRawBodyMaxLength;
        }

        return Math.Min(configuredMaxLength, HardRealtimeRawBodyMaxLength);
    }
}
