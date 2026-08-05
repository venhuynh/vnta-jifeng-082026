using Vnta.AttendanceGateway.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Vnta.AttendanceGateway.Data;

public class ZktecoDbContext : DbContext
{
    public ZktecoDbContext(DbContextOptions<ZktecoDbContext> options) : base(options)
    {
    }

    public DbSet<ZktecoDevice> Devices { get; set; }

    public DbSet<ZktecoDeviceCommand> DeviceCommands { get; set; }

    public DbSet<ZktecoAttendanceLog> AttendanceLogs { get; set; }

    public DbSet<ZktecoAttendanceDailySummary> AttendanceDailySummaries { get; set; }

    public DbSet<ZktecoOpLog> OpLogs { get; set; }

    public DbSet<ZktecoErrorLog> ErrorLogs { get; set; }

    public DbSet<ZktecoEmployee> Employees { get; set; }

    public DbSet<ZktecoDepartment> Departments { get; set; }

    public DbSet<ZktecoPosition> Positions { get; set; }

    public DbSet<ZktecoDeviceUserProfile> DeviceUserProfiles { get; set; }

    public DbSet<ZktecoFingerprintTemplate> FingerprintTemplates { get; set; }

    public DbSet<ZktecoFaceTemplate> FaceTemplates { get; set; }

    public DbSet<ZktecoBioPhoto> BioPhotos { get; set; }

    public DbSet<ZktecoFveinTemplate> FveinTemplates { get; set; }

    public DbSet<ZktecoUserPicture> UserPictures { get; set; }

    public DbSet<ZktecoBioData> BioDataRecords { get; set; }

    public DbSet<ZktecoOutboundAttendanceLog> OutboundAttendanceLogs { get; set; }

    public DbSet<ZktecoOutboundSystemLog> OutboundSystemLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ZktecoDevice>(builder =>
        {
            builder.ToTable("devices");

            builder.Property(x => x.LastRequestTime)
                .HasColumnType("timestamp without time zone");

            builder.HasIndex(x => x.SerialNumber)
                .IsUnique()
                .HasDatabaseName("ux_devices_serial_number_not_empty")
                .HasFilter("""
                    "SerialNumber" IS NOT NULL AND btrim("SerialNumber") <> ''
                    """);
        });
        modelBuilder.Entity<ZktecoEmployee>(builder =>
        {
            builder.ToTable("employees");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.EmployeeCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasMaxLength(256);

            builder.Property(x => x.Avatar)
                .HasColumnType("text");

            builder.HasOne<ZktecoDepartment>()
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ZktecoPosition>()
                .WithMany()
                .HasForeignKey(x => x.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZktecoDepartment>().ToTable("departments");
        modelBuilder.Entity<ZktecoPosition>().ToTable("positions");

        modelBuilder.Entity<ZktecoAttendanceLog>(builder =>
        {
            builder.ToTable("attendance_logs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AttTime)
                .HasColumnType("timestamp without time zone");

            builder.Property(x => x.Status)
                .HasMaxLength(10);

            builder.Property(x => x.Verify)
                .HasMaxLength(10);

            builder.Property(x => x.EmployeeId)
                .HasColumnType("uuid");

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
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
                .HasColumnType("timestamp with time zone");

            builder.HasIndex(x => x.DeviceId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.AttTime);
            builder.HasIndex(x => x.UpdateTime);
            builder.HasIndex(x => x.DedupKey)
                .IsUnique();

            builder.HasOne<ZktecoEmployee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZktecoAttendanceDailySummary>(builder =>
        {
            builder.ToTable("attendance_daily_summaries");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.EmployeeId)
                .HasColumnType("uuid");

            builder.Property(x => x.WorkDate)
                .HasColumnType("date");

            builder.Property(x => x.PunchMomentsText)
                .HasColumnType("text")
                .IsRequired();

            builder.Property(x => x.FirstPunchTime)
                .HasColumnType("timestamp without time zone");

            builder.Property(x => x.LastPunchTime)
                .HasColumnType("timestamp without time zone");

            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.WorkDate);
            builder.HasIndex(x => new { x.EmployeeId, x.WorkDate })
                .IsUnique();

            builder.HasOne<ZktecoEmployee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZktecoOutboundAttendanceLog>(builder =>
        {
            builder.ToTable("outbound_attendance_logs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DeviceSn)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.EmployeeCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.TapTime)
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.LastError)
                .HasColumnType("text");

            builder.HasIndex(x => x.AttendanceLogId)
                .IsUnique()
                .HasDatabaseName("ix_outbound_attendance_logs_attendance_log_id");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("ix_outbound_attendance_logs_status");

            builder.HasIndex(x => x.NextAttemptAtUtc)
                .HasDatabaseName("ix_outbound_attendance_logs_next_attempt");

            builder.HasIndex(x => x.CreatedAtUtc)
                .HasDatabaseName("ix_outbound_attendance_logs_created_at");
        });

        modelBuilder.Entity<ZktecoOpLog>(builder =>
        {
            builder.ToTable("oplog");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .UseIdentityByDefaultColumn();

            builder.Property(x => x.Operator)
                .HasMaxLength(500);

            builder.Property(x => x.OpTime)
                .HasColumnType("timestamp without time zone");

            builder.Property(x => x.OpType)
                .HasMaxLength(500);

            builder.Property(x => x.User)
                .HasMaxLength(50);

            builder.Property(x => x.Obj1)
                .HasMaxLength(500);

            builder.Property(x => x.Obj2)
                .HasMaxLength(500);

            builder.Property(x => x.Obj3)
                .HasMaxLength(500);

            builder.Property(x => x.Obj4)
                .HasMaxLength(500);

            builder.Property(x => x.DeviceId)
                .HasMaxLength(500);

            builder.HasIndex(x => x.DeviceId);
            builder.HasIndex(x => x.OpTime);
        });

        modelBuilder.Entity<ZktecoErrorLog>(builder =>
        {
            builder.ToTable("errorlog");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .UseIdentityByDefaultColumn();

            builder.Property(x => x.CmdId)
                .HasMaxLength(100);

            builder.Property(x => x.DeviceId)
                .HasMaxLength(50);

            builder.HasIndex(x => x.DeviceId);
            builder.HasIndex(x => x.CmdId);
        });

        modelBuilder.Entity<ZktecoDeviceUserProfile>(builder =>
        {
            builder.ToTable("device_user_profiles");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
            builder.Property(x => x.DeviceSn).HasMaxLength(50).IsRequired();
            builder.Property(x => x.FullName).HasMaxLength(200);
            builder.Property(x => x.Password).HasMaxLength(100);
            builder.Property(x => x.CardNumber).HasMaxLength(100);
            builder.Property(x => x.GroupCode).HasMaxLength(50);
            builder.Property(x => x.TimeZoneCode).HasMaxLength(50);
            builder.Property(x => x.PrivilegeCode).HasMaxLength(20);
            builder.Property(x => x.VerifyMode).HasMaxLength(20);
            builder.Property(x => x.ViceCard).HasMaxLength(100);
            builder.HasIndex(x => x.EmployeeCode).IsUnique();
            builder.HasIndex(x => x.EmployeeId);
            builder.HasOne<ZktecoEmployee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZktecoFingerprintTemplate>(builder =>
        {
            builder.ToTable("fingerprint_templates");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
            builder.Property(x => x.DeviceSn).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Fid).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Valid).HasMaxLength(20);
            builder.Property(x => x.MajorVersion).HasMaxLength(20);
            builder.Property(x => x.MinorVersion).HasMaxLength(20);
            builder.Property(x => x.Duress).HasMaxLength(20);
            builder.Property(x => x.TemplateData).HasColumnType("text").IsRequired();
            builder.HasIndex(x => new { x.EmployeeCode, x.Fid }).IsUnique();
            builder.HasIndex(x => x.EmployeeCode);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasOne<ZktecoEmployee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZktecoFaceTemplate>(builder =>
        {
            builder.ToTable("face_templates");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSn).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Fid).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Valid).HasMaxLength(20);
            builder.Property(x => x.Version).HasMaxLength(20);
            builder.Property(x => x.TemplateData).HasColumnType("text").IsRequired();
            builder.HasIndex(x => new { x.EmployeeId, x.Fid }).IsUnique();
            builder.HasIndex(x => x.EmployeeId);
            builder.HasOne<ZktecoEmployee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZktecoBioPhoto>(builder =>
        {
            builder.ToTable("bio_photos");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSn).HasMaxLength(50).IsRequired();
            builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            builder.Property(x => x.Type).HasMaxLength(50);
            builder.Property(x => x.Content).HasColumnType("text").IsRequired();
            builder.HasIndex(x => x.EmployeeId).IsUnique();
            builder.HasOne<ZktecoEmployee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZktecoFveinTemplate>(builder =>
        {
            builder.ToTable("fvein_templates");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSn).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Fid).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Index).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Valid).HasMaxLength(20);
            builder.Property(x => x.Version).HasMaxLength(20);
            builder.Property(x => x.Duress).HasMaxLength(20);
            builder.Property(x => x.TemplateData).HasColumnType("text").IsRequired();
            builder.HasIndex(x => new { x.EmployeeId, x.Fid, x.Index }).IsUnique();
            builder.HasIndex(x => x.EmployeeId);
            builder.HasOne<ZktecoEmployee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZktecoUserPicture>(builder =>
        {
            builder.ToTable("user_pictures");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSn).HasMaxLength(50).IsRequired();
            builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            builder.Property(x => x.Content).HasColumnType("text").IsRequired();
            builder.HasIndex(x => x.EmployeeId).IsUnique();
            builder.HasOne<ZktecoEmployee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZktecoBioData>(builder =>
        {
            builder.ToTable("biodata");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSn).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Pin).HasMaxLength(50).IsRequired();
            builder.Property(x => x.BioNo).HasMaxLength(20);
            builder.Property(x => x.BioIndex).HasMaxLength(20);
            builder.Property(x => x.Valid).HasMaxLength(20);
            builder.Property(x => x.Duress).HasMaxLength(20);
            builder.Property(x => x.BioType).HasMaxLength(20);
            builder.Property(x => x.MajorVersion).HasMaxLength(20);
            builder.Property(x => x.MinorVersion).HasMaxLength(20);
            builder.Property(x => x.Format).HasMaxLength(20);
            builder.Property(x => x.TemplateData).HasColumnType("text").IsRequired();
            builder.HasIndex(x => new { x.EmployeeId, x.BioNo, x.BioIndex, x.BioType }).IsUnique();
            builder.HasIndex(x => x.EmployeeId);
            builder.HasOne<ZktecoEmployee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZktecoDeviceCommand>(builder =>
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
        });

        modelBuilder.Entity<ZktecoOutboundSystemLog>(builder =>
        {
            builder.ToTable("outbound_system_logs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DeviceSn)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.ConnectionId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Direction)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.EventType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Message)
                .HasColumnType("text")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.LastError)
                .HasColumnType("text");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("ix_outbound_system_logs_status");

            builder.HasIndex(x => x.NextAttemptAtUtc)
                .HasDatabaseName("ix_outbound_system_logs_next_attempt");

            builder.HasIndex(x => x.CreatedAtUtc)
                .HasDatabaseName("ix_outbound_system_logs_created_at");
        });

        ApplyVietnamDateTimeConverters(modelBuilder);
    }

    private static void ApplyVietnamDateTimeConverters(ModelBuilder modelBuilder)
    {
        var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTime>(
            value => VietnamTime.ToVietnamLocalTimestamp(value.UtcDateTime),
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), VietnamTime.VietnamOffset));

        var nullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, DateTime?>(
            value => value.HasValue ? VietnamTime.ToVietnamLocalTimestamp(value.Value.UtcDateTime) : null,
            value => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified), VietnamTime.VietnamOffset) : null);

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            value => VietnamTime.ToVietnamLocalTimestamp(value),
            value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            value => value.HasValue ? VietnamTime.ToVietnamLocalTimestamp(value.Value) : value,
            value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified) : value);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetColumnType("timestamp without time zone");
                    property.SetValueConverter(dateTimeOffsetConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetColumnType("timestamp without time zone");
                    property.SetValueConverter(nullableDateTimeOffsetConverter);
                }
                else if (property.ClrType == typeof(DateTime))
                {
                    property.SetColumnType("timestamp without time zone");
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp without time zone");
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }
}
