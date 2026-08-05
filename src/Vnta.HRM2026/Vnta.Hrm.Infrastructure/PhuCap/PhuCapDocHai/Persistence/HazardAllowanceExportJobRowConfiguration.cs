using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

public sealed class HazardAllowanceExportJobRowConfiguration : IEntityTypeConfiguration<HazardAllowanceExportJobRow>
{
    public void Configure(EntityTypeBuilder<HazardAllowanceExportJobRow> builder)
    {
        builder.ToTable("payroll_hazard_allowance_export_jobs", table =>
        {
            table.HasCheckConstraint(
                "CK_payroll_hazard_allowance_export_jobs_Status",
                "\"Status\" IN (0, 1, 2, 3)");
        });
        builder.HasKey(row => row.Id);
        builder.Property(row => row.FilterJson).HasColumnType("jsonb").IsRequired();
        builder.Property(row => row.RequestedBy).HasMaxLength(128).IsRequired();
        builder.Property(row => row.Status).IsRequired();
        builder.Property(row => row.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(row => row.StartedAtUtc).HasColumnType("timestamp without time zone");
        builder.Property(row => row.CompletedAtUtc).HasColumnType("timestamp without time zone");
        builder.Property(row => row.FileName).HasMaxLength(260);
        builder.Property(row => row.OutputPath).HasMaxLength(2048);
        builder.Property(row => row.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(row => new { row.Status, row.CreatedAtUtc })
            .HasDatabaseName("IX_payroll_hazard_allowance_export_jobs_Status_CreatedAtUtc");
        builder.HasIndex(row => new { row.RequestedBy, row.CreatedAtUtc })
            .HasDatabaseName("IX_payroll_hazard_allowance_export_jobs_RequestedBy_CreatedAtUtc");
    }
}
