using Vnta.AttendanceGateway.Integration;

namespace Vnta.AttendanceGateway.Workers;

public sealed class AdmsRealtimePublishWorker(
    AdmsRealtimeEventQueue queue,
    CoreApiClient coreApiClient,
    ILogger<AdmsRealtimePublishWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ADMS realtime publish worker is running.");

        try {
            await foreach(var payload in queue.ReadAllAsync(stoppingToken)) {
                try {
                    if(!coreApiClient.IsDirectCoreApiEnabled()) {
                        continue;
                    }

                    await coreApiClient.PublishAdmsRealtimeEventAsync(payload, stoppingToken);
                }
                catch(Exception ex) when(ex is not OperationCanceledException) {
                    logger.LogDebug(
                        ex,
                        "Failed to forward realtime ADMS event to HRM. EventType={EventType}, DeviceSn={DeviceSn}, FlowId={FlowId}",
                        payload.EventType,
                        payload.Sn ?? "<none>",
                        payload.FlowId ?? "<none>");
                }
            }
        }
        catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) {
        }
        finally {
            logger.LogInformation("ADMS realtime publish worker is stopping.");
        }
    }
}
