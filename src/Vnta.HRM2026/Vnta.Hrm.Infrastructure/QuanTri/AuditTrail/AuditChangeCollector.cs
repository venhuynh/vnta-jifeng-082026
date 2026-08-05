using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

/// <summary>
/// Materializes audit rows from EF tracked changes before EF generates SQL. This class has no
/// persistence side effects; the interceptor is responsible for staging the returned rows.
/// </summary>
public sealed class AuditChangeCollector
{
    private const int MaxDisplayLength = 1_000;
    private const int MaxMetadataEntries = 64;
    private const int MaxMetadataKeyLength = 100;
    private const int MaxMetadataValueLength = 1_000;

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static readonly string[] SensitiveMetadataFragments =
    [
        "password",
        "secret",
        "token",
        "securitystamp",
        "template",
        "biometric",
        "payload",
        "photo",
        "image"
    ];

    private readonly IAuditPolicy _policy;

    public AuditChangeCollector(IAuditPolicy policy)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public IReadOnlyList<AuditEventRow> Collect(ChangeTracker changeTracker, AuditCommand command)
    {
        ArgumentNullException.ThrowIfNull(changeTracker);
        ArgumentNullException.ThrowIfNull(command);

        var events = new List<AuditEventRow>();

        foreach (var entry in changeTracker.Entries())
        {
            if (!CanCollect(entry, out var entityPolicy))
            {
                continue;
            }

            if (!HasStablePrimaryKey(entry))
            {
                continue;
            }

            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Deleted && !entityPolicy.AllowHardDelete)
            {
                continue;
            }

            var propertyChanges = CollectPropertyChanges(entry, entityPolicy);
            if (propertyChanges.Count == 0 && entry.State != Microsoft.EntityFrameworkCore.EntityState.Deleted)
            {
                continue;
            }

            var auditEvent = new AuditEventRow
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = DateTimeOffset.UtcNow,
                ActorId = command.Actor.ActorId,
                ActorDisplayName = command.Actor.DisplayName,
                ActorKind = command.Actor.Kind,
                Source = command.Actor.Source,
                Action = command.ActionIntent,
                EntityType = entityPolicy.LogicalEntityType,
                EntityId = FormatKey(entry),
                EntityDisplayName = FormatEntityDisplayName(entityPolicy, entry.Entity),
                CorrelationId = command.CorrelationId,
                OperationId = command.OperationId,
                EventKey = command.EventKey,
                MetadataJson = SerializeMetadata(command.Metadata),
                SchemaVersion = 1
            };

            foreach (var propertyChange in propertyChanges)
            {
                propertyChange.AuditEventId = auditEvent.Id;
                propertyChange.AuditEvent = auditEvent;
                auditEvent.PropertyChanges.Add(propertyChange);
            }

            events.Add(auditEvent);
        }

        return events;
    }

    private bool CanCollect(EntityEntry entry, out AuditEntityPolicy entityPolicy)
    {
        entityPolicy = null!;

        if (entry.Entity is AuditEventRow or AuditPropertyChangeRow)
        {
            return false;
        }

        if (entry.State is not (Microsoft.EntityFrameworkCore.EntityState.Added
            or Microsoft.EntityFrameworkCore.EntityState.Modified
            or Microsoft.EntityFrameworkCore.EntityState.Deleted))
        {
            return false;
        }

        entityPolicy = _policy.GetPolicy(entry.Metadata.ClrType)!;
        return entityPolicy is not null;
    }

    private static bool HasStablePrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null || key.Properties.Count == 0)
        {
            return false;
        }

        return key.Properties.All(property =>
        {
            var propertyEntry = entry.Property(property.Name);
            return !propertyEntry.IsTemporary && propertyEntry.CurrentValue is not null;
        });
    }

    private static List<AuditPropertyChangeRow> CollectPropertyChanges(
        EntityEntry entry,
        AuditEntityPolicy entityPolicy)
    {
        var changes = new List<AuditPropertyChangeRow>();

        foreach (var property in entry.Properties)
        {
            if (!entityPolicy.TryGetProperty(property.Metadata.Name, out var propertyPolicy))
            {
                continue;
            }

            switch (entry.State)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    changes.Add(CreateChange(property, propertyPolicy, hasOldValue: false, hasNewValue: true));
                    break;

                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                    if (property.IsModified && !ValuesEqual(property.OriginalValue, property.CurrentValue))
                    {
                        changes.Add(CreateChange(property, propertyPolicy, hasOldValue: true, hasNewValue: true));
                    }

                    break;

                case Microsoft.EntityFrameworkCore.EntityState.Deleted:
                    changes.Add(CreateChange(property, propertyPolicy, hasOldValue: true, hasNewValue: false));
                    break;
            }
        }

        return changes;
    }

    private static AuditPropertyChangeRow CreateChange(
        PropertyEntry property,
        AuditPropertyPolicy propertyPolicy,
        bool hasOldValue,
        bool hasNewValue)
    {
        var oldValue = property.OriginalValue;
        var newValue = property.CurrentValue;

        return new AuditPropertyChangeRow
        {
            Id = Guid.NewGuid(),
            PropertyName = property.Metadata.Name,
            PropertyLabel = propertyPolicy.Label,
            IsSensitive = propertyPolicy.IsSensitive,
            OldValueJson = propertyPolicy.IsSensitive || !hasOldValue ? null : SerializeValue(oldValue),
            NewValueJson = propertyPolicy.IsSensitive || !hasNewValue ? null : SerializeValue(newValue),
            OldDisplay = propertyPolicy.IsSensitive
                ? SensitiveDisplay(hasOldValue)
                : hasOldValue ? FormatDisplay(oldValue) : null,
            NewDisplay = propertyPolicy.IsSensitive
                ? SensitiveDisplay(hasNewValue)
                : hasNewValue ? FormatDisplay(newValue) : null
        };
    }

    private static bool ValuesEqual(object? originalValue, object? currentValue) =>
        Equals(originalValue, currentValue);

    private static string FormatKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey()
            ?? throw new InvalidOperationException("An audited entity must have a primary key.");

        if (key.Properties.Count == 1)
        {
            return FormatKeyValue(entry.Property(key.Properties[0].Name).CurrentValue);
        }

        return string.Join(
            ";",
            key.Properties.Select(property =>
                $"{property.Name}={FormatKeyValue(entry.Property(property.Name).CurrentValue)}"));
    }

    private static string FormatKeyValue(object? value) => value switch
    {
        Guid guid => guid.ToString("D"),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        null => string.Empty,
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static string? FormatEntityDisplayName(AuditEntityPolicy policy, object entity)
    {
        var value = policy.DisplayNameSelector?.Invoke(entity);
        return string.IsNullOrWhiteSpace(value) ? null : Truncate(value);
    }

    internal static string? SerializeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        var sanitized = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, value) in metadata)
        {
            if (sanitized.Count == MaxMetadataEntries)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(key) || IsSensitiveMetadataKey(key))
            {
                continue;
            }

            sanitized[Truncate(key, MaxMetadataKeyLength)] = Truncate(value ?? string.Empty, MaxMetadataValueLength);
        }

        return sanitized.Count == 0 ? null : JsonSerializer.Serialize(sanitized, JsonOptions);
    }

    private static bool IsSensitiveMetadataKey(string key) =>
        SensitiveMetadataFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static string SerializeValue(object? value) =>
        JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), JsonOptions);

    private static string FormatDisplay(object? value)
    {
        var display = value switch
        {
            null => "(trống)",
            bool boolean => boolean ? "Có" : "Không",
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString("D"),
            Enum enumeration => enumeration.ToString(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

        return Truncate(display);
    }

    private static string? SensitiveDisplay(bool hasValue) => hasValue ? "Changed" : null;

    private static string Truncate(string value, int maxLength = MaxDisplayLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
