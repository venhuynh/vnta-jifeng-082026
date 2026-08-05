using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Logging;
using Vnta.AttendanceGateway.Protocol.Parsers;
using Vnta.AttendanceGateway.Protocol.Routing;
using Vnta.AttendanceGateway.Security;

namespace Vnta.AttendanceGateway.Network;

public class ClientConnection : IAsyncDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly ILogger _logger;
    private readonly ZktecoRequestRouter _router;
    private readonly AttendanceGatewayRawCommunicationLogger _rawCommunicationLogger;
    private readonly RealtimeGatewayLogPublisher _realtimeGatewayLogPublisher;
    private readonly AdmsActivityPublisher _admsActivityPublisher;
    private readonly string _connectionId;

    public string ConnectionId => _connectionId;

    public ClientConnection(
        TcpClient tcpClient,
        ILogger logger,
        ZktecoRequestRouter router,
        AttendanceGatewayRawCommunicationLogger rawCommunicationLogger,
        RealtimeGatewayLogPublisher realtimeGatewayLogPublisher,
        AdmsActivityPublisher admsActivityPublisher)
    {
        _tcpClient = tcpClient;
        _logger = logger;
        _router = router;
        _rawCommunicationLogger = rawCommunicationLogger;
        _realtimeGatewayLogPublisher = realtimeGatewayLogPublisher;
        _admsActivityPublisher = admsActivityPublisher;
        _connectionId = tcpClient.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("VNTA Attendance Gateway FLOW CONNECT [{ConnectionId}] Thi?t b? b?t d?u k?t n?i.", _connectionId);
        await _admsActivityPublisher.PublishAsync(
            null,
            null,
            "TCP",
            "/tcp/connect",
            "connection-opened",
            "connected",
            $"Thiết bị bắt đầu kết nối tới gateway. ConnectionId={_connectionId}",
            null,
            null,
            _connectionId,
            cancellationToken,
            persistAsSystemLog: false);

        try
        {
            var stream = _tcpClient.GetStream();
            var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
            var flowId = $"{_connectionId}|{DateTimeOffset.Now:yyyyMMddHHmmssfff}";

            var requestHeaderBuilder = new StringBuilder();
            bool isBody = false;
            string firstLine = string.Empty;
            int contentLength = 0;
            string requestMethod = "UNKNOWN";
            string requestUrl = string.Empty;
            string? requestSerialNumber = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(cancellationToken);
                ReadOnlySequence<byte> buffer = result.Buffer;

                SequencePosition consumed = buffer.Start;
                SequencePosition examined = buffer.End;

                if (!isBody)
                {
                    SequencePosition? position;
                    while ((position = buffer.PositionOf((byte)'\n')) != null)
                    {
                        var lineBuffer = buffer.Slice(0, position.Value);
                        var lineString = Encoding.UTF8.GetString(lineBuffer.ToArray()).TrimEnd('\r');

                        requestHeaderBuilder.AppendLine(lineString);

                        var nextLineStart = buffer.GetPosition(1, position.Value);
                        consumed = nextLineStart;
                        buffer = buffer.Slice(nextLineStart);

                        if (string.IsNullOrEmpty(firstLine))
                        {
                            firstLine = lineString;

                            if (!(firstLine.StartsWith("GET ") || firstLine.StartsWith("POST "))
                                || !firstLine.Contains("/iclock", StringComparison.OrdinalIgnoreCase)
                                || !firstLine.Contains("SN=", StringComparison.OrdinalIgnoreCase))
                            {
                                await PublishMonitorEventAsync(
                                    null,
                                    "UNKNOWN",
                                    firstLine,
                                    requestHeaderBuilder.ToString(),
                                    "du_lieu_la",
                                    "firewall-reject",
                                    "Request failed gateway firewall validation",
                                    flowId,
                                    cancellationToken);

                                _logger.LogWarning("=> FIREWALL: Chan ket noi khong hop le tu [{ConnectionId}]. Header: {FirstLine}", _connectionId, firstLine);
                                return;
                            }
                        }
                        else if (lineString.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(lineString.Substring(15).Trim(), out int parsedLength))
                            {
                                contentLength = parsedLength;
                            }
                        }
                        else if (string.IsNullOrEmpty(lineString))
                        {
                            isBody = true;
                            break;
                        }
                    }

                    if (!isBody)
                    {
                        reader.AdvanceTo(consumed, examined);
                        if (result.IsCompleted || result.IsCanceled)
                        {
                            break;
                        }

                        continue;
                    }
                }

                if (isBody)
                {
                    if (buffer.Length >= contentLength)
                    {
                        var bodyBuffer = buffer.Slice(0, contentLength);
                        var bodyData = Encoding.UTF8.GetString(bodyBuffer.ToArray());

                        consumed = bodyBuffer.End;
                        examined = consumed;
                        reader.AdvanceTo(consumed, examined);

                        var fullRawRequest = requestHeaderBuilder + "\n" + bodyData;
                        (requestMethod, requestUrl) = ParseRequestLine(firstLine);
                        requestSerialNumber = ExtractSerialNumber(requestUrl);

                        await _rawCommunicationLogger.LogReceiveAsync(flowId, _connectionId, fullRawRequest, cancellationToken);
                        await PublishMonitorEventAsync(
                            requestSerialNumber,
                            requestMethod,
                            requestUrl,
                            fullRawRequest,
                            "received",
                            "transport-receive",
                            null,
                            flowId,
                            cancellationToken,
                            direction: "receive");

                        _logger.LogDebug(
                            "VNTA Attendance Gateway FLOW RX [{FlowId}]\nConnection: {ConnectionId}\nDirection: RECEIVE\nPayload:\n{Data}\n----------------------------------",
                            flowId,
                            _connectionId,
                            fullRawRequest);

                        var responseBytes = await _router.RouteAsync(firstLine, bodyData, _connectionId, flowId, cancellationToken);
                        var responseText = Encoding.UTF8.GetString(responseBytes);
                        await _rawCommunicationLogger.LogSendAsync(flowId, _connectionId, responseText, cancellationToken);
                        await PublishMonitorEventAsync(
                            requestSerialNumber,
                            requestMethod,
                            requestUrl,
                            responseText,
                            ExtractResponseStatus(responseText),
                            "transport-send",
                            null,
                            flowId,
                            cancellationToken,
                            direction: "send");

                        _logger.LogDebug(
                            "VNTA Attendance Gateway FLOW TX [{FlowId}]\nConnection: {ConnectionId}\nDirection: RESPONSE\nPayload:\n{Response}\n----------------------------------",
                            flowId,
                            _connectionId,
                            responseText);

                        await Task.Delay(150, cancellationToken);
                        await stream.WriteAsync(responseBytes, cancellationToken);
                        break;
                    }

                    reader.AdvanceTo(consumed, examined);
                    if (result.IsCompleted || result.IsCanceled)
                    {
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network Error on Client: [{ConnectionId}]", _connectionId);
        }
        finally
        {
            _logger.LogInformation("VNTA Attendance Gateway FLOW CLOSE [{ConnectionId}] ��ng k?t n?i thi?t b?.", _connectionId);
            await _admsActivityPublisher.PublishAsync(
                null,
                null,
                "TCP",
                "/tcp/disconnect",
                "connection-closed",
                "disconnected",
                $"Thiết bị đã đóng kết nối với gateway. ConnectionId={_connectionId}",
                null,
                null,
                _connectionId,
                CancellationToken.None,
                persistAsSystemLog: false);
            _tcpClient.Close();
        }
    }

    public ValueTask DisposeAsync()
    {
        _tcpClient.Close();
        return ValueTask.CompletedTask;
    }

    private async Task PublishMonitorEventAsync(
        string? serialNumber,
        string requestMethod,
        string requestUrl,
        string payload,
        string logStatus,
        string eventType,
        string? rejectionReason,
        string flowId,
        CancellationToken cancellationToken,
        string direction = "event")
    {
        await _realtimeGatewayLogPublisher.PublishAsync(
            serialNumber,
            null,
            requestMethod,
            requestUrl,
            payload,
            logStatus,
            rejectionReason,
            cancellationToken,
            direction: direction,
            eventType: eventType,
            flowId: flowId,
            connectionId: _connectionId);
    }

    private static (string Method, string Url) ParseRequestLine(string requestLine)
    {
        var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return ("UNKNOWN", requestLine);
        }

        return (parts[0].ToUpperInvariant(), parts[1]);
    }

    private static string? ExtractSerialNumber(string requestUrl)
    {
        var serialNumber = HeaderParser.ExtractQueryParam(requestUrl, "SN");
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return null;
        }

        return VntaCrypto.NormalizeSerial(serialNumber);
    }

    private static string ExtractResponseStatus(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return "sent";
        }

        var lines = responseText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var firstLine = lines.Length > 0 ? lines[0].Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return "sent";
        }

        var parts = firstLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (firstLine.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length >= 3)
            {
                return $"{parts[1]} {parts[2]}";
            }

            if (parts.Length >= 2)
            {
                return parts[1];
            }
        }

        return "sent";
    }
}
