using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Protocol.Handlers;

public class AttendanceLogHandler : IRequestHandler
{
    private readonly AttendanceLogSyncService _attendanceLogSyncService;
    private readonly AdmsActivityPublisher _admsActivityPublisher;
    private readonly ILogger<AttendanceLogHandler> _logger;
    public bool RequiresDeviceAuthorization => true;

    public AttendanceLogHandler(
        AttendanceLogSyncService attendanceLogSyncService,
        AdmsActivityPublisher admsActivityPublisher,
        ILogger<AttendanceLogHandler> logger)
    {
        _attendanceLogSyncService = attendanceLogSyncService;
        _admsActivityPublisher = admsActivityPublisher;
        _logger = logger;
    }

    public bool CanHandle(string method, string url)
    {
        return method == "POST" && url.Contains("/iclock/cdata") && url.Contains("table=ATTLOG");
    }

    public async Task<byte[]> HandleAsync(ZktecoRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var sn = requestContext.Device?.SerialNumber ?? string.Empty;
        _logger.LogInformation("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] Processing ATTLOG payload. SN={SN}", requestContext.FlowId, sn);

        var result = await _attendanceLogSyncService.ProcessAsync(
            sn,
            requestContext.Url,
            requestContext.BodyRawText,
            requestContext.FlowId,
            cancellationToken);

        _logger.LogInformation("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] ATTLOG response prepared. SN={SN}, ReceivedLines={ReceivedLines}, SavedLines={SavedLines}",
            requestContext.FlowId, sn, result.ReceivedLineCount, result.SavedLineCount);

        // Theo giao thức VNTA Attendance Gateway của nhánh ATTLOG, phản hồi dùng số dòng body nhận được từ thiết bị.
        await _admsActivityPublisher.PublishAsync(
            sn,
            null,
            requestContext.Method,
            requestContext.Url,
            "attendance-log-processed",
            "processed",
            $"Đã xử lý ATTLOG. Nhận {result.ReceivedLineCount} dòng, lưu {result.SavedLineCount} dòng.",
            requestContext.BodyRawText,
            requestContext.FlowId,
            requestContext.ConnectionId,
            cancellationToken);

        return ZktecoResponseBuilder.BuildHttpResponse($"OK:{result.ReceivedLineCount}");
    }
}
