using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

/// <summary>
/// Stages audit rows in the same DbContext and transaction as an EF tracked mutation.
/// The interceptor deliberately never calls SaveChanges itself.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditScope _auditScope;
    private readonly AuditChangeCollector _collector;

    public AuditSaveChangesInterceptor(IAuditScope auditScope, IAuditPolicy policy)
    {
        _auditScope = auditScope ?? throw new ArgumentNullException(nameof(auditScope));
        _collector = new AuditChangeCollector(policy);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StageAuditRows(eventData.Context, result);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StageAuditRows(eventData.Context, result);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ClearPendingCaptureMarkers(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ClearPendingCaptureMarkers(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        DetachPendingAuditRows(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DetachPendingAuditRows(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public override void SaveChangesCanceled(DbContextEventData eventData)
    {
        DetachPendingAuditRows(eventData.Context);
        base.SaveChangesCanceled(eventData);
    }

    public override Task SaveChangesCanceledAsync(
        DbContextEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DetachPendingAuditRows(eventData.Context);
        return base.SaveChangesCanceledAsync(eventData, cancellationToken);
    }

    public override InterceptionResult ThrowingConcurrencyException(
        ConcurrencyExceptionEventData eventData,
        InterceptionResult result)
    {
        DetachPendingAuditRows(eventData.Context);
        return base.ThrowingConcurrencyException(eventData, result);
    }

    public override ValueTask<InterceptionResult> ThrowingConcurrencyExceptionAsync(
        ConcurrencyExceptionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        DetachPendingAuditRows(eventData.Context);
        return base.ThrowingConcurrencyExceptionAsync(eventData, result, cancellationToken);
    }

    private void StageAuditRows(DbContext? context, InterceptionResult<int> result)
    {
        if (context is null)
        {
            return;
        }

        NormalizeTimestampWithoutTimeZoneValues(context);

        if (result.HasResult
            || !HasAuditModel(context)
            || _auditScope.Current is not { CaptureMode: AuditCaptureMode.EntityChanges } command
            || HasPendingCapture(context))
        {
            return;
        }

        context.ChangeTracker.DetectChanges();
        var auditEvents = _collector.Collect(context.ChangeTracker, command);
        if (auditEvents.Count == 0)
        {
            return;
        }

        var captureToken = Guid.NewGuid();
        foreach (var auditEvent in auditEvents)
        {
            auditEvent.PendingCaptureToken = captureToken;
        }

        context.Set<AuditEventRow>().AddRange(auditEvents);
    }

    private static void NormalizeTimestampWithoutTimeZoneValues(DbContext context)
    {
        if(!context.Database.IsRelational())
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var property in entry.Properties)
            {
                var clrType = property.Metadata.ClrType;
                if ((clrType != typeof(DateTime) && clrType != typeof(DateTime?))
                    || !string.Equals(
                        property.Metadata.GetColumnType(),
                        "timestamp without time zone",
                        StringComparison.OrdinalIgnoreCase)
                    || property.CurrentValue is not DateTime value)
                {
                    continue;
                }

                // Npgsql từ chối UTC/Local cho cột không có múi giờ; giữ nguyên clock value để không đổi nghĩa dữ liệu đã lưu.
                property.CurrentValue = PostgreSqlTimestamp.ToTimestampWithoutTimeZone(value);
            }
        }
    }

    private static bool HasAuditModel(DbContext context) =>
        context.Model.FindEntityType(typeof(AuditEventRow)) is not null
        && context.Model.FindEntityType(typeof(AuditPropertyChangeRow)) is not null;

    private static bool HasPendingCapture(DbContext context) =>
        context.ChangeTracker.Entries<AuditEventRow>()
            .Any(entry => entry.Entity.PendingCaptureToken is not null);

    private static void ClearPendingCaptureMarkers(DbContext? context)
    {
        if (context is null || !HasAuditModel(context))
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<AuditEventRow>())
        {
            entry.Entity.PendingCaptureToken = null;
        }
    }

    private static void DetachPendingAuditRows(DbContext? context)
    {
        if (context is null || !HasAuditModel(context))
        {
            return;
        }

        var pendingEvents = context.ChangeTracker.Entries<AuditEventRow>()
            .Where(entry => entry.State == EntityState.Added && entry.Entity.PendingCaptureToken is not null)
            .ToArray();

        if (pendingEvents.Length == 0)
        {
            return;
        }

        var pendingEventIds = pendingEvents
            .Select(entry => entry.Entity.Id)
            .ToHashSet();

        var pendingChanges = context.ChangeTracker.Entries<AuditPropertyChangeRow>()
            .Where(entry => entry.State == EntityState.Added
                && (pendingEventIds.Contains(entry.Entity.AuditEventId)
                    || entry.Entity.AuditEvent?.PendingCaptureToken is not null))
            .ToArray();

        foreach (var propertyChange in pendingChanges)
        {
            propertyChange.State = EntityState.Detached;
        }

        foreach (var auditEvent in pendingEvents)
        {
            auditEvent.State = EntityState.Detached;
        }
    }
}
