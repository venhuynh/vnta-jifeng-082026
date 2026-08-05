using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Protocol.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vnta.AttendanceGateway.Workers;

public sealed class AttendancePublishWorker : BackgroundService
{
    private const int MaxAttemptCount = 3;
    private const int BatchSize = 100;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CoreApiClient _coreApiClient;
    private readonly ILogger<AttendancePublishWorker> _logger;

    public AttendancePublishWorker(
        IServiceScopeFactory scopeFactory,
        CoreApiClient coreApiClient,
        ILogger<AttendancePublishWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _coreApiClient = coreApiClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Attendance publish worker is running.");

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
                    _logger.LogWarning(ex, "Attendance publish worker hit an unexpected error and will retry.");
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            _logger.LogInformation("Attendance publish worker is stopping.");
        }
    }

    private async Task<int> ProcessNextBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
        var now = DateTimeOffset.UtcNow;

        var items = await dbContext.OutboundAttendanceLogs
            .Where(item =>
                (item.Status == OutboundDeliveryStatuses.Pending || item.Status == OutboundDeliveryStatuses.Retrying)
                && (item.NextAttemptAtUtc == null || item.NextAttemptAtUtc <= now))
            .OrderBy(item => item.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return 0;
        }

        foreach (var group in items.GroupBy(item => item.DeviceSn, StringComparer.OrdinalIgnoreCase))
        {
            await ProcessGroupAsync(dbContext, group.Key, group.ToList(), cancellationToken);
        }

        return items.Count;
    }

    private async Task ProcessGroupAsync(
        ZktecoDbContext dbContext,
        string deviceSn,
        List<Vnta.AttendanceGateway.Domain.ZktecoOutboundAttendanceLog> items,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in items)
        {
            item.AttemptCount += 1;
            item.LastAttemptAtUtc = now;
            item.UpdatedAtUtc = now;
        }

        try
        {
            var publishResult = await _coreApiClient.SendAttendanceLogsAsync(
                deviceSn,
                items.Select(ToDto).ToList(),
                cancellationToken);

            if (publishResult.IsSuccess)
            {
                foreach (var item in items)
                {
                    item.Status = OutboundDeliveryStatuses.Delivered;
                    item.DeliveredAtUtc = DateTimeOffset.UtcNow;
                    item.NextAttemptAtUtc = null;
                    item.LastError = null;
                    item.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogDebug(
                    "Delivered outbound attendance batch. DeviceSn={DeviceSn}, ItemCount={ItemCount}, DeliveredCount={DeliveredCount}",
                    deviceSn,
                    items.Count,
                    publishResult.DeliveredCount);

                return;
            }

            foreach (var item in items)
            {
                ApplyFailureState(
                    item,
                    publishResult.ShouldRetry,
                    publishResult.ShouldRetry
                        ? "Retryable attendance delivery failure."
                        : "Non-retryable attendance delivery failure.");
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Attendance batch delivery failed. DeviceSn={DeviceSn}, ItemCount={ItemCount}, ShouldRetry={ShouldRetry}",
                deviceSn,
                items.Count,
                publishResult.ShouldRetry);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            foreach (var item in items)
            {
                ApplyFailureState(item, true, ex.Message);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                ex,
                "Unexpected error while processing durable outbound attendance batch. DeviceSn={DeviceSn}, ItemCount={ItemCount}",
                deviceSn,
                items.Count);
        }
    }

    private static void ApplyFailureState(
        Vnta.AttendanceGateway.Domain.ZktecoOutboundAttendanceLog item,
        bool shouldRetry,
        string errorMessage)
    {
        var now = DateTimeOffset.UtcNow;
        var canRetry = shouldRetry && item.AttemptCount < MaxAttemptCount;

        item.Status = canRetry
            ? OutboundDeliveryStatuses.Retrying
            : OutboundDeliveryStatuses.Failed;
        item.NextAttemptAtUtc = canRetry ? now.Add(RetryDelay) : null;
        item.LastError = canRetry
            ? errorMessage
            : $"Retry limit reached or non-retryable failure. {errorMessage}";
        item.FailedAtUtc = canRetry ? null : now;
        item.UpdatedAtUtc = now;
    }

    private static AttLogDto ToDto(Vnta.AttendanceGateway.Domain.ZktecoOutboundAttendanceLog item)
        => new(
            item.EmployeeCode,
            DateTime.SpecifyKind(item.TapTime, DateTimeKind.Unspecified),
            item.VerificationMode,
            item.InOutMode);
}
