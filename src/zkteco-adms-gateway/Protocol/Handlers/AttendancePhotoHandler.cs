using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;

namespace Vnta.AttendanceGateway.Protocol.Handlers;

public sealed class AttendancePhotoHandler : IRequestHandler
{
    private readonly AttendancePhotoStampSyncService _attendancePhotoStampSyncService;
    private readonly AdmsActivityPublisher _admsActivityPublisher;
    private readonly ILogger<AttendancePhotoHandler> _logger;

    public bool RequiresDeviceAuthorization => true;

    public AttendancePhotoHandler(
        AttendancePhotoStampSyncService attendancePhotoStampSyncService,
        AdmsActivityPublisher admsActivityPublisher,
        ILogger<AttendancePhotoHandler> logger)
    {
        _attendancePhotoStampSyncService = attendancePhotoStampSyncService;
        _admsActivityPublisher = admsActivityPublisher;
        _logger = logger;
    }

    public bool CanHandle(string method, string url)
    {
        return method == "POST"
               && url.Contains("/iclock/cdata", StringComparison.OrdinalIgnoreCase)
               && url.Contains("table=ATTPHOTO", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<byte[]> HandleAsync(ZktecoRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var sn = requestContext.Device?.SerialNumber ?? string.Empty;
        _logger.LogInformation("VNTA Attendance Gateway FLOW HANDLER [{FlowId}] Processing ATTPHOTO payload. SN={SN}", requestContext.FlowId, sn);

        var result = await _attendancePhotoStampSyncService.ProcessAsync(
            sn,
            requestContext.Url,
            requestContext.BodyRawText,
            requestContext.FlowId,
            cancellationToken);

        _logger.LogInformation(
            "VNTA Attendance Gateway FLOW HANDLER [{FlowId}] ATTPHOTO response prepared. SN={SN}, ReceivedLines={ReceivedLines}, Stamp={Stamp}",
            requestContext.FlowId,
            sn,
            result.ReceivedLineCount,
            result.Stamp ?? "<empty>");

        await _admsActivityPublisher.PublishAsync(
            sn,
            null,
            requestContext.Method,
            requestContext.Url,
            "attendance-photo-processed",
            result.DeviceResolved ? "processed" : "ignored",
            result.DeviceResolved
                ? $"Đã xử lý ATTPHOTO. Nhận {result.ReceivedLineCount} dòng, Stamp={result.Stamp ?? "<empty>"}."
                : "Bỏ qua ATTPHOTO vì không tìm thấy thiết bị tương ứng.",
            requestContext.BodyRawText,
            requestContext.FlowId,
            requestContext.ConnectionId,
            cancellationToken);

        return ZktecoResponseBuilder.BuildHttpResponse($"OK:{result.ReceivedLineCount}");
    }
}
