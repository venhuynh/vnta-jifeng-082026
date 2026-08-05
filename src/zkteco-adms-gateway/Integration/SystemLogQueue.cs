using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Vnta.AttendanceGateway.Integration;

public sealed class SystemLogQueue(
    IServiceScopeFactory scopeFactory,
    ILogger<SystemLogQueue> logger)
{
    public async Task<bool> EnqueueAsync(
        string deviceSn,
        string connectionId,
        string direction,
        string eventType,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
            var now = DateTimeOffset.UtcNow;

            dbContext.OutboundSystemLogs.Add(new ZktecoOutboundSystemLog
            {
                Id = Guid.NewGuid(),
                DeviceSn = deviceSn,
                ConnectionId = connectionId,
                Direction = direction,
                EventType = eventType,
                Message = message,
                OccurredAtUtc = now,
                AttemptCount = 0,
                Status = OutboundDeliveryStatuses.Pending,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                NextAttemptAtUtc = now
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to persist outbound system log. DeviceSn={DeviceSn}, EventType={EventType}",
                deviceSn,
                eventType);

            return false;
        }
    }
}
