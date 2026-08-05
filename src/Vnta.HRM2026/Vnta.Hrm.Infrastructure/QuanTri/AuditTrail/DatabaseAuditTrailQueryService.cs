using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

/// <summary>
/// Reads the append-only audit trail with a fixed, database-backed ordering. The transport
/// boundary is responsible for authorization and supplies the immutable <see cref="AuditReadAccess"/>
/// used to mask sensitive property values before a DTO leaves the server.
/// </summary>
public sealed class DatabaseAuditTrailQueryService : IAuditTrailQueryService
{
    public const int MaxPageSize = 100;
    public const int MaxContextTake = 100;
    public static readonly TimeSpan MaxTimeWindow = TimeSpan.FromDays(366);

    private const int MaxActorIdLength = 256;
    private const int MaxActionLength = 100;
    private const int MaxEntityTypeLength = 200;
    private const int MaxEntityIdLength = 512;
    private const int MaxCorrelationIdLength = 128;

    private readonly ApplicationDbContext _dbContext;

    public DatabaseAuditTrailQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<AuditEventPageDto> GetPageAsync(
        AuditEventFilter filter,
        AuditReadAccess access,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(access);

        var normalizedFilter = NormalizeAndValidateFilter(filter);
        var rows = await ApplyFilter(_dbContext.AuditEvents.AsNoTracking(), normalizedFilter)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(ToListItemExpression)
            .Take(normalizedFilter.PageSize + 1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hasNextPage = rows.Count > normalizedFilter.PageSize;
        if (hasNextPage)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        var nextCursor = hasNextPage
            ? new AuditEventCursor(rows[^1].OccurredAtUtc, rows[^1].Id)
            : null;

        return new AuditEventPageDto(rows, nextCursor);
    }

    public async Task<AuditEventDetailDto?> GetDetailAsync(
        Guid id,
        AuditReadAccess access,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);

        var auditEvent = await _dbContext.AuditEvents
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AuditEventHeaderProjection(
                x.Id,
                x.OccurredAtUtc,
                x.ActorId,
                x.ActorDisplayName,
                x.ActorKind,
                x.Source,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.EntityDisplayName,
                x.CorrelationId,
                x.OperationId))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (auditEvent is null)
        {
            return null;
        }

        var propertyChanges = await _dbContext.AuditPropertyChanges
            .AsNoTracking()
            .Where(change => change.AuditEventId == auditEvent.Id)
            .OrderBy(change => change.PropertyName)
            .ThenBy(change => change.Id)
            .Select(change => new AuditPropertyChangeProjection(
                change.PropertyName,
                change.PropertyLabel,
                change.OldDisplay,
                change.NewDisplay,
                change.IsSensitive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AuditEventDetailDto(
            auditEvent.Id,
            auditEvent.OccurredAtUtc,
            auditEvent.ActorId,
            auditEvent.ActorDisplayName,
            auditEvent.ActorKind,
            auditEvent.Source,
            auditEvent.Action,
            auditEvent.EntityType,
            auditEvent.EntityId,
            auditEvent.EntityDisplayName,
            auditEvent.CorrelationId,
            auditEvent.OperationId,
            propertyChanges
                .Select(change => ToPropertyChangeDto(change, access))
                .ToList());
    }

    public async Task<AuditEventContextDto?> GetContextAsync(
        Guid eventId,
        AuditReadAccess access,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);

        if (take is < 1 or > MaxContextTake)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take),
                take,
                $"Context take must be between 1 and {MaxContextTake}.");
        }

        var correlationId = await _dbContext.AuditEvents
            .AsNoTracking()
            .Where(x => x.Id == eventId)
            .Select(x => x.CorrelationId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (correlationId is null)
        {
            return null;
        }

        var items = await _dbContext.AuditEvents
            .AsNoTracking()
            .Where(x => x.CorrelationId == correlationId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(ToListItemExpression)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AuditEventContextDto(correlationId, items);
    }

    private static IQueryable<AuditEventRow> ApplyFilter(
        IQueryable<AuditEventRow> query,
        NormalizedAuditEventFilter filter)
    {
        if (filter.FromUtc is { } fromUtc)
        {
            query = query.Where(x => x.OccurredAtUtc >= fromUtc);
        }

        if (filter.ToUtc is { } toUtc)
        {
            query = query.Where(x => x.OccurredAtUtc <= toUtc);
        }

        if (filter.ActorId is not null)
        {
            query = query.Where(x => x.ActorId == filter.ActorId);
        }

        if (filter.Action is not null)
        {
            query = query.Where(x => x.Action == filter.Action);
        }

        if (filter.EntityType is not null)
        {
            query = query.Where(x => x.EntityType == filter.EntityType);
        }

        if (filter.EntityId is not null)
        {
            query = query.Where(x => x.EntityId == filter.EntityId);
        }

        if (filter.CorrelationId is not null)
        {
            query = query.Where(x => x.CorrelationId == filter.CorrelationId);
        }

        if (filter.Cursor is { } cursor)
        {
            query = query.Where(x =>
                x.OccurredAtUtc < cursor.OccurredAtUtc ||
                (x.OccurredAtUtc == cursor.OccurredAtUtc && x.Id.CompareTo(cursor.Id) < 0));
        }

        return query;
    }

    private static NormalizedAuditEventFilter NormalizeAndValidateFilter(AuditEventFilter filter)
    {
        if (filter.PageSize is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(filter),
                filter.PageSize,
                $"Page size must be between 1 and {MaxPageSize}.");
        }

        var fromUtc = filter.FromUtc?.ToUniversalTime();
        var toUtc = filter.ToUtc?.ToUniversalTime();

        if (fromUtc is { } from && toUtc is { } to)
        {
            if (from > to)
            {
                throw new ArgumentException("FromUtc must be earlier than or equal to ToUtc.", nameof(filter));
            }

            if (to - from > MaxTimeWindow)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(filter),
                    $"The requested time window must not exceed {MaxTimeWindow.TotalDays:0} days.");
            }
        }

        return new NormalizedAuditEventFilter(
            fromUtc,
            toUtc,
            NormalizeFilterValue(filter.ActorId, MaxActorIdLength, nameof(filter.ActorId)),
            NormalizeFilterValue(filter.Action, MaxActionLength, nameof(filter.Action)),
            NormalizeFilterValue(filter.EntityType, MaxEntityTypeLength, nameof(filter.EntityType)),
            NormalizeFilterValue(filter.EntityId, MaxEntityIdLength, nameof(filter.EntityId)),
            NormalizeFilterValue(filter.CorrelationId, MaxCorrelationIdLength, nameof(filter.CorrelationId)),
            NormalizeCursor(filter.Cursor),
            filter.PageSize);
    }

    private static AuditEventCursor? NormalizeCursor(AuditEventCursor? cursor) =>
        cursor is null
            ? null
            : new AuditEventCursor(cursor.OccurredAtUtc.ToUniversalTime(), cursor.Id);

    private static string? NormalizeFilterValue(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                normalized.Length,
                $"{parameterName} must not exceed {maxLength} characters.");
        }

        return normalized;
    }

    internal static AuditPropertyChangeDto ToPropertyChangeDto(
        AuditPropertyChangeProjection change,
        AuditReadAccess access) =>
        new(
            change.PropertyName,
            change.PropertyLabel,
            change.IsSensitive && !access.CanReadSensitiveValues ? null : change.OldDisplay,
            change.IsSensitive && !access.CanReadSensitiveValues ? null : change.NewDisplay,
            change.IsSensitive,
            Changed: true);

    private static readonly System.Linq.Expressions.Expression<Func<AuditEventRow, AuditEventListItemDto>> ToListItemExpression =
        auditEvent => new AuditEventListItemDto(
            auditEvent.Id,
            auditEvent.OccurredAtUtc,
            auditEvent.ActorId,
            auditEvent.ActorDisplayName,
            auditEvent.ActorKind,
            auditEvent.Source,
            auditEvent.Action,
            auditEvent.EntityType,
            auditEvent.EntityId,
            auditEvent.EntityDisplayName,
            auditEvent.CorrelationId,
            auditEvent.OperationId);

    internal sealed record AuditPropertyChangeProjection(
        string PropertyName,
        string PropertyLabel,
        string? OldDisplay,
        string? NewDisplay,
        bool IsSensitive);

    private sealed record AuditEventHeaderProjection(
        Guid Id,
        DateTimeOffset OccurredAtUtc,
        string ActorId,
        string ActorDisplayName,
        AuditActorKind ActorKind,
        AuditSource Source,
        string Action,
        string EntityType,
        string? EntityId,
        string? EntityDisplayName,
        string CorrelationId,
        Guid OperationId);

    private sealed record NormalizedAuditEventFilter(
        DateTimeOffset? FromUtc,
        DateTimeOffset? ToUtc,
        string? ActorId,
        string? Action,
        string? EntityType,
        string? EntityId,
        string? CorrelationId,
        AuditEventCursor? Cursor,
        int PageSize);
}
