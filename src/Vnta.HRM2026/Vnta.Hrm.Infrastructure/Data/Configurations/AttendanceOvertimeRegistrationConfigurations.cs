using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.Data.Configurations;

public sealed class AttendanceOvertimeRegistrationRequestRowConfiguration
    : IEntityTypeConfiguration<AttendanceOvertimeRegistrationRequestRow>
{
    public void Configure(EntityTypeBuilder<AttendanceOvertimeRegistrationRequestRow> builder)
    {
        builder.ToTable("attendance_overtime_registration_requests");

        builder.HasKey(x => x.Id);

        builder.Property<uint>("xmin")
            .IsRowVersion();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WorkDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.DayType)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.WorkshopCode)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.WorkshopName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.RequestedBy)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ApprovedBy)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.Note)
            .HasColumnType("text")
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(x => x.LastActionAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.SubmittedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.ApprovedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(128);

        builder.HasIndex(x => x.WorkDate)
            .HasDatabaseName("IX_attendance_overtime_registration_requests_WorkDate");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_attendance_overtime_registration_requests_Status");

        builder.HasIndex(x => new { x.WorkshopCode, x.WorkDate })
            .IsUnique()
            .HasDatabaseName("UX_attendance_overtime_registration_requests_WorkshopCode_WorkDate");

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.RequestedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceOvertimeRegistrationDetailRowConfiguration
    : IEntityTypeConfiguration<AttendanceOvertimeRegistrationDetailRow>
{
    public void Configure(EntityTypeBuilder<AttendanceOvertimeRegistrationDetailRow> builder)
    {
        builder.ToTable("attendance_overtime_registration_details");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.EmployeeCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.EmployeeName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.PositionName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.TeamCode)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.TeamName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.AssignmentType)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.RequestId)
            .HasDatabaseName("IX_attendance_overtime_registration_details_RequestId");

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_attendance_overtime_registration_details_EmployeeId");

        builder.HasIndex(x => new { x.RequestId, x.EmployeeId })
            .IsUnique()
            .HasDatabaseName("UX_attendance_overtime_registration_details_RequestId_EmployeeId");

        builder.HasOne<AttendanceOvertimeRegistrationRequestRow>()
            .WithMany()
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceOvertimeRegistrationHistoryRowConfiguration
    : IEntityTypeConfiguration<AttendanceOvertimeRegistrationHistoryRow>
{
    public void Configure(EntityTypeBuilder<AttendanceOvertimeRegistrationHistoryRow> builder)
    {
        builder.ToTable("attendance_overtime_registration_histories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FromStatus)
            .HasConversion<short?>()
            .HasColumnType("smallint");

        builder.Property(x => x.ToStatus)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.ActionName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasColumnType("text");

        builder.Property(x => x.PerformedBy)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.PerformedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.HasIndex(x => x.RequestId)
            .HasDatabaseName("IX_attendance_overtime_registration_histories_RequestId");

        builder.HasIndex(x => x.PerformedAtUtc)
            .HasDatabaseName("IX_attendance_overtime_registration_histories_PerformedAtUtc");

        builder.HasOne<AttendanceOvertimeRegistrationRequestRow>()
            .WithMany()
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.PerformedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
