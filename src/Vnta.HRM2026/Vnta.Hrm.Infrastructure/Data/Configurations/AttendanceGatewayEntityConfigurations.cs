using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.Data.Configurations;

public sealed class AdmsDeviceCommandRowConfiguration : IEntityTypeConfiguration<AdmsDeviceCommandRow>
{
    public void Configure(EntityTypeBuilder<AdmsDeviceCommandRow> builder)
    {
        builder.ToTable("device_cmd");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.DeviceSn)
            .HasMaxLength(50);

        builder.Property(x => x.Content)
            .HasColumnType("text");

        builder.Property(x => x.CommitTime)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.TransTime)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.ResponseTime)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.ReturnValue)
            .HasColumnType("text");

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.DeviceSn)
            .HasDatabaseName("ix_device_cmd_device_sn");

        builder.HasIndex(x => x.CommitTime)
            .HasDatabaseName("ix_device_cmd_commit_time");

        builder.HasIndex(x => x.ResponseTime)
            .HasDatabaseName("ix_device_cmd_response_time");
    }
}

public sealed class AttendanceLogRowConfiguration : IEntityTypeConfiguration<AttendanceLogRow>
{
    public void Configure(EntityTypeBuilder<AttendanceLogRow> builder)
    {
        builder.ToTable("attendance_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AttTime)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.Status)
            .HasMaxLength(10);

        builder.Property(x => x.Verify)
            .HasMaxLength(10);

        builder.Property(x => x.WorkCode)
            .HasMaxLength(50);

        builder.Property(x => x.Reserved1)
            .HasMaxLength(50);

        builder.Property(x => x.Reserved2)
            .HasMaxLength(50);

        builder.Property(x => x.DeviceCode)
            .HasMaxLength(50);

        builder.Property(x => x.Temperature)
            .HasMaxLength(50);

        builder.Property(x => x.DedupKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.UpdateTime)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.AttTime)
            .HasDatabaseName("IX_attendance_logs_AttTime");

        builder.HasIndex(x => x.DeviceId)
            .HasDatabaseName("IX_attendance_logs_DeviceId");

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_attendance_logs_EmployeeId");

        builder.HasIndex(x => x.UpdateTime)
            .HasDatabaseName("IX_attendance_logs_UpdateTime");

        builder.HasIndex(x => x.DedupKey)
            .IsUnique()
            .HasDatabaseName("IX_attendance_logs_DedupKey");

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceBiometricDataRowConfiguration : IEntityTypeConfiguration<AttendanceBiometricDataRow>
{
    public void Configure(EntityTypeBuilder<AttendanceBiometricDataRow> builder)
    {
        builder.ToTable("biometric_data");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.LastUpdated)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.CardNumber)
            .HasMaxLength(255);

        builder.Property(x => x.Password)
            .HasMaxLength(255);

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_biometric_data_EmployeeId");

        builder.HasIndex(x => x.LastUpdated)
            .HasDatabaseName("IX_biometric_data_LastUpdated");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.BiometricDataRows)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceDeviceUserProfileRowConfiguration : IEntityTypeConfiguration<AttendanceDeviceUserProfileRow>
{
    public void Configure(EntityTypeBuilder<AttendanceDeviceUserProfileRow> builder)
    {
        builder.ToTable("device_user_profiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.EmployeeCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DeviceSn)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasMaxLength(255);

        builder.Property(x => x.Password)
            .HasMaxLength(255);

        builder.Property(x => x.CardNumber)
            .HasMaxLength(255);

        builder.Property(x => x.GroupCode)
            .HasMaxLength(50);

        builder.Property(x => x.TimeZoneCode)
            .HasMaxLength(100);

        builder.Property(x => x.PrivilegeCode)
            .HasMaxLength(50);

        builder.Property(x => x.VerifyMode)
            .HasMaxLength(50);

        builder.Property(x => x.ViceCard)
            .HasMaxLength(255);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_device_user_profiles_EmployeeId");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.DeviceUserProfiles)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceFingerprintTemplateRowConfiguration : IEntityTypeConfiguration<AttendanceFingerprintTemplateRow>
{
    public void Configure(EntityTypeBuilder<AttendanceFingerprintTemplateRow> builder)
    {
        builder.ToTable("fingerprint_templates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.EmployeeCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DeviceSn)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Fid)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Valid)
            .HasMaxLength(50);

        builder.Property(x => x.TemplateData)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.MajorVersion)
            .HasMaxLength(50);

        builder.Property(x => x.MinorVersion)
            .HasMaxLength(50);

        builder.Property(x => x.Duress)
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_fingerprint_templates_EmployeeId");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.FingerprintTemplates)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceBioPhotoRowConfiguration : IEntityTypeConfiguration<AttendanceBioPhotoRow>
{
    public void Configure(EntityTypeBuilder<AttendanceBioPhotoRow> builder)
    {
        builder.ToTable("bio_photos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.DeviceSn)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(50);

        builder.Property(x => x.Content)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_bio_photos_EmployeeId");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.BioPhotos)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceUserPictureRowConfiguration : IEntityTypeConfiguration<AttendanceUserPictureRow>
{
    public void Configure(EntityTypeBuilder<AttendanceUserPictureRow> builder)
    {
        builder.ToTable("user_pictures");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.DeviceSn)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_user_pictures_EmployeeId");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.UserPictures)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceDailySummaryRowConfiguration : IEntityTypeConfiguration<AttendanceDailySummaryRow>
{
    public void Configure(EntityTypeBuilder<AttendanceDailySummaryRow> builder)
    {
        builder.ToTable("attendance_daily_summaries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WorkDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.PunchCount)
            .IsRequired();

        builder.Property(x => x.PunchMomentsText)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.FirstPunchTime)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.LastPunchTime)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_attendance_daily_summaries_EmployeeId");

        builder.HasIndex(x => x.WorkDate)
            .HasDatabaseName("IX_attendance_daily_summaries_WorkDate");

        builder.HasIndex(x => new { x.EmployeeId, x.WorkDate })
            .IsUnique()
            .HasDatabaseName("IX_attendance_daily_summaries_EmployeeId_WorkDate");

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceWorkdaySummaryRowConfiguration : IEntityTypeConfiguration<AttendanceWorkdaySummaryRow>
{
    public void Configure(EntityTypeBuilder<AttendanceWorkdaySummaryRow> builder)
    {
        builder.ToTable(
            "attendance_workday_summaries",
            table => table.HasCheckConstraint(
                "CK_attendance_workday_summaries_DayType",
                $"""
                "DayType" IN ('{AttendanceWorkCalendarDayTypes.Regular}', '{AttendanceWorkCalendarDayTypes.DayOff}', '{AttendanceWorkCalendarDayTypes.Holiday}')
                """));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WorkDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.DayType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ScheduledStartAt)
            .HasMaxLength(20);

        builder.Property(x => x.ScheduledEndAt)
            .HasMaxLength(20);

        builder.Property(x => x.CheckInAt)
            .HasMaxLength(20);

        builder.Property(x => x.CheckOutAt)
            .HasMaxLength(20);

        builder.Property(x => x.LateMinutes)
            .IsRequired();

        builder.Property(x => x.EarlyLeaveMinutes)
            .IsRequired();

        builder.Property(x => x.ComputedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.Note)
            .HasColumnType("text");

        builder.Property(x => x.IsLocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.OvertimeMinutes)
            .IsRequired();

        builder.Property(x => x.OvertimeMinutes15)
            .IsRequired();

        builder.Property(x => x.OvertimeMinutes20)
            .IsRequired();

        builder.Property(x => x.OvertimeMinutes30)
            .IsRequired();

        builder.Property(x => x.CheckInForOT15)
            .HasMaxLength(20);

        builder.Property(x => x.IsRegisterForOT)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.RequireDocument)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_attendance_workday_summaries_EmployeeId");

        builder.HasIndex(x => x.CodeKetQuaTinhCongId)
            .HasDatabaseName("IX_attendance_workday_summaries_CodeKetQuaTinhCongId");

        builder.HasIndex(x => x.WorkDate)
            .HasDatabaseName("IX_attendance_workday_summaries_WorkDate");

        builder.HasIndex(x => new { x.EmployeeId, x.WorkDate })
            .IsUnique()
            .HasDatabaseName("IX_attendance_workday_summaries_EmployeeId_WorkDate");

        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AttendanceShiftRow>()
            .WithMany()
            .HasForeignKey(x => x.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AttendanceStatusCodeRow>()
            .WithMany()
            .HasForeignKey(x => x.CodeKetQuaTinhCongId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceStatusCodeRowConfiguration : IEntityTypeConfiguration<AttendanceStatusCodeRow>
{
    public void Configure(EntityTypeBuilder<AttendanceStatusCodeRow> builder)
    {
        builder.ToTable("attendance_status_codes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Kind)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CongTangCa)
            .HasColumnName("Cong_Tang_Ca")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CongHanhChinh)
            .HasColumnName("Cong_Hanh_Chinh")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.PhuCapTrachNhiemTinhNangSuat)
            .HasColumnName("Phu_Cap_Trach_Nhiem_Tinh_Nang_Suat")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.PhuCapDocHai)
            .HasColumnName("Phu_Cap_Doc_Hai")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.PhuCapTrachNhiemKhac)
            .HasColumnName("Phu_Cap_Trach_Nhiem_Khac")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.PhuCapPhepLe)
            .HasColumnName("Phu_Cap_Phep_Le")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.PhuCapTrachNhiemKhongTinhNangSuat)
            .HasColumnName("Phu_Cap_Trach_Nhiem_Khong_Tinh_Nang_Suat")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.PhuCapChuyenCan)
            .HasColumnName("Phu_Cap_Chuyen_Can")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.PhuCapThamNien)
            .HasColumnName("Phu_Cap_Tham_Nien")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.KhauTruTamUng)
            .HasColumnName("Khau_Tru_Tam_Ung")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");
    }
}

public sealed class AttendanceWorkCalendarDayRowConfiguration : IEntityTypeConfiguration<AttendanceWorkCalendarDayRow>
{
    public void Configure(EntityTypeBuilder<AttendanceWorkCalendarDayRow> builder)
    {
        builder.ToTable(
            "attendance_work_calendar_days",
            table => table.HasCheckConstraint(
                "CK_attendance_work_calendar_days_DayType",
                $"""
                "DayType" IN ({(short)AttendanceWorkCalendarDayType.DayOff}, {(short)AttendanceWorkCalendarDayType.Holiday})
                """));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property<uint>("xmin")
            .IsRowVersion();

        builder.Property(x => x.WorkDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.DayType)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200);

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.WorkDate)
            .IsUnique()
            .HasDatabaseName("IX_attendance_work_calendar_days_WorkDate");
    }
}

public sealed class AttendanceGatewayEmployeeRowConfiguration : IEntityTypeConfiguration<AttendanceGatewayEmployeeRow>
{
    public void Configure(EntityTypeBuilder<AttendanceGatewayEmployeeRow> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmployeeCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(x => x.Avatar)
            .HasColumnType("text");

        builder.Property(x => x.HireDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.SeniorityStartDate)
            .HasColumnType("date");

        builder.Property(x => x.ResignedDate)
            .HasColumnType("date");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.DeletedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.DepartmentId)
            .HasDatabaseName("IX_employees_DepartmentId");

        builder.HasIndex(x => x.PositionId)
            .HasDatabaseName("IX_employees_PositionId");

        builder.HasIndex(x => x.IsDeleted)
            .HasDatabaseName("IX_employees_IsDeleted");

        builder.HasIndex(x => x.EmployeeCode)
            .IsUnique()
            .HasDatabaseName("ux_employees_employee_code_active")
            .HasFilter("""
                "IsDeleted" = FALSE
                AND "EmployeeCode" IS NOT NULL
                AND btrim("EmployeeCode") <> ''
                """);

        builder.HasOne<AttendanceDepartmentRow>()
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AttendanceGatewayPositionRow>()
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceDepartmentRowConfiguration : IEntityTypeConfiguration<AttendanceDepartmentRow>
{
    public void Configure(EntityTypeBuilder<AttendanceDepartmentRow> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CenterName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DepartmentOrWorkshopName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TeamName)
            .HasMaxLength(200);

        builder.Property(x => x.GroupName)
            .HasMaxLength(200);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");
    }
}

public sealed class AttendanceDeviceRowConfiguration : IEntityTypeConfiguration<AttendanceDeviceRow>
{
    public void Configure(EntityTypeBuilder<AttendanceDeviceRow> builder)
    {
        builder.ToTable("devices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.SerialNumber)
            .HasMaxLength(50);

        builder.HasIndex(x => x.SerialNumber)
            .IsUnique()
            .HasDatabaseName("ux_devices_serial_number_not_empty")
            .HasFilter("""
                "SerialNumber" IS NOT NULL AND btrim("SerialNumber") <> ''
                """);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(50);

        builder.Property(x => x.MacAddress)
            .HasMaxLength(50);

        builder.Property(x => x.Location)
            .HasMaxLength(500);

        builder.Property(x => x.ActivationCode)
            .HasMaxLength(200);

        builder.Property(x => x.VendorName)
            .HasMaxLength(100);

        builder.Property(x => x.DeviceModel)
            .HasMaxLength(200);

        builder.Property(x => x.FirmwareVersion)
            .HasMaxLength(100);

        builder.Property(x => x.FingerprintVersion)
            .HasMaxLength(100);

        builder.Property(x => x.TimeZone)
            .HasMaxLength(50);

        builder.Property(x => x.AttendanceLogStamp)
            .HasMaxLength(100);

        builder.Property(x => x.AttendancePhotoStamp)
            .HasMaxLength(100);

        builder.Property(x => x.OperationLogStamp)
            .HasMaxLength(100);

        builder.Property(x => x.ErrorLogStamp)
            .HasMaxLength(100);

        builder.Property(x => x.TransferFlag)
            .HasMaxLength(1000);

        builder.Property(x => x.Delay)
            .HasMaxLength(100);

        builder.Property(x => x.Realtime)
            .HasMaxLength(20);

        builder.Property(x => x.TransInterval)
            .HasMaxLength(100);

        builder.Property(x => x.TransTimes)
            .HasMaxLength(100);

        builder.Property(x => x.Encrypt)
            .HasMaxLength(20);

        builder.Property(x => x.ErrorDelay)
            .HasMaxLength(100);

        builder.Property(x => x.LastRequestTime)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.IrTempDetectionFunOn)
            .HasMaxLength(20);

        builder.Property(x => x.MaskDetectionFunOn)
            .HasMaxLength(20);

        builder.Property(x => x.MultiBioDataSupport)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");
    }
}

public sealed class AttendanceGatewayPositionRowConfiguration : IEntityTypeConfiguration<AttendanceGatewayPositionRow>
{
    public void Configure(EntityTypeBuilder<AttendanceGatewayPositionRow> builder)
    {
        builder.ToTable("positions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.EmployeeCount)
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");
    }
}

public sealed class AttendanceShiftRowConfiguration : IEntityTypeConfiguration<AttendanceShiftRow>
{
    public void Configure(EntityTypeBuilder<AttendanceShiftRow> builder)
    {
        builder.ToTable("shifts");

        builder.HasKey(x => x.Id);

        builder.Property<uint>("xmin")
            .IsRowVersion();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ShortName)
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.DepartmentGroup)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.StartTime)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(x => x.BreakStartTime)
            .HasMaxLength(5);

        builder.Property(x => x.BreakEndTime)
            .HasMaxLength(5);

        builder.Property(x => x.ColorHex)
            .HasMaxLength(7);

        builder.Property(x => x.WorkingDays)
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("IX_shifts_Code");
    }
}

public sealed class ShiftSchedulingSettingRowConfiguration : IEntityTypeConfiguration<ShiftSchedulingSettingRow>
{
    public void Configure(EntityTypeBuilder<ShiftSchedulingSettingRow> builder)
    {
        builder.ToTable("shift_scheduling_settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ShiftId);

        builder.Property(x => x.Value)
            .HasMaxLength(500);

        builder.Property(x => x.EffectiveFromDate)
            .HasColumnType("date");

        builder.Property(x => x.EffectiveToDate)
            .HasColumnType("date");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.ShiftId)
            .HasDatabaseName("IX_shift_scheduling_settings_ShiftId");

        builder.HasOne<AttendanceShiftRow>()
            .WithMany()
            .HasForeignKey(x => x.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
