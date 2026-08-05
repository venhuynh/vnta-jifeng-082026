using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Protocol.Handlers;

public sealed class DeviceOptionsHandler : IRequestHandler
{
    private readonly DeviceOptionsSyncService _deviceOptionsSyncService;
    private readonly AdmsActivityPublisher _admsActivityPublisher;
    private readonly ILogger<DeviceOptionsHandler> _logger;

    public bool RequiresDeviceAuthorization => true;

    public DeviceOptionsHandler(
        DeviceOptionsSyncService deviceOptionsSyncService,
        AdmsActivityPublisher admsActivityPublisher,
        ILogger<DeviceOptionsHandler> logger)
    {
        _deviceOptionsSyncService = deviceOptionsSyncService;
        _admsActivityPublisher = admsActivityPublisher;
        _logger = logger;
    }

    public bool CanHandle(string method, string url)
    {
        return method == "POST"
               && url.Contains("/iclock/cdata", StringComparison.OrdinalIgnoreCase)
               && url.Contains("table=options", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<byte[]> HandleAsync(ZktecoRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var normalizedSerial = requestContext.Device?.SerialNumber ?? string.Empty;
        _logger.LogInformation("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] Processing table=options payload. SN={SN}", requestContext.FlowId, normalizedSerial);

        var processed = await _deviceOptionsSyncService.ProcessAsync(
            normalizedSerial,
            requestContext.BodyRawText,
            requestContext.FlowId,
            cancellationToken);

        await _admsActivityPublisher.PublishAsync(
            normalizedSerial,
            null,
            requestContext.Method,
            requestContext.Url,
            "device-options-processed",
            processed ? "processed" : "ignored",
            processed
                ? "Đã xử lý thành công payload table=options của thiết bị."
                : "Bỏ qua payload table=options vì dữ liệu rỗng hoặc không tìm thấy thiết bị.",
            requestContext.BodyRawText,
            requestContext.FlowId,
            requestContext.ConnectionId,
            cancellationToken);

        return ZktecoResponseBuilder.BuildHttpResponse("OK");
    }
}
