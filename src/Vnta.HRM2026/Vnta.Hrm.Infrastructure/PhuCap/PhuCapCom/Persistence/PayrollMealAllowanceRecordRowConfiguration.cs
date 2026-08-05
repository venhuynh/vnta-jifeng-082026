using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.Integrations.Payroll;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Persistence;

/// <summary>Feature-owned mapping. Table, constraint, index and concurrency names are intentionally stable.</summary>
public sealed class PayrollMealAllowanceRecordRowConfiguration : IEntityTypeConfiguration<PayrollMealAllowanceRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollMealAllowanceRecordRow> builder)
    {
        builder.ToTable("payroll_allowance_meal_records", table =>
        {
            table.HasCheckConstraint("CK_payroll_allowance_meal_records_QualifiedMealDays", "\"QualifiedMealDays\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_meal_records_Overtime1900Days", "\"Overtime1900Days\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_meal_records_MealAllowancePerQualifiedDay", "\"MealAllowancePerQualifiedDay\" >= 0");
            table.HasCheckConstraint("CK_payroll_allowance_meal_records_MealAllowanceAmount", "\"MealAllowanceAmount\" >= 0");
        });

        builder.HasKey(x => x.PayrollAllowanceSummaryRecordId);
        builder.Property(x => x.PayrollAllowanceSummaryRecordId).ValueGeneratedNever();
        builder.Property(x => x.QualifiedMealDays).IsRequired();
        builder.Property(x => x.Overtime1900Days).IsRequired();
        builder.Property(x => x.MealAllowancePerQualifiedDay).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.MealAllowanceAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.RuleCode).HasMaxLength(64).HasDefaultValue("qualified-meal").IsRequired();
        builder.Property(x => x.RuleVersion).HasMaxLength(64);
        builder.Property(x => x.Note).HasColumnType("text");
        builder.Property(x => x.IsLocked).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CalculatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(128);
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp without time zone").IsConcurrencyToken();
        builder.Property(x => x.UpdatedBy).HasMaxLength(128);
        builder.HasIndex(x => x.IsLocked).HasDatabaseName("IX_payroll_allowance_meal_records_IsLocked");
        builder.HasOne<PayrollAllowanceSummaryRecordRow>()
            .WithOne()
            .HasForeignKey<PayrollMealAllowanceRecordRow>(x => x.PayrollAllowanceSummaryRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
