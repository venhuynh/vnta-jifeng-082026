using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Protocol.Handlers;

public class OperationalLogHandler : IRequestHandler
{
    private readonly SystemLogQueue _systemLogQueue;
    private readonly OperationalLogSyncService _operationalLogSyncService;
    private readonly AdmsActivityPublisher _admsActivityPublisher;
    private readonly ILogger<OperationalLogHandler> _logger;
    public bool RequiresDeviceAuthorization => true;

    public OperationalLogHandler(
        SystemLogQueue systemLogQueue,
        OperationalLogSyncService operationalLogSyncService,
        AdmsActivityPublisher admsActivityPublisher,
        ILogger<OperationalLogHandler> logger)
    {
        _systemLogQueue = systemLogQueue;
        _operationalLogSyncService = operationalLogSyncService;
        _admsActivityPublisher = admsActivityPublisher;
        _logger = logger;
    }

    public bool CanHandle(string method, string url)
    {
        return method == "POST" && url.Contains("/iclock/cdata") && url.Contains("table=OPERLOG");
    }

    public async Task<byte[]> HandleAsync(ZktecoRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var sn = requestContext.Device?.SerialNumber ?? string.Empty;
        _logger.LogInformation("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] Processing OPERLOG payload. SN={SN}", requestContext.FlowId, sn);

        var result = await _operationalLogSyncService.ProcessAsync(
            sn,
            requestContext.Url,
            requestContext.BodyRawText,
            requestContext.FlowId,
            cancellationToken);

        await _systemLogQueue.EnqueueAsync(
            sn,
            requestContext.ConnectionId,
            "inbound",
            "operational-log",
            requestContext.BodyRawText,
            cancellationToken);

        _logger.LogInformation(
            "VNTA Attendance Gateway FLOW HANDLER [{FlowId}] OPERLOG response prepared. SN={SN}, ReceivedLines={ReceivedLines}, SavedLines={SavedLines}, Stamp={Stamp}",
            requestContext.FlowId,
            sn,
            result.ReceivedLineCount,
            result.SavedLineCount,
            result.Stamp ?? "<empty>");

        foreach (var activity in result.SemanticActivities)
        {
            await _admsActivityPublisher.PublishAsync(
                sn,
                null,
                requestContext.Method,
                requestContext.Url,
                activity.EventType,
                activity.LogStatus,
                activity.SummaryText,
                activity.RawBody,
                requestContext.FlowId,
                requestContext.ConnectionId,
                cancellationToken);
        }

        await _admsActivityPublisher.PublishAsync(
            sn,
            null,
            requestContext.Method,
            requestContext.Url,
            "operational-log-processed",
            result.DeviceResolved ? "processed" : "ignored",
            result.DeviceResolved
                ? $"Đã xử lý OPERLOG. Nhận {result.ReceivedLineCount} dòng, lưu {result.SavedLineCount} dòng."
                : "Bỏ qua OPERLOG vì không tìm thấy thiết bị tương ứng.",
            requestContext.BodyRawText,
            requestContext.FlowId,
            requestContext.ConnectionId,
            cancellationToken);

        return ZktecoResponseBuilder.BuildHttpResponse($"OK:{result.ReceivedLineCount}");
    }
}
