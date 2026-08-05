using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem.Persistence;

public sealed class PayrollResponsibilityAllowanceGradeRowConfiguration
    : IEntityTypeConfiguration<PayrollResponsibilityAllowanceGradeRow>
{
    public void Configure(EntityTypeBuilder<PayrollResponsibilityAllowanceGradeRow> builder)
    {
        builder.ToTable(
            "payroll_allowance_responsibility_grade",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_payroll_allowance_responsibility_grade_Month",
                    "\"Month\" BETWEEN 1 AND 12");
                table.HasCheckConstraint(
                    "CK_payroll_allowance_responsibility_grade_StandardResponsibilityAllowanceAmount",
                    "\"StandardResponsibilityAllowanceAmount\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_allowance_responsibility_grade_DisplayOrder",
                    "\"DisplayOrder\" >= 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.StandardResponsibilityAllowanceAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => new { x.Year, x.Month, x.Code })
            .IsUnique()
            .HasDatabaseName("UX_payroll_allowance_responsibility_grade_Year_Month_Code");

        builder.HasIndex(x => new { x.Year, x.Month, x.DisplayOrder, x.Code })
            .HasDatabaseName("IX_payroll_allowance_responsibility_grade_Year_Month_DisplayOrder_Code");
    }
}

public sealed class PayrollResponsibilityAllowanceGradePositionRowConfiguration
    : IEntityTypeConfiguration<PayrollResponsibilityAllowanceGradePositionRow>
{
    public void Configure(EntityTypeBuilder<PayrollResponsibilityAllowanceGradePositionRow> builder)
    {
        builder.ToTable(
            "payroll_allowance_responsibility_grade_positions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_payroll_allowance_responsibility_grade_positions_Month",
                    "\"Month\" BETWEEN 1 AND 12");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => new { x.Year, x.Month, x.PositionId })
            .IsUnique()
            .HasDatabaseName("UX_payroll_allowance_responsibility_grade_positions_Year_Month_PositionId");

        builder.HasIndex(x => new { x.Year, x.Month, x.GradeId })
            .HasDatabaseName("IX_payroll_allowance_responsibility_grade_positions_Year_Month_GradeId");

        builder.HasOne<PayrollResponsibilityAllowanceGradeRow>()
            .WithMany()
            .HasForeignKey(x => x.GradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AttendanceGatewayPositionRow>()
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PayrollResponsibilityAllowanceEmployeeAssignmentRowConfiguration
    : IEntityTypeConfiguration<PayrollResponsibilityAllowanceEmployeeAssignmentRow>
{
    public void Configure(EntityTypeBuilder<PayrollResponsibilityAllowanceEmployeeAssignmentRow> builder)
    {
        builder.ToTable("payroll_allowance_responsibility_employee_assignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PayrollAllowanceSummaryRecordId)
            .IsRequired();

        builder.Property(x => x.GradeId)
            .IsRequired(false);

        builder.Property(x => x.IsAssignGradeFromPosition)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.PayrollAllowanceSummaryRecordId)
            .IsUnique()
            .HasDatabaseName("UX_payroll_allowance_responsibility_employee_assignments_PayrollAllowanceSummaryRecordId");

        builder.HasIndex(x => x.GradeId)
            .HasDatabaseName("IX_payroll_allowance_responsibility_employee_assignments_GradeId");

        builder.HasOne<PayrollAllowanceSummaryRecordRow>()
            .WithOne()
            .HasForeignKey<PayrollResponsibilityAllowanceEmployeeAssignmentRow>(x => x.PayrollAllowanceSummaryRecordId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_payroll_allowance_responsibility_employee_assignments_summary");

        builder.HasOne<PayrollResponsibilityAllowanceGradeRow>()
            .WithMany()
            .HasForeignKey(x => x.GradeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_payroll_allowance_responsibility_employee_assignments_grade");
    }
}

public sealed class PayrollResponsibilityAllowanceAbcRowConfiguration
    : IEntityTypeConfiguration<PayrollResponsibilityAllowanceAbcRow>
{
    public void Configure(EntityTypeBuilder<PayrollResponsibilityAllowanceAbcRow> builder)
    {
        builder.ToTable(
            "payroll_allowance_responsibility_abc",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_payroll_allowance_responsibility_abc_Month",
                    "\"Month\" BETWEEN 1 AND 12");
                table.HasCheckConstraint(
                    "CK_payroll_allowance_responsibility_abc_ActualWorkDays",
                    "\"ActualWorkDays\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_allowance_responsibility_abc_StandardWorkDays",
                    "\"StandardWorkDays\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_allowance_responsibility_abc_MonthlyPerformanceBonusAmount",
                    "\"MonthlyPerformanceBonusAmount\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_allowance_responsibility_abc_StandardResponsibilityAllowanceAmount",
                    "\"StandardResponsibilityAllowanceAmount\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_allowance_responsibility_abc_ActualResponsibilityAllowanceAmount",
                    "\"ActualResponsibilityAllowanceAmount\" >= 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PayrollAllowanceSummaryRecordId)
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.Property(x => x.EmployeeCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EmployeeName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DepartmentName)
            .HasMaxLength(200);

        builder.Property(x => x.PositionName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.GradeCode)
            .HasMaxLength(50);

        builder.Property(x => x.GradeName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ActualWorkDays)
            .HasPrecision(9, 2)
            .IsRequired();

        builder.Property(x => x.StandardWorkDays)
            .HasPrecision(9, 2)
            .IsRequired();

        builder.Property(x => x.AbcRating)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.MonthlyPerformanceBonusAmount)
            .HasPrecision(9, 4)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.StandardResponsibilityAllowanceAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.ActualResponsibilityAllowanceAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.IsLocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.CalculatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.CalculatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsConcurrencyToken();

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.LockedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.LockedBy)
            .HasMaxLength(100);

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.Year, x.Month, x.PayrollAllowanceSummaryRecordId })
            .IsUnique()
            .HasDatabaseName("UX_payroll_allowance_responsibility_abc_Year_Month_PayrollAllowanceSummaryRecordId");

        builder.HasIndex(x => new { x.Year, x.Month, x.EmployeeId })
            .IsUnique()
            .HasDatabaseName("UX_payroll_allowance_responsibility_abc_Year_Month_EmployeeId");

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_payroll_allowance_responsibility_abc_EmployeeId");

        builder.HasIndex(x => new { x.Year, x.Month, x.IsLocked })
            .HasDatabaseName("IX_payroll_allowance_responsibility_abc_Year_Month_IsLocked");

        builder.HasOne<PayrollAllowanceSummaryRecordRow>()
            .WithMany()
            .HasForeignKey(x => x.PayrollAllowanceSummaryRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AttendanceGatewayPositionRow>()
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PayrollResponsibilityAllowanceGradeRow>()
            .WithMany()
            .HasForeignKey(x => x.GradeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
