using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;
using Vnta.AttendanceGateway.Protocol.Parsers;

namespace Vnta.AttendanceGateway.Protocol.Handlers;

public class CommandFetchHandler : IRequestHandler
{
    private readonly DeviceCommandPollingService _deviceCommandPollingService;
    private readonly SystemLogQueue _systemLogQueue;
    private readonly AdmsActivityPublisher _admsActivityPublisher;
    private readonly ILogger<CommandFetchHandler> _logger;
    public bool RequiresDeviceAuthorization => true;

    public CommandFetchHandler(
        DeviceCommandPollingService deviceCommandPollingService,
        SystemLogQueue systemLogQueue,
        AdmsActivityPublisher admsActivityPublisher,
        ILogger<CommandFetchHandler> logger)
    {
        _deviceCommandPollingService = deviceCommandPollingService;
        _systemLogQueue = systemLogQueue;
        _admsActivityPublisher = admsActivityPublisher;
        _logger = logger;
    }

    public bool CanHandle(string method, string url)
    {
        return method == "GET" && url.Contains("/iclock/getrequest");
    }

    public async Task<byte[]> HandleAsync(ZktecoRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var normalizedSerial = requestContext.Device?.SerialNumber ?? string.Empty;
        var rawInfo = HeaderParser.ExtractQueryParam(requestContext.Url, "INFO");
        _logger.LogInformation("Attendance Gateway FLOW HANDLER [{FlowId}] Processing command poll. SN={SN}", requestContext.FlowId, normalizedSerial);
        if (!string.IsNullOrWhiteSpace(rawInfo))
        {
            _logger.LogDebug("Received device INFO payload during command poll for {SN}: {Info}", normalizedSerial, rawInfo);
        }

        var command = await _deviceCommandPollingService.GetNextPendingCommandAsync(normalizedSerial, requestContext.FlowId, cancellationToken);
        if (command is null)
        {
            await _systemLogQueue.EnqueueAsync(
                normalizedSerial,
                requestContext.ConnectionId,
                "outbound",
                "command-poll-empty",
                "Gateway returned OK because no pending device command was available.",
                cancellationToken);

            await _admsActivityPublisher.PublishAsync(
                normalizedSerial,
                null,
                requestContext.Method,
                requestContext.Url,
                "command-poll-empty",
                "idle",
                "Thiết bị hỏi lệnh nhưng hiện chưa có lệnh chờ.",
                rawInfo,
                requestContext.FlowId,
                requestContext.ConnectionId,
                cancellationToken);
            return ZktecoResponseBuilder.BuildHttpResponse("OK");
        }

        var payload = command.ToDeviceResponse();
        _logger.LogInformation("Attendance Gateway FLOW HANDLER [{FlowId}] Dispatching command {CommandId} to device [{SN}]: {Payload}", requestContext.FlowId, command.CommandId ?? "(no-id)", normalizedSerial, payload.Trim());
        await _systemLogQueue.EnqueueAsync(
            normalizedSerial,
            requestContext.ConnectionId,
            "outbound",
            "command-dispatch",
            payload,
            cancellationToken);

        await _admsActivityPublisher.PublishAsync(
            normalizedSerial,
            null,
            requestContext.Method,
            requestContext.Url,
            "command-dispatch",
            "dispatched",
            $"Đã phát lệnh {command.CommandId ?? "(no-id)"} tới thiết bị.",
            payload,
            requestContext.FlowId,
            requestContext.ConnectionId,
            cancellationToken);

        return ZktecoResponseBuilder.BuildHttpResponse(payload);
    }
}
