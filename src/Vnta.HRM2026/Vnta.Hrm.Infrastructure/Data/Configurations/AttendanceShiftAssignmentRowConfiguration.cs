using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.Data.Configurations;

public sealed class AttendanceShiftAssignmentRowConfiguration : IEntityTypeConfiguration<AttendanceShiftAssignmentRow>
{
    public void Configure(EntityTypeBuilder<AttendanceShiftAssignmentRow> builder)
    {
        builder.ToTable("shift_assignments");

        builder.HasKey(x => x.Id);

        builder.Property<uint>("xmin")
            .IsRowVersion();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WorkDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.CreationType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.WorkDate)
            .HasDatabaseName("IX_shift_assignments_WorkDate");

        builder.HasIndex(x => new { x.ShiftId, x.WorkDate })
            .HasDatabaseName("IX_shift_assignments_ShiftId_WorkDate");

        builder.HasIndex(x => x.CreationType)
            .HasDatabaseName("IX_shift_assignments_CreationType");

        builder.HasIndex(x => new { x.EmployeeId, x.WorkDate })
            .IsUnique()
            .HasDatabaseName("UX_shift_assignments_EmployeeId_WorkDate");

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AttendanceShiftRow>()
            .WithMany()
            .HasForeignKey(x => x.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
