using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vnta.AttendanceGateway.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CenterName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DepartmentOrWorkshopName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GroupName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "device_cmd",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceSn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    CommitTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TransTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ResponseTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReturnValue = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_cmd", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MacAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Port = table.Column<int>(type: "integer", nullable: true),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActivationCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VendorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceModel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FirmwareVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FingerprintVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsInUse = table.Column<bool>(type: "boolean", nullable: false),
                    UserCount = table.Column<int>(type: "integer", nullable: false),
                    AttendanceLogCount = table.Column<int>(type: "integer", nullable: false),
                    FingerprintCount = table.Column<int>(type: "integer", nullable: false),
                    AttendanceLogStamp = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AttendancePhotoStamp = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OperationLogStamp = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorLogStamp = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransferFlag = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Delay = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Realtime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TransInterval = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransTimes = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Encrypt = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ErrorDelay = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Timeout = table.Column<int>(type: "integer", nullable: true),
                    SyncTime = table.Column<int>(type: "integer", nullable: false),
                    LastRequestTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IrTempDetectionFunOn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MaskDetectionFunOn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MultiBioDataSupport = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "errorlog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ErrCode = table.Column<string>(type: "text", nullable: true),
                    ErrMsg = table.Column<string>(type: "text", nullable: true),
                    DataOrigin = table.Column<string>(type: "text", nullable: true),
                    CmdId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Additional = table.Column<string>(type: "text", nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_errorlog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oplog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Operator = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OpTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OpType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    User = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Obj1 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Obj2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Obj3 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Obj4 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oplog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbound_attendance_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceSn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TapTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    VerificationMode = table.Column<int>(type: "integer", nullable: false),
                    InOutMode = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound_attendance_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbound_system_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceSn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConnectionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound_system_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance_daily_summaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PunchCount = table.Column<int>(type: "integer", nullable: false),
                    PunchMomentsText = table.Column<string>(type: "text", nullable: false),
                    FirstPunchTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastPunchTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_daily_summaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_daily_summaries_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Verify = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    WorkCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Reserved1 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Reserved2 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DeviceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MaskFlag = table.Column<int>(type: "integer", nullable: true),
                    Temperature = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DedupKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_logs_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bio_photos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceSn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Size = table.Column<int>(type: "integer", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bio_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bio_photos_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "biodata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceSn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Pin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BioNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BioIndex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Valid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Duress = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BioType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MajorVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MinorVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TemplateData = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_biodata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_biodata_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_user_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DeviceSn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Password = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CardNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GroupCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TimeZoneCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PrivilegeCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VerifyMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ViceCard = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_user_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_device_user_profiles_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "face_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceSn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Fid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: true),
                    Valid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TemplateData = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_face_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_face_templates_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fingerprint_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DeviceSn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Fid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: true),
                    Valid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TemplateData = table.Column<string>(type: "text", nullable: false),
                    MajorVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MinorVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Duress = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fingerprint_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fingerprint_templates_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fvein_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceSn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Fid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Index = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: true),
                    Valid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TemplateData = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Duress = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fvein_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fvein_templates_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_pictures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceSn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_pictures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_pictures_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_daily_summaries_EmployeeId",
                table: "attendance_daily_summaries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_daily_summaries_EmployeeId_WorkDate",
                table: "attendance_daily_summaries",
                columns: new[] { "EmployeeId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_daily_summaries_WorkDate",
                table: "attendance_daily_summaries",
                column: "WorkDate");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_logs_AttTime",
                table: "attendance_logs",
                column: "AttTime");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_logs_DedupKey",
                table: "attendance_logs",
                column: "DedupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_logs_DeviceId",
                table: "attendance_logs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_logs_EmployeeId",
                table: "attendance_logs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_logs_UpdateTime",
                table: "attendance_logs",
                column: "UpdateTime");

            migrationBuilder.CreateIndex(
                name: "IX_bio_photos_EmployeeId",
                table: "bio_photos",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_biodata_EmployeeId",
                table: "biodata",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_biodata_EmployeeId_BioNo_BioIndex_BioType",
                table: "biodata",
                columns: new[] { "EmployeeId", "BioNo", "BioIndex", "BioType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_cmd_commit_time",
                table: "device_cmd",
                column: "CommitTime");

            migrationBuilder.CreateIndex(
                name: "ix_device_cmd_device_sn",
                table: "device_cmd",
                column: "DeviceSn");

            migrationBuilder.CreateIndex(
                name: "ix_device_cmd_response_time",
                table: "device_cmd",
                column: "ResponseTime");

            migrationBuilder.CreateIndex(
                name: "IX_device_user_profiles_EmployeeCode",
                table: "device_user_profiles",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_user_profiles_EmployeeId",
                table: "device_user_profiles",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_DepartmentId",
                table: "employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_PositionId",
                table: "employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_errorlog_CmdId",
                table: "errorlog",
                column: "CmdId");

            migrationBuilder.CreateIndex(
                name: "IX_errorlog_DeviceId",
                table: "errorlog",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_face_templates_EmployeeId",
                table: "face_templates",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_face_templates_EmployeeId_Fid",
                table: "face_templates",
                columns: new[] { "EmployeeId", "Fid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fingerprint_templates_EmployeeCode",
                table: "fingerprint_templates",
                column: "EmployeeCode");

            migrationBuilder.CreateIndex(
                name: "IX_fingerprint_templates_EmployeeCode_Fid",
                table: "fingerprint_templates",
                columns: new[] { "EmployeeCode", "Fid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fingerprint_templates_EmployeeId",
                table: "fingerprint_templates",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_fvein_templates_EmployeeId",
                table: "fvein_templates",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_fvein_templates_EmployeeId_Fid_Index",
                table: "fvein_templates",
                columns: new[] { "EmployeeId", "Fid", "Index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oplog_DeviceId",
                table: "oplog",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_oplog_OpTime",
                table: "oplog",
                column: "OpTime");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_attendance_logs_attendance_log_id",
                table: "outbound_attendance_logs",
                column: "AttendanceLogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbound_attendance_logs_created_at",
                table: "outbound_attendance_logs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_attendance_logs_next_attempt",
                table: "outbound_attendance_logs",
                column: "NextAttemptAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_attendance_logs_status",
                table: "outbound_attendance_logs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_system_logs_created_at",
                table: "outbound_system_logs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_system_logs_next_attempt",
                table: "outbound_system_logs",
                column: "NextAttemptAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_system_logs_status",
                table: "outbound_system_logs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_pictures_EmployeeId",
                table: "user_pictures",
                column: "EmployeeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_daily_summaries");

            migrationBuilder.DropTable(
                name: "attendance_logs");

            migrationBuilder.DropTable(
                name: "bio_photos");

            migrationBuilder.DropTable(
                name: "biodata");

            migrationBuilder.DropTable(
                name: "device_cmd");

            migrationBuilder.DropTable(
                name: "device_user_profiles");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "errorlog");

            migrationBuilder.DropTable(
                name: "face_templates");

            migrationBuilder.DropTable(
                name: "fingerprint_templates");

            migrationBuilder.DropTable(
                name: "fvein_templates");

            migrationBuilder.DropTable(
                name: "oplog");

            migrationBuilder.DropTable(
                name: "outbound_attendance_logs");

            migrationBuilder.DropTable(
                name: "outbound_system_logs");

            migrationBuilder.DropTable(
                name: "user_pictures");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "positions");
        }
    }
}
