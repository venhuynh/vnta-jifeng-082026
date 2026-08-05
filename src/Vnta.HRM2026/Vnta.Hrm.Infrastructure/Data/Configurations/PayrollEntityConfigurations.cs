using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Integrations.Payroll;
using Vnta.Hrm.Infrastructure.TinhLuong.LuongCanBan;
namespace Vnta.Hrm.Infrastructure.Data.Configurations;

public sealed class BasicSalaryRecordRowConfiguration : IEntityTypeConfiguration<BasicSalaryRecordRow>
{
    public void Configure(EntityTypeBuilder<BasicSalaryRecordRow> builder)
    {
        builder.ToTable(
            "payroll_basic_salary_records",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_payroll_basic_salary_records_PayrollMonth",
                    "\"PayrollMonth\" BETWEEN 1 AND 12");
                table.HasCheckConstraint(
                    "CK_payroll_basic_salary_records_PayrollYear",
                    "\"PayrollYear\" BETWEEN 1 AND 9999");
                table.HasCheckConstraint(
                    "CK_payroll_basic_salary_records_BasicSalary",
                    "\"BasicSalary\" > 0");
                table.HasCheckConstraint(
                    "CK_payroll_basic_salary_records_StandardWorkingDays",
                    "\"StandardWorkingDays\" > 0");
                table.HasCheckConstraint(
                    "CK_payroll_basic_salary_records_DailySalary",
                    "\"DailySalary\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_basic_salary_records_HourlySalary",
                    "\"HourlySalary\" >= 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.BasicSalary)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.StandardWorkingDays)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.DailySalary)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.HourlySalary)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_payroll_basic_salary_records_EmployeeId");

        builder.HasIndex(x => new { x.PayrollYear, x.PayrollMonth })
            .HasDatabaseName("IX_payroll_basic_salary_records_PayrollYear_PayrollMonth");

        builder.HasIndex(x => new { x.EmployeeId, x.PayrollYear, x.PayrollMonth })
            .IsUnique()
            .HasDatabaseName("IX_payroll_basic_salary_records_EmployeeId_PayrollYear_PayrollMonth");

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Ánh xạ snapshot công tổng hợp theo nhân viên và kỳ lương.
/// </summary>
public sealed class PayrollMonthlyWorkInputRowConfiguration : IEntityTypeConfiguration<PayrollMonthlyWorkInputRow>
{
    public void Configure(EntityTypeBuilder<PayrollMonthlyWorkInputRow> builder)
    {
        builder.ToTable(
            "payroll_monthly_work_inputs",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_payroll_monthly_work_inputs_PayrollMonth",
                    "\"PayrollMonth\" BETWEEN 1 AND 12");
                table.HasCheckConstraint(
                    "CK_payroll_monthly_work_inputs_PayrollYear",
                    "\"PayrollYear\" BETWEEN 1 AND 9999");
                table.HasCheckConstraint(
                    "CK_payroll_monthly_work_inputs_AdministrativeWorkDays",
                    "\"AdministrativeWorkDays\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_monthly_work_inputs_LateEarlyLeaveMinutes",
                    "\"LateEarlyLeaveMinutes\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_monthly_work_inputs_OvertimeMinutes15",
                    "\"OvertimeMinutes15\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_monthly_work_inputs_OvertimeMinutes20",
                    "\"OvertimeMinutes20\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_monthly_work_inputs_OvertimeMinutes30",
                    "\"OvertimeMinutes30\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_monthly_work_inputs_PayrollWorkDays",
                    "\"PayrollWorkDays\" >= 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PayrollYear)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.PayrollMonth)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.AdministrativeWorkDays)
            .HasPrecision(9, 4)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.LateEarlyLeaveMinutes)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.OvertimeMinutes15)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.OvertimeMinutes20)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.OvertimeMinutes30)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.PayrollWorkDays)
            .HasPrecision(9, 4)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.IsLocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.EmployeeId, x.PayrollYear, x.PayrollMonth })
            .IsUnique()
            .HasDatabaseName("UX_payroll_monthly_work_inputs_EmployeeId_PayrollYear_PayrollMonth");

        builder.HasIndex(x => new { x.PayrollYear, x.PayrollMonth })
            .HasDatabaseName("IX_payroll_monthly_work_inputs_PayrollYear_PayrollMonth");

        builder.HasIndex(x => x.IsLocked)
            .HasDatabaseName("IX_payroll_monthly_work_inputs_IsLocked");

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
/// <summary>
/// Ánh xạ bảng snapshot tổng hợp phụ cấp, bao gồm ràng buộc dữ liệu tiền tệ,
/// duy nhất theo nhân viên/kỳ và token đồng thời cho các thao tác ghi.
/// </summary>
public sealed class PayrollAllowanceSummaryRecordRowConfiguration : IEntityTypeConfiguration<PayrollAllowanceSummaryRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollAllowanceSummaryRecordRow> builder)
    {
        builder.ToTable(
            "payroll_allowance_summary_records",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_payroll_allowance_summary_records_PayrollMonth",
                    """
                    "PayrollMonth" >= 1 AND "PayrollMonth" <= 12
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_summary_records_PayrollYear",
                    """
                    "PayrollYear" >= 1 AND "PayrollYear" <= 9999
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_summary_records_ResponsibilityAllowanceAmount",
                    """
                    "ResponsibilityAllowanceAmount" >= 0
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_summary_records_ResponsibilityOtherAllowanceAmount",
                    """
                    "ResponsibilityOtherAllowanceAmount" >= 0
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_summary_records_SeniorityAllowanceAmount",
                    """
                    "SeniorityAllowanceAmount" >= 0
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_summary_records_AttendanceAllowanceAmount",
                    """
                    "AttendanceAllowanceAmount" >= 0
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_summary_records_MealAllowanceAmount",
                    """
                    "MealAllowanceAmount" >= 0
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_summary_records_HazardAllowanceAmount",
                    """
                    "HazardAllowanceAmount" >= 0
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_summary_records_OtherAllowanceAmount",
                    """
                    "OtherAllowanceAmount" >= 0
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_summary_records_LeaveHolidayAllowanceAmount",
                    """
                    "LeaveHolidayAllowanceAmount" >= 0
                    """);
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PayrollMonth)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.PayrollYear)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.ResponsibilityAllowanceAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.ResponsibilityOtherAllowanceAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.SeniorityAllowanceAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.AttendanceAllowanceAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.MealAllowanceAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.HazardAllowanceAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.OtherAllowanceAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.LeaveHolidayAllowanceAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.IsLocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasColumnType("text");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsConcurrencyToken();

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(128);

        // Mỗi nhân viên chỉ có một snapshot trong một kỳ lương.
        builder.HasIndex(x => new { x.EmployeeId, x.PayrollYear, x.PayrollMonth })
            .IsUnique()
            .HasDatabaseName("UX_payroll_allowance_summary_records_EmployeeId_PayrollYear_PayrollMonth");

        builder.HasIndex(x => new { x.PayrollYear, x.PayrollMonth })
            .HasDatabaseName("IX_payroll_allowance_summary_records_PayrollYear_PayrollMonth");

        builder.HasIndex(x => x.IsLocked)
            .HasDatabaseName("IX_payroll_allowance_summary_records_IsLocked");

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PayrollDeductionSummaryRecordRowConfiguration : IEntityTypeConfiguration<PayrollDeductionSummaryRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollDeductionSummaryRecordRow> builder)
    {
        builder.ToTable(
            "payroll_decuction_summary_records",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_payroll_decuction_summary_records_PayrollMonth",
                    """
                    "PayrollMonth" >= 1 AND "PayrollMonth" <= 12
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_decuction_summary_records_PayrollYear",
                    """
                    "PayrollYear" >= 1 AND "PayrollYear" <= 9999
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_decuction_summary_records_SocialInsuranceDeductionAmount",
                    "\"SocialInsuranceDeductionAmount\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_decuction_summary_records_PersonalIncomeTaxDeductionAmount",
                    "\"PersonalIncomeTaxDeductionAmount\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_decuction_summary_records_UnionFeeDeductionAmount",
                    "\"UnionFeeDeductionAmount\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_decuction_summary_records_AdvanceDeductionAmount",
                    "\"AdvanceDeductionAmount\" >= 0");
                table.HasCheckConstraint(
                    "CK_payroll_decuction_summary_records_OtherDeductionAmount",
                    "\"OtherDeductionAmount\" >= 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PayrollMonth)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.PayrollYear)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.SocialInsuranceDeductionAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.PersonalIncomeTaxDeductionAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.UnionFeeDeductionAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.AdvanceDeductionAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.OtherDeductionAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.IsLocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasColumnType("text");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(128)
            .IsRequired();

        // Giá trị này được client gửi lại khi sửa/xóa/khóa để phát hiện thay đổi đồng thời.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsConcurrencyToken();

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(128);

        builder.HasIndex(x => new { x.EmployeeId, x.PayrollYear, x.PayrollMonth })
            .IsUnique()
            .HasDatabaseName("UX_payroll_decuction_summary_records_EmployeeId_PayrollYear_PayrollMonth");

        builder.HasIndex(x => new { x.PayrollYear, x.PayrollMonth })
            .HasDatabaseName("IX_payroll_decuction_summary_records_PayrollYear_PayrollMonth");

        builder.HasIndex(x => x.IsLocked)
            .HasDatabaseName("IX_payroll_decuction_summary_records_IsLocked");

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PayrollDeductionTaxRecordRowConfiguration
    : IEntityTypeConfiguration<PayrollDeductionTaxRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollDeductionTaxRecordRow> builder) =>
        PayrollDeductionRecordConfiguration.ConfigureAmountRecord(
            builder,
            "payroll_decuction_tax_records",
            "CK_payroll_decuction_tax_records_DeductionAmount",
            "IX_payroll_decuction_tax_records_IsLocked");
}

public sealed class PayrollDeductionUnionFeeRecordRowConfiguration
    : IEntityTypeConfiguration<PayrollDeductionUnionFeeRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollDeductionUnionFeeRecordRow> builder) =>
        PayrollDeductionRecordConfiguration.ConfigureAmountRecord(
            builder,
            "payroll_decuction_union_fee_records",
            "CK_payroll_decuction_union_fee_records_DeductionAmount",
            "IX_payroll_decuction_union_fee_records_IsLocked");
}

public sealed class PayrollDeductionAdvanceRecordRowConfiguration
    : IEntityTypeConfiguration<PayrollDeductionAdvanceRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollDeductionAdvanceRecordRow> builder) =>
        PayrollDeductionRecordConfiguration.ConfigureAmountRecord(
            builder,
            "payroll_decuction_advance_records",
            "CK_payroll_decuction_advance_records_DeductionAmount",
            "IX_payroll_decuction_advance_records_IsLocked");
}

public sealed class PayrollDeductionOtherRecordRowConfiguration
    : IEntityTypeConfiguration<PayrollDeductionOtherRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollDeductionOtherRecordRow> builder)
    {
        PayrollDeductionRecordConfiguration.ConfigureAmountRecord(
            builder,
            "payroll_decuction_other_records",
            "CK_payroll_decuction_other_records_DeductionAmount",
            "IX_payroll_decuction_other_records_IsLocked");

        builder.Property(x => x.Description)
            .HasColumnType("text");

        builder.Property(x => x.Note)
            .HasColumnType("text");
    }
}

file static class PayrollDeductionRecordConfiguration
{
    public static void ConfigureAmountRecord<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        string tableName,
        string amountCheckConstraintName,
        string lockedIndexName)
        where TEntity : class
    {
        builder.ToTable(tableName, table =>
            table.HasCheckConstraint(amountCheckConstraintName, "\"DeductionAmount\" >= 0"));

        builder.HasKey("PayrollDeductionSummaryRecordId");
        builder.Property<Guid>("PayrollDeductionSummaryRecordId").ValueGeneratedNever();
        builder.Property<decimal>("DeductionAmount").HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property<bool>("IsLocked").HasDefaultValue(false).IsRequired();
        builder.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp without time zone").IsRequired();
        builder.Property<DateTime?>("UpdatedAtUtc").HasColumnType("timestamp without time zone");
        builder.HasIndex("IsLocked").HasDatabaseName(lockedIndexName);
        builder.HasOne<PayrollDeductionSummaryRecordRow>()
            .WithOne()
            .HasForeignKey<TEntity>("PayrollDeductionSummaryRecordId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PayrollAllowanceOtherResponsibilityRecordRowConfiguration
    : IEntityTypeConfiguration<PayrollAllowanceOtherResponsibilityRecordRow>
{
    public void Configure(EntityTypeBuilder<PayrollAllowanceOtherResponsibilityRecordRow> builder)
    {
        builder.ToTable(
            "payroll_allowance_other_responsibility_records",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_payroll_allowance_other_responsibility_records_AllowanceWorkdayCount",
                    """
                    "AllowanceWorkdayCount" >= 0
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_other_responsibility_records_StandardResponsibilityAllowanceAmount",
                    """
                    "StandardResponsibilityAllowanceAmount" >= 0
                    """);
                table.HasCheckConstraint(
                    "CK_payroll_allowance_other_responsibility_records_ActualResponsibilityAllowanceAmount",
                    """
                    "ActualResponsibilityAllowanceAmount" >= 0
                    """);
            });

        builder.HasKey(x => x.PayrollAllowanceSummaryRecordId);

        builder.Property(x => x.PayrollAllowanceSummaryRecordId)
            .ValueGeneratedNever();

        builder.Property(x => x.AllowanceWorkdayCount)
            .HasPrecision(10, 4)
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

        builder.Property(x => x.Note)
            .HasColumnType("text");

        builder.Property(x => x.IsLocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.RefreshedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.RefreshedBy)
            .HasMaxLength(128);

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

        builder.HasIndex(x => x.IsLocked)
            .HasDatabaseName("IX_payroll_allowance_other_responsibility_records_IsLocked");

        builder.HasOne<PayrollAllowanceSummaryRecordRow>()
            .WithOne()
            .HasForeignKey<PayrollAllowanceOtherResponsibilityRecordRow>(x => x.PayrollAllowanceSummaryRecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
