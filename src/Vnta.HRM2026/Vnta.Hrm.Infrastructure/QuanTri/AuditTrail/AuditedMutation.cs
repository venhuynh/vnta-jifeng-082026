using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

/// <summary>
/// Transactional path for raw SQL, bulk, import, and operation-level batch writes that bypass
/// EF's change tracker. The business mutation and its single audit event succeed or fail together.
/// </summary>
public sealed class AuditedMutation : IAuditedMutation
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditScope _auditScope;

    public AuditedMutation(ApplicationDbContext dbContext, IAuditScope auditScope)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditScope = auditScope ?? throw new ArgumentNullException(nameof(auditScope));
    }

    public async Task<T> ExecuteAsync<T>(
        AuditCommand command,
        Func<CancellationToken, Task<T>> mutation,
        Func<T, AuditOperationEvent> eventFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(mutation);
        ArgumentNullException.ThrowIfNull(eventFactory);

        // Preserve the caller's capture mode: feature endpoints request
        // entity-level changes, while legacy callers can opt into operation-only.
        var operationCommand = command;

        using var scope = _auditScope.Begin(operationCommand);

        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await ExecuteAndSaveAsync(operationCommand, mutation, eventFactory, cancellationToken)
                .ConfigureAwait(false);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(
                token => ExecuteInOwnedTransactionAsync(operationCommand, mutation, eventFactory, token),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<T> ExecuteInOwnedTransactionAsync<T>(
        AuditCommand command,
        Func<CancellationToken, Task<T>> mutation,
        Func<T, AuditOperationEvent> eventFactory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var result = await ExecuteAndSaveAsync(command, mutation, eventFactory, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Preserve the original mutation or audit failure.
            }

            throw;
        }
    }

    private async Task<T> ExecuteAndSaveAsync<T>(
        AuditCommand command,
        Func<CancellationToken, Task<T>> mutation,
        Func<T, AuditOperationEvent> eventFactory,
        CancellationToken cancellationToken)
    {
        var result = await mutation(cancellationToken).ConfigureAwait(false);
        var operationEvent = eventFactory(result)
            ?? throw new InvalidOperationException("An audited operation must produce an audit event.");

        ValidateOperationEvent(operationEvent);
        _auditScope.RefineAction(operationEvent.Action);
        _auditScope.SetOperationOutcome(operationEvent.Outcome);

        var auditEvent = CreateAuditEvent(command, operationEvent);
        auditEvent.PendingCaptureToken = Guid.NewGuid();
        _dbContext.Set<AuditEventRow>().Add(auditEvent);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            auditEvent.PendingCaptureToken = null;
            return result;
        }
        catch
        {
            DetachStagedOperationEvent(auditEvent);
            throw;
        }
    }

    private static AuditEventRow CreateAuditEvent(AuditCommand command, AuditOperationEvent operationEvent) =>
        new()
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ActorId = command.Actor.ActorId,
            ActorDisplayName = command.Actor.DisplayName,
            ActorKind = command.Actor.Kind,
            Source = command.Actor.Source,
            Action = operationEvent.Action,
            EntityType = operationEvent.EntityType,
            EntityId = operationEvent.EntityId,
            EntityDisplayName = operationEvent.EntityDisplayName,
            CorrelationId = command.CorrelationId,
            OperationId = command.OperationId,
            EventKey = command.EventKey,
            MetadataJson = AuditChangeCollector.SerializeMetadata(
                MergeMetadata(command.Metadata, operationEvent.Metadata, operationEvent.Outcome)),
            SchemaVersion = 1
        };

    private void DetachStagedOperationEvent(AuditEventRow auditEvent)
    {
        var entry = _dbContext.Entry(auditEvent);
        if (entry.State == EntityState.Added)
        {
            entry.State = EntityState.Detached;
        }
    }

    private static IReadOnlyDictionary<string, string> MergeMetadata(
        IReadOnlyDictionary<string, string>? commandMetadata,
        IReadOnlyDictionary<string, string>? operationMetadata,
        AuditOperationOutcome outcome)
    {
        var merged = new Dictionary<string, string>(StringComparer.Ordinal);

        if (commandMetadata is not null)
        {
            foreach (var (key, value) in commandMetadata)
            {
                merged[key] = value;
            }
        }

        if (operationMetadata is not null)
        {
            foreach (var (key, value) in operationMetadata)
            {
                merged[key] = value;
            }
        }

        merged["outcome"] = outcome.ToString();
        return merged;
    }

    private static void ValidateOperationEvent(AuditOperationEvent operationEvent)
    {
        ArgumentNullException.ThrowIfNull(operationEvent);

        if (string.IsNullOrWhiteSpace(operationEvent.Action) || operationEvent.Action.Length > 100)
        {
            throw new ArgumentException("An audit operation action between 1 and 100 characters is required.", nameof(operationEvent));
        }

        if (string.IsNullOrWhiteSpace(operationEvent.EntityType) || operationEvent.EntityType.Length > 200)
        {
            throw new ArgumentException("An audit operation entity type between 1 and 200 characters is required.", nameof(operationEvent));
        }
    }
}
