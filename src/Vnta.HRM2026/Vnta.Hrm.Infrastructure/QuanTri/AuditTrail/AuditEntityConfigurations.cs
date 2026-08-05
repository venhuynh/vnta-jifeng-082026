using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

public sealed class AuditEventRowConfiguration : IEntityTypeConfiguration<AuditEventRow>
{
    public void Configure(EntityTypeBuilder<AuditEventRow> builder)
    {
        builder.ToTable("events", "audit");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ActorId)
            .HasColumnName("actor_id")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.ActorDisplayName)
            .HasColumnName("actor_display_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.ActorKind)
            .HasColumnName("actor_kind")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id")
            .HasColumnType("text");

        builder.Property(x => x.EntityDisplayName)
            .HasColumnName("entity_display_name")
            .HasColumnType("text");

        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.OperationId)
            .HasColumnName("operation_id")
            .IsRequired();

        builder.Property(x => x.EventKey)
            .HasColumnName("event_key")
            .HasMaxLength(200);

        builder.Property(x => x.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(x => x.SchemaVersion)
            .HasColumnName("schema_version")
            .HasColumnType("smallint")
            .HasDefaultValue((short)1)
            .IsRequired();

        builder.HasIndex(x => x.OccurredAtUtc)
            .IsDescending()
            .HasDatabaseName("ix_audit_events_occurred_at_utc_desc");

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_audit_events_entity_occurred_at_utc_desc");

        builder.HasIndex(x => new { x.ActorId, x.OccurredAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("ix_audit_events_actor_occurred_at_utc_desc");

        builder.HasIndex(x => new { x.Action, x.OccurredAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("ix_audit_events_action_occurred_at_utc_desc");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("ix_audit_events_correlation_id");

        builder.HasIndex(x => x.OperationId)
            .HasDatabaseName("ix_audit_events_operation_id");

        builder.HasIndex(x => x.EventKey)
            .IsUnique()
            .HasFilter("\"event_key\" IS NOT NULL")
            .HasDatabaseName("ux_audit_events_event_key");
    }
}

public sealed class AuditPropertyChangeRowConfiguration : IEntityTypeConfiguration<AuditPropertyChangeRow>
{
    public void Configure(EntityTypeBuilder<AuditPropertyChangeRow> builder)
    {
        builder.ToTable("property_changes", "audit");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.AuditEventId)
            .HasColumnName("audit_event_id")
            .IsRequired();

        builder.Property(x => x.PropertyName)
            .HasColumnName("property_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PropertyLabel)
            .HasColumnName("property_label")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.OldValueJson)
            .HasColumnName("old_value_json")
            .HasColumnType("jsonb");

        builder.Property(x => x.NewValueJson)
            .HasColumnName("new_value_json")
            .HasColumnType("jsonb");

        builder.Property(x => x.OldDisplay)
            .HasColumnName("old_display")
            .HasColumnType("text");

        builder.Property(x => x.NewDisplay)
            .HasColumnName("new_display")
            .HasColumnType("text");

        builder.Property(x => x.IsSensitive)
            .HasColumnName("is_sensitive")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.OldCiphertext)
            .HasColumnName("old_ciphertext")
            .HasColumnType("bytea");

        builder.Property(x => x.NewCiphertext)
            .HasColumnName("new_ciphertext")
            .HasColumnType("bytea");

        builder.Property(x => x.EncryptionKeyId)
            .HasColumnName("encryption_key_id")
            .HasMaxLength(100);

        builder.HasIndex(x => x.AuditEventId)
            .HasDatabaseName("ix_audit_property_changes_audit_event_id");

        builder.HasOne(x => x.AuditEvent)
            .WithMany(x => x.PropertyChanges)
            .HasForeignKey(x => x.AuditEventId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
