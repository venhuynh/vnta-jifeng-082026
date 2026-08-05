using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Protocol.Handlers;

public sealed class DeviceCommandResultHandler : IRequestHandler
{
    private readonly DeviceCommandCallbackService _deviceCommandCallbackService;
    private readonly AdmsActivityPublisher _admsActivityPublisher;
    private readonly ILogger<DeviceCommandResultHandler> _logger;

    public bool RequiresDeviceAuthorization => true;

    public DeviceCommandResultHandler(
        DeviceCommandCallbackService deviceCommandCallbackService,
        AdmsActivityPublisher admsActivityPublisher,
        ILogger<DeviceCommandResultHandler> logger)
    {
        _deviceCommandCallbackService = deviceCommandCallbackService;
        _admsActivityPublisher = admsActivityPublisher;
        _logger = logger;
    }

    public bool CanHandle(string method, string url)
    {
        return method == "POST"
               && url.Contains("/iclock", StringComparison.OrdinalIgnoreCase)
               && url.Contains("devicecmd", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<byte[]> HandleAsync(ZktecoRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var normalizedSerial = requestContext.Device?.SerialNumber ?? string.Empty;
        _logger.LogInformation("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] Processing devicecmd callback. SN={SN}", requestContext.FlowId, normalizedSerial);

        var updatedCount = await _deviceCommandCallbackService.ProcessAsync(
            normalizedSerial,
            requestContext.BodyRawText,
            requestContext.FlowId,
            cancellationToken);

        _logger.LogInformation("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] Devicecmd callback finished. SN={SN}, UpdatedRows={UpdatedCount}",
            requestContext.FlowId, normalizedSerial, updatedCount);

        await _admsActivityPublisher.PublishAsync(
            normalizedSerial,
            null,
            requestContext.Method,
            requestContext.Url,
            "device-command-callback-processed",
            updatedCount > 0 ? "processed" : "ignored",
            updatedCount > 0
                ? $"Đã xử lý callback devicecmd. Cập nhật {updatedCount} dòng kết quả lệnh."
                : "Không cập nhật được dòng kết quả nào từ callback devicecmd.",
            requestContext.BodyRawText,
            requestContext.FlowId,
            requestContext.ConnectionId,
            cancellationToken);

        return ZktecoResponseBuilder.BuildHttpResponse("OK");
    }
}
