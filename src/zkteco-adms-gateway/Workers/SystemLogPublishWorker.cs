using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Integration.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vnta.AttendanceGateway.Workers;

public sealed class SystemLogPublishWorker : BackgroundService
{
    private const int MaxAttemptCount = 3;
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CoreApiClient _coreApiClient;
    private readonly ILogger<SystemLogPublishWorker> _logger;

    public SystemLogPublishWorker(
        IServiceScopeFactory scopeFactory,
        CoreApiClient coreApiClient,
        ILogger<SystemLogPublishWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _coreApiClient = coreApiClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("System log publish worker is running.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_coreApiClient.IsDirectCoreApiEnabled())
                    {
                        await Task.Delay(PollInterval, stoppingToken);
                        continue;
                    }

                    var processedCount = await ProcessNextBatchAsync(stoppingToken);
                    if (processedCount == 0)
                    {
                        await Task.Delay(PollInterval, stoppingToken);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "System log publish worker hit an unexpected error and will retry.");
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            _logger.LogInformation("System log publish worker is stopping.");
        }
    }

    private async Task<int> ProcessNextBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
        var now = DateTimeOffset.UtcNow;

        var items = await dbContext.OutboundSystemLogs
            .Where(item =>
                (item.Status == OutboundDeliveryStatuses.Pending || item.Status == OutboundDeliveryStatuses.Retrying)
                && (item.NextAttemptAtUtc == null || item.NextAttemptAtUtc <= now))
            .OrderBy(item => item.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            await ProcessItemAsync(dbContext, item, cancellationToken);
        }

        return items.Count;
    }

    private async Task ProcessItemAsync(
        ZktecoDbContext dbContext,
        Vnta.AttendanceGateway.Domain.ZktecoOutboundSystemLog item,
        CancellationToken cancellationToken)
    {
        try
        {
            item.AttemptCount += 1;
            item.LastAttemptAtUtc = DateTimeOffset.UtcNow;
            item.UpdatedAtUtc = DateTimeOffset.UtcNow;

            var publishResult = await _coreApiClient.PublishSystemLogAsync(ToRequest(item), cancellationToken);
            if (publishResult.IsSuccess)
            {
                item.Status = OutboundDeliveryStatuses.Delivered;
                item.DeliveredAtUtc = DateTimeOffset.UtcNow;
                item.NextAttemptAtUtc = null;
                item.LastError = null;
                item.UpdatedAtUtc = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogDebug(
                    "Delivered outbound system log. Id={Id}, DeviceSn={DeviceSn}, EventType={EventType}, AttemptCount={AttemptCount}",
                    item.Id,
                    item.DeviceSn,
                    item.EventType,
                    item.AttemptCount);

                return;
            }

            if (!publishResult.ShouldRetry)
            {
                item.Status = OutboundDeliveryStatuses.Failed;
                item.FailedAtUtc = DateTimeOffset.UtcNow;
                item.NextAttemptAtUtc = null;
                item.LastError = "Non-retryable delivery failure.";
                item.UpdatedAtUtc = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Marked outbound system log as failed without retry. Id={Id}, DeviceSn={DeviceSn}, EventType={EventType}, AttemptCount={AttemptCount}",
                    item.Id,
                    item.DeviceSn,
                    item.EventType,
                    item.AttemptCount);

                return;
            }

            if (item.AttemptCount >= MaxAttemptCount)
            {
                item.Status = OutboundDeliveryStatuses.Failed;
                item.FailedAtUtc = DateTimeOffset.UtcNow;
                item.NextAttemptAtUtc = null;
                item.LastError = $"Retry limit reached after {item.AttemptCount} attempts.";
                item.UpdatedAtUtc = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Marked outbound system log as failed after retry limit. Id={Id}, DeviceSn={DeviceSn}, EventType={EventType}, AttemptCount={AttemptCount}",
                    item.Id,
                    item.DeviceSn,
                    item.EventType,
                    item.AttemptCount);

                return;
            }

            item.Status = OutboundDeliveryStatuses.Retrying;
            item.NextAttemptAtUtc = DateTimeOffset.UtcNow.Add(RetryDelay);
            item.LastError = "Retryable delivery failure.";
            item.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Scheduled retry for outbound system log. Id={Id}, DeviceSn={DeviceSn}, EventType={EventType}, AttemptCount={AttemptCount}",
                item.Id,
                item.DeviceSn,
                item.EventType,
                item.AttemptCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            item.Status = item.AttemptCount >= MaxAttemptCount
                ? OutboundDeliveryStatuses.Failed
                : OutboundDeliveryStatuses.Retrying;
            item.LastError = ex.Message;
            item.NextAttemptAtUtc = item.Status == OutboundDeliveryStatuses.Failed
                ? null
                : DateTimeOffset.UtcNow.Add(RetryDelay);
            item.FailedAtUtc = item.Status == OutboundDeliveryStatuses.Failed
                ? DateTimeOffset.UtcNow
                : null;
            item.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                ex,
                "Unexpected error while processing durable outbound system log. Id={Id}, DeviceSn={DeviceSn}, EventType={EventType}, AttemptCount={AttemptCount}",
                item.Id,
                item.DeviceSn,
                item.EventType,
                item.AttemptCount);
        }
    }

    private static CoreApiSystemLogRequest ToRequest(Vnta.AttendanceGateway.Domain.ZktecoOutboundSystemLog item)
        => new(
            item.DeviceSn,
            item.ConnectionId,
            item.Direction,
            item.EventType,
            item.Message,
            item.OccurredAtUtc);
}
