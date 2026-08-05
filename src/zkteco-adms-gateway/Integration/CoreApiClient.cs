using System.Net.Http.Json;
using Vnta.AttendanceGateway.Configuration;
using Vnta.AttendanceGateway.Integration.Models;
using Vnta.AttendanceGateway.Logging;
using Vnta.AttendanceGateway.Protocol.Models;
using Microsoft.Extensions.Options;

namespace Vnta.AttendanceGateway.Integration;

public class CoreApiClient(
    HttpClient httpClient,
    IOptions<CoreApiOptions> options,
    ILogger<CoreApiClient> logger,
    AttendanceGatewayRawCommunicationLogger rawCommunicationLogger)
{
    private readonly CoreApiOptions _options = options.Value;

    public bool IsDirectCoreApiEnabled()
        => CanUseDirectCoreApi();

    public async Task<AttendancePublishResult> SendAttendanceLogsAsync(string deviceSn, List<AttLogDto> logs, CancellationToken cancellationToken)
    {
        if (logs.Count == 0)
        {
            return AttendancePublishResult.Success(0);
        }

        if (!CanUseDirectCoreApi())
        {
            logger.LogWarning(
                "Skipped direct Core API attendance forwarding because standalone mode disables backend direct calls. DeviceSn={DeviceSn}, Count={Count}",
                deviceSn,
                logs.Count);
            return AttendancePublishResult.NonRetryableFailure();
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"{_options.AttendanceEndpoint}?sn={Uri.EscapeDataString(deviceSn)}",
                logs,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Successfully forwarded {Count} logs for SN: {DeviceSn} to Core API.", logs.Count, deviceSn);
                return AttendancePublishResult.Success(logs.Count);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning("Core API rejected attendance logs without retry because endpoint is unavailable. DeviceSn={DeviceSn}, Status={Status}", deviceSn, response.StatusCode);
                return AttendancePublishResult.NonRetryableFailure();
            }

            logger.LogWarning("Core API rejected logs for {DeviceSn}. HTTP Status: {Status}", deviceSn, response.StatusCode);
            return AttendancePublishResult.RetryableFailure();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to Core API to forward logs for SN: {DeviceSn}", deviceSn);
            return AttendancePublishResult.RetryableFailure();
        }
    }

    public async Task<SystemLogPublishResult> PublishSystemLogAsync(
        string deviceSn,
        string connectionId,
        string direction,
        string eventType,
        string message,
        CancellationToken cancellationToken)
    {
        var request = new CoreApiSystemLogRequest(
            DeviceSn: deviceSn,
            ConnectionId: connectionId,
            Direction: direction,
            EventType: eventType,
            Message: message,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        return await PublishSystemLogAsync(request, cancellationToken);
    }

    public async Task<SystemLogPublishResult> PublishSystemLogAsync(CoreApiSystemLogRequest request, CancellationToken cancellationToken)
    {
        if (!CanUseDirectCoreApi())
        {
            logger.LogDebug(
                "Skipped direct Core API system log publish because standalone mode disables backend direct calls. DeviceSn={DeviceSn}, EventType={EventType}",
                request.DeviceSn,
                request.EventType);
            return SystemLogPublishResult.NonRetryableFailure();
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(_options.SystemLogEndpoint, request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return SystemLogPublishResult.Success();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "Core API system log endpoint is not available ({StatusCode}). DeviceSn={DeviceSn}, Endpoint={Endpoint}",
                    response.StatusCode,
                    request.DeviceSn,
                    _options.SystemLogEndpoint);
                return SystemLogPublishResult.NonRetryableFailure();
            }

            logger.LogDebug("Core API system log endpoint returned {StatusCode} for device {DeviceSn}", response.StatusCode, request.DeviceSn);
            return SystemLogPublishResult.RetryableFailure();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to publish system log for SN: {DeviceSn}", request.DeviceSn);
            return SystemLogPublishResult.RetryableFailure();
        }
    }

    public async Task<bool> PublishAdmsRealtimeEventAsync(CoreApiAdmsRealtimeEventRequest request, CancellationToken cancellationToken)
    {
        if (!CanUseDirectCoreApi())
        {
            return false;
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(_options.RealtimeEndpoint, request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Successfully forwarded realtime ADMS event for SN={DeviceSn}, EventType={EventType} to Core API.",
                    request.Sn,
                    request.EventType);
                return true;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "Core API rejected realtime event without retry because endpoint is unavailable. DeviceSn={DeviceSn}, Endpoint={Endpoint}",
                    request.Sn,
                    _options.RealtimeEndpoint);
                await rawCommunicationLogger.LogGatewayErrorAsync(
                    request.FlowId ?? "<none>",
                    request.ConnectionId ?? "<none>",
                    BuildRealtimeForwardFailureLog(
                        request,
                        $"Core API realtime endpoint is unavailable. StatusCode={(int)response.StatusCode} {response.StatusCode}. TargetUrl={ResolveRealtimeTargetUrl()}"),
                    cancellationToken);
                return false;
            }

            logger.LogWarning(
                "Core API rejected realtime event for SN={DeviceSn}. HTTP Status: {Status}",
                request.Sn,
                response.StatusCode);
            await rawCommunicationLogger.LogGatewayErrorAsync(
                request.FlowId ?? "<none>",
                request.ConnectionId ?? "<none>",
                BuildRealtimeForwardFailureLog(
                    request,
                    $"Core API rejected realtime event. StatusCode={(int)response.StatusCode} {response.StatusCode}. TargetUrl={ResolveRealtimeTargetUrl()}"),
                cancellationToken);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to Core API to forward realtime event for SN: {DeviceSn}", request.Sn);
            await rawCommunicationLogger.LogGatewayErrorAsync(
                request.FlowId ?? "<none>",
                request.ConnectionId ?? "<none>",
                BuildRealtimeForwardFailureLog(
                    request,
                    $"Exception={ex.GetType().Name}: {ex.Message}. TargetUrl={ResolveRealtimeTargetUrl()}"),
                cancellationToken);
            return false;
        }
    }

    private bool CanUseDirectCoreApi()
        => _options.Enabled && !string.IsNullOrWhiteSpace(_options.BaseUrl);

    private string ResolveRealtimeTargetUrl()
    {
        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return _options.RealtimeEndpoint;
        }

        return new Uri(baseUri, _options.RealtimeEndpoint).ToString();
    }

    private static string BuildRealtimeForwardFailureLog(
        CoreApiAdmsRealtimeEventRequest request,
        string failureDetail)
    {
        return string.Join(
            Environment.NewLine,
            [
                "Realtime forward to HRM failed.",
                $"Failure={failureDetail}",
                $"DeviceSn={request.Sn ?? "<none>"}",
                $"DeviceName={request.DeviceName ?? "<none>"}",
                $"EventType={request.EventType}",
                $"FlowId={request.FlowId ?? "<none>"}",
                $"ConnectionId={request.ConnectionId ?? "<none>"}",
                $"Direction={request.Direction}",
                $"LogStatus={request.LogStatus}",
                $"GatewayRequest={request.RequestMethod} {request.RequestUrl}",
                $"ReceivedAtUtc={request.ReceivedAtUtc:O}",
                $"Summary={request.SummaryText ?? "<none>"}"
            ]);
    }
}

public readonly record struct SystemLogPublishResult(bool IsSuccess, bool ShouldRetry)
{
    public static SystemLogPublishResult Success() => new(true, false);

    public static SystemLogPublishResult RetryableFailure() => new(false, true);

    public static SystemLogPublishResult NonRetryableFailure() => new(false, false);
}

public readonly record struct AttendancePublishResult(int DeliveredCount, bool IsSuccess, bool ShouldRetry)
{
    public static AttendancePublishResult Success(int deliveredCount) => new(deliveredCount, true, false);

    public static AttendancePublishResult RetryableFailure() => new(0, false, true);

    public static AttendancePublishResult NonRetryableFailure() => new(0, false, false);
}
