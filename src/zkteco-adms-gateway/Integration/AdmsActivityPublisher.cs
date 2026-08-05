namespace Vnta.AttendanceGateway.Integration;

public sealed class AdmsActivityPublisher
{
    private readonly RealtimeGatewayLogPublisher _realtimeGatewayLogPublisher;
    private readonly SystemLogQueue _systemLogQueue;
    private readonly ILogger<AdmsActivityPublisher> _logger;

    public AdmsActivityPublisher(
        RealtimeGatewayLogPublisher realtimeGatewayLogPublisher,
        SystemLogQueue systemLogQueue,
        ILogger<AdmsActivityPublisher> logger)
    {
        _realtimeGatewayLogPublisher = realtimeGatewayLogPublisher;
        _systemLogQueue = systemLogQueue;
        _logger = logger;
    }

    public async Task PublishAsync(
        string? deviceSn,
        string? deviceName,
        string requestMethod,
        string requestUrl,
        string eventType,
        string logStatus,
        string summaryText,
        string? rawBody,
        string? flowId,
        string? connectionId,
        CancellationToken cancellationToken = default,
        string direction = "event",
        string? rejectionReason = null,
        bool persistAsSystemLog = true)
    {
        try
        {
            await _realtimeGatewayLogPublisher.PublishAsync(
                deviceSn,
                deviceName,
                requestMethod,
                requestUrl,
                rawBody,
                logStatus,
                rejectionReason,
                cancellationToken,
                direction: direction,
                eventType: eventType,
                flowId: flowId,
                connectionId: connectionId,
                summaryText: summaryText,
                isSemantic: true,
                signalREventName: "GatewayActivityEvent");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Could not publish semantic ADMS activity event. EventType={EventType}, DeviceSn={DeviceSn}, FlowId={FlowId}",
                eventType,
                deviceSn ?? "<none>",
                flowId ?? "<none>");
        }

        if (!persistAsSystemLog || string.IsNullOrWhiteSpace(deviceSn))
        {
            return;
        }

        var systemLogMessage = BuildSystemLogMessage(summaryText, rawBody);
        await _systemLogQueue.EnqueueAsync(
            deviceSn.Trim().ToUpperInvariant(),
            connectionId ?? string.Empty,
            direction,
            eventType,
            systemLogMessage,
            cancellationToken);
    }

    private static string BuildSystemLogMessage(string summaryText, string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return summaryText;
        }

        return $"{summaryText}\n\nRAW:\n{rawBody}";
    }
}
