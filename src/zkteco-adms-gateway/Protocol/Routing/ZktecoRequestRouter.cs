using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vnta.AttendanceGateway.Protocol.Handlers;
using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;
using Vnta.AttendanceGateway.Security;
using Microsoft.Extensions.Logging;

namespace Vnta.AttendanceGateway.Protocol.Routing;

public class ZktecoRequestRouter
{
    private readonly IEnumerable<IRequestHandler> _handlers;
    private readonly ILogger<ZktecoRequestRouter> _logger;
    private readonly DeviceAuthorizationService _deviceAuthorizationService;
    private readonly RealtimeGatewayLogPublisher _realtimeAdmsLogPublisher;

    public ZktecoRequestRouter(
        IEnumerable<IRequestHandler> handlers,
        ILogger<ZktecoRequestRouter> logger,
        DeviceAuthorizationService deviceAuthorizationService,
        RealtimeGatewayLogPublisher realtimeAdmsLogPublisher)
    {
        _handlers = handlers;
        _logger = logger;
        _deviceAuthorizationService = deviceAuthorizationService;
        _realtimeAdmsLogPublisher = realtimeAdmsLogPublisher;
    }

    /// <summary>
    /// Routes the incoming HTTP request to the appropriate registered Handler.
    /// </summary>
    public async Task<byte[]> RouteAsync(string requestLine, string bodyData, string connectionId, string flowId, CancellationToken cancellationToken)
    {
        // Example requestLine: "POST /iclock/cdata?SN=AA123&table=ATTLOG HTTP/1.1"
        var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length < 2)
        {
            _logger.LogWarning("VNTA Attendance Gateway FLOW ROUTE [{FlowId}] Invalid HTTP request line. Connection={ConnectionId}, Line={Line}", flowId, connectionId, requestLine);
            await _realtimeAdmsLogPublisher.PublishAsync(
                null,
                null,
                "UNKNOWN",
                requestLine,
                requestLine,
                "du_lieu_la",
                "Invalid HTTP request line",
                cancellationToken,
                direction: "event",
                eventType: "routing-invalid-request",
                flowId: flowId,
                connectionId: connectionId);
            return ZktecoResponseBuilder.BuildHttpResponse("Bad Request", "400 Bad Request");
        }

        var method = parts[0].ToUpperInvariant(); // GET, POST
        var url = parts[1]; // /iclock/cdata?...

        _logger.LogInformation("VNTA Attendance Gateway FLOW ROUTE [{FlowId}] Connection={ConnectionId}, Method={Method}, Url={Url}", flowId, connectionId, method, url);

        var handler = _handlers.FirstOrDefault(h => h.CanHandle(method, url));

        if (handler != null)
        {
            DeviceAuthorizationContext? deviceContext = null;
            if (handler.RequiresDeviceAuthorization)
            {
                var authorizationResult = await _deviceAuthorizationService.AuthorizeAsync(method, url, connectionId, flowId, cancellationToken);
                if (!authorizationResult.IsAuthorized)
                {
                    return authorizationResult.FailureResponse ?? ZktecoResponseBuilder.BuildHttpResponse("OK: 0");
                }

                deviceContext = authorizationResult.Device;
            }

            return await handler.HandleAsync(new ZktecoRequestContext
            {
                Method = method,
                Url = url,
                BodyRawText = bodyData,
                ConnectionId = connectionId,
                FlowId = flowId,
                Device = deviceContext
            }, cancellationToken);
        }

        // Fallback: invalid or unsupported VNTA Attendance Gateway requests are rejected at router level.
        _logger.LogWarning("VNTA Attendance Gateway FLOW ROUTE [{FlowId}] No suitable handler found. Connection={ConnectionId}, Method={Method}, Url={Url}", flowId, connectionId, method, url);
        await _realtimeAdmsLogPublisher.PublishAsync(
            null,
            null,
            method,
            url,
            requestLine,
            "du_lieu_la",
            "No suitable VNTA Attendance Gateway handler found",
            cancellationToken,
            direction: "event",
            eventType: "routing-no-handler",
            flowId: flowId,
            connectionId: connectionId);
        return ZktecoResponseBuilder.BuildHttpResponse("Bad Request", "400 Bad Request");
    }
}

