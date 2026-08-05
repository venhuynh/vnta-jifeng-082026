using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.Identity;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.Data.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.EmployeeId)
            .HasColumnType("uuid");

        builder.Property(x => x.ApprovalStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(EmployeeAccountApprovalStatus.Draft)
            .IsRequired();

        builder.Property(x => x.AccessLevel)
            .HasMaxLength(100);

        builder.Property(x => x.ApprovedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.RejectedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => x.EmployeeId)
            .IsUnique()
            .HasDatabaseName("UX_AspNetUsers_EmployeeId_Active")
            .HasFilter("\"EmployeeId\" IS NOT NULL AND \"IsActive\" = TRUE");

        builder.HasIndex(x => x.ApprovalStatus)
            .HasDatabaseName("IX_AspNetUsers_ApprovalStatus");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
