using Vnta.PostgresSync.Console.Configuration;
using Vnta.PostgresSync.Console.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Globalization;
using System.Text;

System.Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
System.Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:dd/MM/yyyy HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

string[] biometricSourceTables =
[
    "public.device_user_profiles",
    "public.biodata",
    "public.face_templates",
    "public.fingerprint_templates",
    "public.fvein_templates",
    "public.bio_photos",
    "public.user_pictures"
];

string[] biometricSourceDependencyTables =
[
    "public.departments",
    "public.positions",
    "public.employees",
    "public.devices"
];

string[] attendanceDailyTables =
[
    "public.attendance_logs",
    "public.attendance_daily_summaries",
    "public.attendance_workday_summaries"
];

string[] attendanceDependencyTables =
[
    "public.departments",
    "public.positions",
    "public.employees",
    "public.shifts",
    "public.devices",
    "public.attendance_status_codes"
];

string[] familyDeductionTables =
[
    "public.payroll_employee_tax_dependents"
];

string[] familyDeductionDependencyTables =
[
    "public.departments",
    "public.positions",
    "public.employees"
];

try
{
    var builder = Host.CreateApplicationBuilder(args);
    ConfigureAppConfiguration(builder);

    var logsPath = ResolveLogPath(
        builder.Configuration,
        builder.Environment,
        "Logs/jifeng-postgres-sync");
    Directory.CreateDirectory(logsPath);

    var retainedFileCountLimit = builder.Configuration.GetValue("Serilog:RetainedFileCountLimit", 14);
    var fileSizeLimitBytes = builder.Configuration.GetValue<long?>("Serilog:FileSizeLimitBytes") ?? 104_857_600;

    builder.Services.AddSerilog((services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Vnta.PostgresSync.Console")
            .Enrich.WithProperty("Service", "VNTA Postgres Sync Console")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:dd/MM/yyyy HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.Async(a => a.File(
                formatter: new CompactJsonFormatter(),
                path: Path.Combine(logsPath, "application-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedFileCountLimit,
                fileSizeLimitBytes: fileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                shared: true))
            .WriteTo.Async(a => a.File(
                formatter: new CompactJsonFormatter(),
                path: Path.Combine(logsPath, "error-.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                retainedFileCountLimit: retainedFileCountLimit,
                fileSizeLimitBytes: fileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                shared: true));
    });

    builder.Services.Configure<PostgresSyncOptions>(
        builder.Configuration.GetSection(PostgresSyncOptions.SectionName));
    builder.Services.AddSingleton<SchemaInspectionService>();
    builder.Services.AddSingleton<PostgresSyncRunner>();

    var host = builder.Build();

    var command = CommandModeResolver.Resolve(args);
    await using (var scope = host.Services.CreateAsyncScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Startup");
        logger.LogInformation("Running PostgreSQL sync console in mode {Mode}.", command);
    }

    if (command == CommandMode.InteractiveMenu)
    {
        await RunInteractiveMenuAsync(host.Services, CancellationToken.None);
        return;
    }

    await ExecuteCommandAsync(host.Services, command, args, CancellationToken.None);
}
catch (Exception ex)
{
    Log.Fatal(ex, "PostgreSQL sync console terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
Log.CloseAndFlush();
}

static string ResolveLogPath(
    IConfiguration configuration,
    IHostEnvironment environment,
    string defaultRelativePath)
{
    var configuredPath = configuration["Serilog:LogPath"];
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        configuredPath = defaultRelativePath;
    }

    return Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.Combine(environment.ContentRootPath, configuredPath);
}

static void ConfigureAppConfiguration(HostApplicationBuilder builder)
{
    var appBasePath = AppContext.BaseDirectory;

    builder.Configuration.Sources.Clear();
    builder.Configuration
        .SetBasePath(appBasePath)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile(
            $"appsettings.{builder.Environment.EnvironmentName}.json",
            optional: true,
            reloadOnChange: true)
        .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();
}

static async Task RunSyncOnceAsync(
    IServiceProvider services,
    IReadOnlyList<SyncPhase> phases,
    IReadOnlyDictionary<string, string>? tokens,
    IReadOnlySet<string>? includedTables,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var runner = scope.ServiceProvider.GetRequiredService<PostgresSyncRunner>();
    var resolvedTokens = new Dictionary<string, string>(
        tokens ?? new Dictionary<string, string>(),
        StringComparer.OrdinalIgnoreCase);
    resolvedTokens.TryAdd("family_filter", "TRUE");
    await runner.RunOnceAsync(phases, resolvedTokens, includedTables, cancellationToken);
}

async Task ExecuteCommandAsync(
    IServiceProvider services,
    CommandMode command,
    string[] args,
    CancellationToken cancellationToken)
{
    var includedTables = ResolveIncludedTables(args);

    switch (command)
    {
        case CommandMode.Inspect:
            await RunInspectAsync(services, cancellationToken);
            return;

        case CommandMode.SyncMasterData:
            await RunSyncOnceAsync(services, [SyncPhase.MasterData], null, includedTables, cancellationToken);
            return;

        case CommandMode.SyncBiometricSourceData:
            await RunSyncOnceAsync(
                services,
                [SyncPhase.MasterData, SyncPhase.BiometricSourceData],
                null,
                ResolveBiometricSourceTables(includedTables),
                cancellationToken);
            return;

        case CommandMode.SyncAttendanceDaily:
            var attendanceDateRange = TryResolveAttendanceDateRange(args);
            if (attendanceDateRange is { } resolvedAttendanceDateRange)
            {
                System.Console.WriteLine(
                    $"Khoảng ngày đồng bộ chấm công: từ={resolvedAttendanceDateRange.From:yyyy-MM-dd}; đến={resolvedAttendanceDateRange.To:yyyy-MM-dd}");
            }

            await RunSyncOnceAsync(
                services,
                [SyncPhase.MasterData, SyncPhase.AttendanceDaily],
                attendanceDateRange is null ? null : BuildAttendanceRangeTokens(attendanceDateRange.Value),
                ResolveAttendanceTables(includedTables),
                cancellationToken);
            return;

        case CommandMode.SyncAttendanceLogs:
            var dateRange = ResolveAttendanceDateRange(args);
            System.Console.WriteLine(
                $"Khoảng ngày đồng bộ dữ liệu chấm công thô: từ={dateRange.From:yyyy-MM-dd}; đến={dateRange.To:yyyy-MM-dd}");
            await RunSyncOnceAsync(
                services,
                [SyncPhase.MasterData, SyncPhase.AttendanceDaily],
                BuildAttendanceRangeTokens(dateRange),
                ResolveAttendanceTables(
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "public.attendance_logs"
                    }),
                cancellationToken);
            return;

        case CommandMode.SyncAttendanceWorkdaySummaries:
            var workdaySummaryPeriod = ResolveAttendanceWorkdaySummaryPeriod(args);
            var workdaySummaryStart = new DateOnly(
                workdaySummaryPeriod.Year,
                workdaySummaryPeriod.Month,
                1);
            var workdaySummaryDateRange = CreateAttendanceDateRange(
                workdaySummaryStart,
                workdaySummaryStart.AddMonths(1).AddDays(-1));
            System.Console.WriteLine(
                $"Đồng bộ bảng công tháng: tháng={workdaySummaryPeriod.Month:00}; năm={workdaySummaryPeriod.Year:0000}");
            await RunSyncOnceAsync(
                services,
                [SyncPhase.MasterData, SyncPhase.AttendanceDaily],
                BuildAttendanceRangeTokens(workdaySummaryDateRange),
                ResolveAttendanceTables(
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "public.attendance_workday_summaries"
                    }),
                cancellationToken);
            return;

        case CommandMode.SyncPayrollBasicSalary:
            var payrollPeriod = ResolvePayrollPeriod(args);
            System.Console.WriteLine(
                $"Đồng bộ lương cơ bản cho kỳ: tháng={payrollPeriod.Month:00}; năm={payrollPeriod.Year:0000}");
            await RunSyncPayrollBasicSalaryAsync(
                services,
                payrollPeriod.Month,
                payrollPeriod.Year,
                cancellationToken);
            return;

        case CommandMode.SyncPayrollOtherAllowance:
            var otherAllowancePeriod = ResolvePayrollPeriod(args);
            System.Console.WriteLine(
                $"Đồng bộ phụ cấp khác cho kỳ: tháng={otherAllowancePeriod.Month:00}; năm={otherAllowancePeriod.Year:0000}");
            await RunSyncPayrollOtherAllowanceAsync(
                services,
                otherAllowancePeriod.Month,
                otherAllowancePeriod.Year,
                cancellationToken);
            return;

        case CommandMode.SyncPayrollInsuranceDeduction:
            var insuranceDeductionPeriod = ResolvePayrollPeriod(args);
            System.Console.WriteLine(
                $"Đồng bộ khấu trừ BHXH-Y tế cho kỳ: tháng={insuranceDeductionPeriod.Month:00}; năm={insuranceDeductionPeriod.Year:0000}");
            await RunSyncPayrollInsuranceDeductionAsync(
                services,
                insuranceDeductionPeriod.Month,
                insuranceDeductionPeriod.Year,
                cancellationToken);
            return;

        case CommandMode.SyncPayrollResponsibilityAllowance:
            var responsibilityPeriod = ResolvePayrollPeriod(args);
            System.Console.WriteLine(
                $"Đồng bộ phụ cấp trách nhiệm cho kỳ: tháng={responsibilityPeriod.Month:00}; năm={responsibilityPeriod.Year:0000}");
            await RunSyncPayrollResponsibilityAllowanceAsync(
                services,
                responsibilityPeriod.Month,
                responsibilityPeriod.Year,
                cancellationToken);
            return;

        case CommandMode.SyncFamilyDeduction:
            await RunSyncOnceAsync(
                services,
                [SyncPhase.MasterData],
                null,
                ResolveFamilyDeductionTables(includedTables),
                cancellationToken);
            return;

        case CommandMode.SyncAll:
        default:
            await RunSyncOnceAsync(
                services,
                [SyncPhase.MasterData, SyncPhase.BiometricSourceData, SyncPhase.AttendanceDaily],
                null,
                ResolveSyncAllTables(includedTables),
                cancellationToken);
            return;
    }
}

static async Task RunInspectAsync(
    IServiceProvider services,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var inspector = scope.ServiceProvider.GetRequiredService<SchemaInspectionService>();
    await inspector.WriteSchemaComparisonAsync(cancellationToken);
}

static async Task RunSyncPayrollBasicSalaryAsync(
    IServiceProvider services,
    int targetMonth,
    int targetYear,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var runner = scope.ServiceProvider.GetRequiredService<PostgresSyncRunner>();
    var result = await runner.SyncPayrollBasicSalaryFromPreviousMonthAsync(
        targetMonth,
        targetYear,
        cancellationToken);

    System.Console.WriteLine(
        "Đồng bộ lương cơ bản hoàn tất: "
        + $"kỳ nguồn={result.SourceMonth:00}/{result.SourceYear}; "
        + $"kỳ đích={result.TargetMonth:00}/{result.TargetYear}; "
        + $"dòng nguồn={result.SourceRecordCount}; "
        + $"tạo mới={result.CreatedRecordCount}; "
        + $"cập nhật={result.UpdatedRecordCount}; "
        + $"không đổi={result.UnchangedRecordCount}.");
}

static async Task RunSyncPayrollOtherAllowanceAsync(
    IServiceProvider services,
    int payrollMonth,
    int payrollYear,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var runner = scope.ServiceProvider.GetRequiredService<PostgresSyncRunner>();
    var result = await runner.SyncPayrollOtherAllowanceAsync(
        payrollMonth,
        payrollYear,
        cancellationToken);

    System.Console.WriteLine(
        "Đồng bộ phụ cấp khác hoàn tất: "
        + $"dòng nguồn={result.SourceRecordCount}; "
        + $"tạo mới={result.CreatedRecordCount}; "
        + $"cập nhật={result.UpdatedRecordCount}; "
        + $"không đổi={result.UnchangedRecordCount}; "
        + $"bỏ qua do khóa={result.SkippedLockedRecordCount}; "
        + $"chuẩn hóa snapshot thành cố định={result.NormalizedToFixedSnapshotRecordCount}; "
        + $"tổng tiền nguồn={result.SourceTotalAmount:N0}; "
        + $"tổng tiền đã xử lý={result.SynchronizedTotalAmount:N0}.");
}

static async Task RunSyncPayrollInsuranceDeductionAsync(
    IServiceProvider services,
    int payrollMonth,
    int payrollYear,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var runner = scope.ServiceProvider.GetRequiredService<PostgresSyncRunner>();
    var result = await runner.SyncPayrollInsuranceDeductionAsync(
        payrollMonth,
        payrollYear,
        cancellationToken);

    System.Console.WriteLine(
        "Đồng bộ khấu trừ BHXH-Y tế hoàn tất: "
        + $"dòng nguồn={result.SourceRecordCount}; "
        + $"tạo mới={result.CreatedRecordCount}; "
        + $"cập nhật={result.UpdatedRecordCount}; "
        + $"không đổi={result.UnchangedRecordCount}; "
        + $"bỏ qua do khóa={result.SkippedLockedRecordCount}; "
        + $"tổng khấu trừ nguồn={result.SourceTotalDeductionAmount:N0}; "
        + $"tổng khấu trừ đã xử lý={result.SynchronizedTotalDeductionAmount:N0}.");
}

static async Task RunSyncPayrollResponsibilityAllowanceAsync(
    IServiceProvider services,
    int payrollMonth,
    int payrollYear,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var runner = scope.ServiceProvider.GetRequiredService<PostgresSyncRunner>();
    var result = await runner.SyncPayrollResponsibilityAllowanceAsync(
        payrollMonth,
        payrollYear,
        cancellationToken);

    System.Console.WriteLine(
        "Đồng bộ phụ cấp trách nhiệm hoàn tất: "
        + $"dòng nguồn={result.SourceRecordCount}; "
        + $"tạo mới={result.CreatedRecordCount}; "
        + $"cập nhật={result.UpdatedRecordCount}; "
        + $"không đổi={result.UnchangedRecordCount}; "
        + $"bỏ qua do khóa={result.SkippedLockedRecordCount}; "
        + $"bỏ qua do thiếu bản ghi tổng hợp={result.SkippedMissingParentRecordCount}; "
        + $"tổng phụ cấp nguồn={result.SourceTotalAmount:N0}; "
        + $"tổng phụ cấp đã xử lý={result.SynchronizedTotalAmount:N0}.");
}

async Task RunInteractiveMenuAsync(
    IServiceProvider services,
    CancellationToken cancellationToken)
{
    while (true)
    {
        WriteMenu();
        var selection = System.Console.ReadLine()?.Trim();
        if (selection is null
            || string.Equals(selection, "0", StringComparison.OrdinalIgnoreCase)
            || string.Equals(selection, "q", StringComparison.OrdinalIgnoreCase)
            || string.Equals(selection, "exit", StringComparison.OrdinalIgnoreCase))
        {
            System.Console.WriteLine("Đã thoát chương trình đồng bộ.");
            return;
        }

        var command = ResolveMenuSelection(selection);
        if (command is null)
        {
            System.Console.WriteLine("Lựa chọn không hợp lệ. Vui lòng chọn lại.");
            continue;
        }

        System.Console.WriteLine();
        System.Console.WriteLine($"Đang chạy chức năng: {GetCommandTitle(command.Value)}");

        try
        {
            await ExecuteCommandAsync(services, command.Value, [], cancellationToken);
            System.Console.WriteLine("Chức năng đã chạy xong.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Chức năng bị lỗi: {ex.Message}");
        }

        System.Console.WriteLine();
        System.Console.Write("Nhấn Enter để quay lại menu...");
        System.Console.ReadLine();
    }
}

static void WriteMenu()
{
    if (!System.Console.IsOutputRedirected)
    {
        System.Console.Clear();
    }

    System.Console.WriteLine("========================================");
    System.Console.WriteLine(" VNTA PostgreSQL Sync Console");
    System.Console.WriteLine("========================================");
    System.Console.WriteLine("1. Kiểm tra schema nguồn/đích");
    System.Console.WriteLine("2. Đồng bộ dữ liệu danh mục");
    System.Console.WriteLine("3. Đồng bộ dữ liệu sinh trắc học gốc");
    System.Console.WriteLine("4. Đồng bộ dữ liệu chấm công thô theo ngày");
    System.Console.WriteLine("5. Đồng bộ dữ liệu chấm công theo ngày");
    System.Console.WriteLine("6. Đồng bộ lương cơ bản từ tháng trước");
    System.Console.WriteLine("7. Đồng bộ tất cả");
    System.Console.WriteLine("8. Đồng bộ bảng công tháng");
    System.Console.WriteLine("9. Đồng bộ giảm trừ gia cảnh");
    System.Console.WriteLine("10. Đồng bộ phụ cấp khác theo kỳ lương");
    System.Console.WriteLine("11. Đồng bộ khấu trừ BHXH-Y tế theo kỳ lương");
    System.Console.WriteLine("12. Đồng bộ phụ cấp trách nhiệm theo kỳ lương");
    System.Console.WriteLine("0. Thoát");
    System.Console.WriteLine("----------------------------------------");
    System.Console.Write("Chọn chức năng: ");
}

static CommandMode? ResolveMenuSelection(string? selection)
{
    return selection switch
    {
        "1" => CommandMode.Inspect,
        "2" => CommandMode.SyncMasterData,
        "3" => CommandMode.SyncBiometricSourceData,
        "4" => CommandMode.SyncAttendanceLogs,
        "5" => CommandMode.SyncAttendanceDaily,
        "6" => CommandMode.SyncPayrollBasicSalary,
        "7" => CommandMode.SyncAll,
        "8" => CommandMode.SyncAttendanceWorkdaySummaries,
        "9" => CommandMode.SyncFamilyDeduction,
        "10" => CommandMode.SyncPayrollOtherAllowance,
        "11" => CommandMode.SyncPayrollInsuranceDeduction,
        "12" => CommandMode.SyncPayrollResponsibilityAllowance,
        _ => null
    };
}

static string GetCommandTitle(CommandMode command)
{
    return command switch
    {
        CommandMode.Inspect => "Kiểm tra schema nguồn/đích",
        CommandMode.SyncMasterData => "Đồng bộ dữ liệu danh mục",
        CommandMode.SyncBiometricSourceData => "Đồng bộ dữ liệu sinh trắc học gốc",
        CommandMode.SyncAttendanceLogs => "Đồng bộ dữ liệu chấm công thô theo ngày",
        CommandMode.SyncAttendanceDaily => "Đồng bộ dữ liệu chấm công theo ngày",
        CommandMode.SyncAttendanceWorkdaySummaries => "Đồng bộ bảng công tháng",
        CommandMode.SyncPayrollBasicSalary => "Đồng bộ lương cơ bản từ tháng trước",
        CommandMode.SyncPayrollOtherAllowance => "Đồng bộ phụ cấp khác theo kỳ lương",
        CommandMode.SyncPayrollInsuranceDeduction => "Đồng bộ khấu trừ BHXH-Y tế theo kỳ lương",
        CommandMode.SyncPayrollResponsibilityAllowance => "Đồng bộ phụ cấp trách nhiệm theo kỳ lương",
        CommandMode.SyncFamilyDeduction => "Đồng bộ giảm trừ gia cảnh",
        CommandMode.SyncAll => "Đồng bộ tất cả",
        _ => command.ToString()
    };
}

static PayrollPeriod ResolvePayrollPeriod(string[] args)
{
    var month = TryReadIntArgument(args, "--month")
        ?? PromptForInt("Tháng đích (1-12): ");
    var year = TryReadIntArgument(args, "--year")
        ?? PromptForInt("Năm đích (yyyy): ");

    if (month is < 1 or > 12)
    {
        throw new InvalidOperationException("Tháng đích phải nằm trong khoảng 1..12.");
    }

    if (year is < 1 or > 9999)
    {
        throw new InvalidOperationException("Năm đích phải nằm trong khoảng 1..9999.");
    }

    return new PayrollPeriod(month, year);
}

static PayrollPeriod ResolveAttendanceWorkdaySummaryPeriod(string[] args)
{
    var month = TryReadIntArgument(args, "--month")
        ?? PromptForInt("Tháng cần đồng bộ (1-12): ");
    var year = TryReadIntArgument(args, "--year")
        ?? PromptForInt("Năm cần đồng bộ (yyyy): ");

    if (month is < 1 or > 12)
    {
        throw new InvalidOperationException("Tháng cần đồng bộ phải nằm trong khoảng 1..12.");
    }

    if (year is < 1 or > 9999)
    {
        throw new InvalidOperationException("Năm cần đồng bộ phải nằm trong khoảng 1..9999.");
    }

    return new PayrollPeriod(month, year);
}

static AttendanceDateRange? TryResolveAttendanceDateRange(string[] args)
{
    var from = TryReadDateArgument(args, "--from");
    var to = TryReadDateArgument(args, "--to");

    if (from is null && to is null)
    {
        return null;
    }

    if (from is null || to is null)
    {
        throw new InvalidOperationException(
            "Khi dùng --from hoặc --to cho sync-attendance, cần truyền đầy đủ cả hai mốc ngày.");
    }

    return CreateAttendanceDateRange(from.Value, to.Value);
}

static AttendanceDateRange ResolveAttendanceDateRange(string[] args)
{
    var from = TryReadDateArgument(args, "--from")
        ?? PromptForDate("Từ ngày (yyyy-MM-dd hoặc dd/MM/yyyy): ");
    var to = TryReadDateArgument(args, "--to")
        ?? PromptForDate("Đến ngày (yyyy-MM-dd hoặc dd/MM/yyyy): ");

    return CreateAttendanceDateRange(from, to);
}

static AttendanceDateRange CreateAttendanceDateRange(DateOnly from, DateOnly to)
{
    if (to < from)
    {
        throw new InvalidOperationException("Đến ngày phải lớn hơn hoặc bằng từ ngày.");
    }

    return new AttendanceDateRange(from, to);
}

static IReadOnlySet<string>? ResolveIncludedTables(string[] args)
{
    var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    for (var index = 1; index < args.Length; index++)
    {
        var arg = args[index];
        if (arg.StartsWith("--table=", StringComparison.OrdinalIgnoreCase))
        {
            AddTables(tables, arg["--table=".Length..]);
            continue;
        }

        if (arg.StartsWith("--tables=", StringComparison.OrdinalIgnoreCase))
        {
            AddTables(tables, arg["--tables=".Length..]);
            continue;
        }

        if ((string.Equals(arg, "--table", StringComparison.OrdinalIgnoreCase)
                || string.Equals(arg, "--tables", StringComparison.OrdinalIgnoreCase))
            && index + 1 < args.Length)
        {
            AddTables(tables, args[++index]);
        }
    }

    return tables.Count == 0 ? null : tables;
}

static void AddTables(ISet<string> tables, string rawValue)
{
    foreach (var table in rawValue.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        tables.Add(table);
    }
}

static IReadOnlyDictionary<string, string> BuildAttendanceRangeTokens(AttendanceDateRange dateRange)
{
    var from = dateRange.From.ToDateTime(TimeOnly.MinValue);
    var toExclusive = dateRange.To.AddDays(1).ToDateTime(TimeOnly.MinValue);
    var toExclusiveDate = dateRange.To.AddDays(1);

    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["from_date"] = dateRange.From.ToString("yyyy-MM-dd"),
        ["to_date"] = dateRange.To.ToString("yyyy-MM-dd"),
        ["to_exclusive_date"] = toExclusiveDate.ToString("yyyy-MM-dd"),
        ["from_start"] = $"{from:yyyy-MM-dd HH:mm:ss}",
        ["to_exclusive_start"] = $"{toExclusive:yyyy-MM-dd HH:mm:ss}"
    };
}

IReadOnlySet<string> ResolveBiometricSourceTables(IReadOnlySet<string>? includedTables)
{
    return ResolveTablesWithDependencies(
        includedTables,
        biometricSourceTables,
        biometricSourceDependencyTables);
}

IReadOnlySet<string> ResolveAttendanceTables(IReadOnlySet<string>? includedTables)
{
    return ResolveTablesWithDependencies(
        includedTables,
        attendanceDailyTables,
        attendanceDependencyTables);
}

IReadOnlySet<string> ResolveFamilyDeductionTables(IReadOnlySet<string>? includedTables)
{
    return ResolveTablesWithDependencies(
        includedTables,
        familyDeductionTables,
        familyDeductionDependencyTables);
}

IReadOnlySet<string>? ResolveSyncAllTables(IReadOnlySet<string>? includedTables)
{
    if (includedTables is null || includedTables.Count == 0)
    {
        return null;
    }

    var tables = new HashSet<string>(includedTables, StringComparer.OrdinalIgnoreCase);

    AddDependencyTables(
        tables,
        includedTables,
        biometricSourceTables,
        biometricSourceDependencyTables);
    AddDependencyTables(
        tables,
        includedTables,
        attendanceDailyTables,
        attendanceDependencyTables);
    return tables;
}

static HashSet<string> ResolveTablesWithDependencies(
    IReadOnlySet<string>? includedTables,
    IReadOnlyList<string> primaryTables,
    IReadOnlyList<string> dependencyTables)
{
    var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    if (includedTables is null || includedTables.Count == 0)
    {
        foreach (var table in dependencyTables)
        {
            tables.Add(table);
        }

        foreach (var table in primaryTables)
        {
            tables.Add(table);
        }

        return tables;
    }

    foreach (var table in includedTables)
    {
        tables.Add(table);
    }

    AddDependencyTables(tables, includedTables, primaryTables, dependencyTables);
    return tables;
}

static void AddDependencyTables(
    ISet<string> tables,
    IReadOnlySet<string> requestedTables,
    IReadOnlyList<string> primaryTables,
    IReadOnlyList<string> dependencyTables)
{
    var requestedPrimaryTable = requestedTables.Any(table =>
        primaryTables.Contains(table, StringComparer.OrdinalIgnoreCase));

    if (!requestedPrimaryTable)
    {
        return;
    }

    foreach (var table in dependencyTables)
    {
        tables.Add(table);
    }
}

static DateOnly? TryReadDateArgument(string[] args, string name)
{
    for (var index = 0; index < args.Length; index++)
    {
        var arg = args[index];
        if (arg.StartsWith($"{name}=", StringComparison.OrdinalIgnoreCase))
        {
            return ParseDate(arg[(name.Length + 1)..]);
        }

        if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase)
            && index + 1 < args.Length)
        {
            return ParseDate(args[index + 1]);
        }
    }

    return null;
}

static int? TryReadIntArgument(string[] args, string name)
{
    for (var index = 0; index < args.Length; index++)
    {
        var arg = args[index];
        if (arg.StartsWith($"{name}=", StringComparison.OrdinalIgnoreCase))
        {
            return ParseInt(arg[(name.Length + 1)..], name);
        }

        if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase)
            && index + 1 < args.Length)
        {
            return ParseInt(args[index + 1], name);
        }
    }

    return null;
}

static DateOnly PromptForDate(string prompt)
{
    while (true)
    {
        System.Console.Write(prompt);
        var rawValue = System.Console.ReadLine();

        if (TryParseDate(rawValue, out var value))
        {
            return value;
        }

        System.Console.WriteLine("Ngày không hợp lệ. Vui lòng nhập theo định dạng yyyy-MM-dd hoặc dd/MM/yyyy.");
    }
}

static int PromptForInt(string prompt)
{
    while (true)
    {
        System.Console.Write(prompt);
        var rawValue = System.Console.ReadLine();

        if (int.TryParse(rawValue, out var value))
        {
            return value;
        }

        System.Console.WriteLine("Giá trị không hợp lệ. Vui lòng nhập số nguyên.");
    }
}

static DateOnly ParseDate(string rawValue)
{
    if (TryParseDate(rawValue, out var value))
    {
        return value;
    }

    throw new InvalidOperationException(
        $"Ngày '{rawValue}' không hợp lệ. Vui lòng nhập theo định dạng yyyy-MM-dd hoặc dd/MM/yyyy.");
}

static int ParseInt(string rawValue, string argumentName)
{
    if (int.TryParse(rawValue, out var value))
    {
        return value;
    }

    throw new InvalidOperationException(
        $"Giá trị '{rawValue}' cho {argumentName} không hợp lệ. Vui lòng nhập số nguyên.");
}

static bool TryParseDate(string? rawValue, out DateOnly value)
{
    return DateOnly.TryParseExact(
            rawValue,
            ["yyyy-MM-dd", "dd/MM/yyyy"],
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out value)
        || DateOnly.TryParse(rawValue, out value);
}

internal readonly record struct AttendanceDateRange(DateOnly From, DateOnly To);
internal readonly record struct PayrollPeriod(int Month, int Year);
