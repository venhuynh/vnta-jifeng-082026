using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Protocol.Handlers;

public sealed class ErrorLogHandler : IRequestHandler
{
    private readonly SystemLogQueue _systemLogQueue;
    private readonly ErrorLogSyncService _errorLogSyncService;
    private readonly AdmsActivityPublisher _admsActivityPublisher;
    private readonly ILogger<ErrorLogHandler> _logger;

    public bool RequiresDeviceAuthorization => true;

    public ErrorLogHandler(
        SystemLogQueue systemLogQueue,
        ErrorLogSyncService errorLogSyncService,
        AdmsActivityPublisher admsActivityPublisher,
        ILogger<ErrorLogHandler> logger)
    {
        _systemLogQueue = systemLogQueue;
        _errorLogSyncService = errorLogSyncService;
        _admsActivityPublisher = admsActivityPublisher;
        _logger = logger;
    }

    public bool CanHandle(string method, string url)
    {
        return method == "POST"
               && url.Contains("/iclock/cdata", StringComparison.OrdinalIgnoreCase)
               && url.Contains("table=ERRORLOG", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<byte[]> HandleAsync(ZktecoRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var sn = requestContext.Device?.SerialNumber ?? string.Empty;
        _logger.LogInformation("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] Processing ERRORLOG payload. SN={SN}", requestContext.FlowId, sn);

        var result = await _errorLogSyncService.ProcessAsync(
            sn,
            requestContext.Url,
            requestContext.BodyRawText,
            requestContext.FlowId,
            cancellationToken);

        await _systemLogQueue.EnqueueAsync(
            sn,
            requestContext.ConnectionId,
            "inbound",
            "error-log",
            requestContext.BodyRawText,
            cancellationToken);

        _logger.LogInformation(
            "VNTA Attendance Gateway FLOW HANDLER [{FlowId}] ERRORLOG response prepared. SN={SN}, ReceivedLines={ReceivedLines}, SavedLines={SavedLines}, Stamp={Stamp}",
            requestContext.FlowId,
            sn,
            result.ReceivedLineCount,
            result.SavedLineCount,
            result.Stamp ?? "<empty>");

        await _admsActivityPublisher.PublishAsync(
            sn,
            null,
            requestContext.Method,
            requestContext.Url,
            "error-log-processed",
            result.DeviceResolved ? "processed" : "ignored",
            result.DeviceResolved
                ? $"Đã xử lý ERRORLOG. Nhận {result.ReceivedLineCount} dòng, lưu {result.SavedLineCount} dòng."
                : "Bỏ qua ERRORLOG vì không tìm thấy thiết bị tương ứng.",
            requestContext.BodyRawText,
            requestContext.FlowId,
            requestContext.ConnectionId,
            cancellationToken);

        return ZktecoResponseBuilder.BuildHttpResponse("OK");
    }
}
