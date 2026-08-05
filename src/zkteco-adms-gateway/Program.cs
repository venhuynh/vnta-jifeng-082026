using Vnta.AttendanceGateway.Configuration;
using Vnta.AttendanceGateway.Integration;
using Vnta.AttendanceGateway.Logging;
using Vnta.AttendanceGateway.Network;
using Vnta.AttendanceGateway.Protocol.Handlers;
using Vnta.AttendanceGateway.Protocol.Routing;
using Vnta.AttendanceGateway.Security;
using Vnta.AttendanceGateway.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Npgsql;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:dd/MM/yyyy HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .AddCommandLine(args);

    var logsPath = ResolveLogPath(
        builder.Configuration,
        builder.Environment,
        "Logs/jifeng-attendance-gateway");
    Directory.CreateDirectory(logsPath);
    var retainedFileCountLimit = builder.Configuration.GetValue("Serilog:RetainedFileCountLimit", 14);
    var fileSizeLimitBytes = builder.Configuration.GetValue<long?>("Serilog:FileSizeLimitBytes") ?? 104_857_600;

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "VNTA.AttendanceGateway")
            .Enrich.WithProperty("Service", "VNTA Attendance Gateway")
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

    builder.Services.Configure<AttendanceGatewayOptions>(builder.Configuration.GetSection(AttendanceGatewayOptions.SectionName));
    builder.Services.Configure<CoreApiOptions>(builder.Configuration.GetSection(CoreApiOptions.SectionName));
    builder.Services.Configure<FrontendCorsOptions>(builder.Configuration.GetSection(FrontendCorsOptions.SectionName));

    var gatewayOptions = builder.Configuration.GetSection(AttendanceGatewayOptions.SectionName).Get<AttendanceGatewayOptions>()
        ?? new AttendanceGatewayOptions();
    var frontendCorsOptions = builder.Configuration.GetSection(FrontendCorsOptions.SectionName).Get<FrontendCorsOptions>()
        ?? new FrontendCorsOptions();
    var allowedOrigins = frontendCorsOptions.AllowedOrigins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    builder.WebHost.UseUrls($"http://0.0.0.0:{gatewayOptions.ControlPlanePort}");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendSignalR", policy =>
        {
            if (allowedOrigins.Length == 0 && !frontendCorsOptions.AllowPrivateNetworkOrigins)
            {
                return;
            }

            if (frontendCorsOptions.AllowPrivateNetworkOrigins)
            {
                policy
                    .SetIsOriginAllowed(origin => IsAllowedFrontendOrigin(origin, allowedOrigins))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();

                return;
            }

            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

// Runtime configuration is mandatory; never fall back to tracked credentials.
var connectionString = builder.Configuration.GetConnectionString("Postgres")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("VNTA_DB");
}
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Database connection is required. Set ConnectionStrings__Postgres, ConnectionStrings__DefaultConnection, or VNTA_DB.");
}

builder.Services.AddDbContext<Vnta.AttendanceGateway.Data.ZktecoDbContext>(opts => 
    opts.UseNpgsql(connectionString));

// Register In-Memory Cache
builder.Services.AddMemoryCache();

// Register SignalR for Frontend Broadcasting
builder.Services.AddSignalR();  

// Register Core Services
builder.Services.AddHttpClient<CoreApiClient>((serviceProvider, client) =>
{
    var coreApiOptions = serviceProvider.GetRequiredService<IOptions<CoreApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(coreApiOptions.BaseUrl))
    {
        client.BaseAddress = new Uri(coreApiOptions.BaseUrl, UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(coreApiOptions.TimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
{
    var coreApiOptions = serviceProvider.GetRequiredService<IOptions<CoreApiOptions>>().Value;
    var handler = new HttpClientHandler();
    if (!coreApiOptions.Enabled)
    {
        return handler;
    }

    if (string.IsNullOrWhiteSpace(coreApiOptions.ClientCertificatePath)
        || string.IsNullOrWhiteSpace(coreApiOptions.ClientCertificatePassword))
    {
        throw new InvalidOperationException("Core API client certificate path and password are required when CoreApi is enabled.");
    }

    var certificate = X509CertificateLoader.LoadPkcs12FromFile(
        coreApiOptions.ClientCertificatePath,
        coreApiOptions.ClientCertificatePassword);
    handler.ClientCertificates.Add(certificate);
    if (!string.IsNullOrWhiteSpace(coreApiOptions.TrustedServerCertificateSha256Thumbprint))
    {
        handler.ServerCertificateCustomValidationCallback = (_, serverCertificate, _, errors) =>
        {
            if (serverCertificate is null || (errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
            {
                return false;
            }

            if (errors == SslPolicyErrors.None)
            {
                return true;
            }

            var actualThumbprint = Convert.ToHexString(SHA256.HashData(serverCertificate.GetRawCertData()));
            var configuredThumbprint = coreApiOptions.TrustedServerCertificateSha256Thumbprint.Replace(":", string.Empty, StringComparison.Ordinal);
            return string.Equals(actualThumbprint, configuredThumbprint, StringComparison.OrdinalIgnoreCase);
        };
    }

    return handler;
})
.AddHttpMessageHandler<CoreApiHmacAuthenticationHandler>();

builder.Services.AddTransient<CoreApiHmacAuthenticationHandler>();

builder.Services.AddSingleton<SystemLogQueue>();
builder.Services.AddSingleton<AdmsRealtimeEventQueue>();
builder.Services.AddSingleton<AttendanceGatewayRawCommunicationLogger>();
builder.Services.AddSingleton<RealtimeGatewayLogPublisher>();
builder.Services.AddSingleton<AdmsActivityPublisher>();
builder.Services.AddSingleton<DeviceCommandPollingService>();
builder.Services.AddSingleton<DeviceCommandCallbackService>();
builder.Services.AddSingleton<DeviceOptionsSyncService>();
builder.Services.AddSingleton<AttendanceGatewayEmployeeIdentityResolver>();
builder.Services.AddSingleton<AttendancePhotoStampSyncService>();
builder.Services.AddSingleton<AttendanceLogSyncService>();
builder.Services.AddSingleton<OperationalLogSyncService>();
builder.Services.AddSingleton<BioDataSyncService>();
builder.Services.AddSingleton<ErrorLogSyncService>();
builder.Services.AddSingleton<DeviceAuthorizationService>();
builder.Services.AddSingleton<IRequestHandler, HandshakeHandler>();
builder.Services.AddSingleton<IRequestHandler, AttendanceLogHandler>();
builder.Services.AddSingleton<IRequestHandler, AttendancePhotoHandler>();
builder.Services.AddSingleton<IRequestHandler, CommandFetchHandler>();
builder.Services.AddSingleton<IRequestHandler, DeviceCommandResultHandler>();
builder.Services.AddSingleton<IRequestHandler, DeviceOptionsHandler>();
builder.Services.AddSingleton<IRequestHandler, OperationalLogHandler>();
builder.Services.AddSingleton<IRequestHandler, BioDataHandler>();
builder.Services.AddSingleton<IRequestHandler, ErrorLogHandler>();
builder.Services.AddSingleton<ZktecoRequestRouter>();

builder.Services.AddSingleton<ZktecoTcpServerManager>();

// Register Hosted Service
builder.Services.AddHostedService<SystemLogPublishWorker>();
builder.Services.AddHostedService<AttendancePublishWorker>();
builder.Services.AddHostedService<AdmsRealtimePublishWorker>();
builder.Services.AddHostedService<ZktecoTcpListenerWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Vnta.AttendanceGateway.Data.ZktecoDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AttendanceGatewayStartup");
    await WaitForDatabaseAsync(dbContext, startupLogger, CancellationToken.None);
    await ZktecoSchemaGuard.EnsureEmployeeAvatarColumnAsync(dbContext, CancellationToken.None);
    var serialGuardResult = await ZktecoSchemaGuard.EnsureUniqueDeviceSerialNumbersAsync(dbContext, CancellationToken.None);
    if (serialGuardResult.HasChanges)
    {
        startupLogger.LogInformation(
            "Normalized duplicated attendance devices on startup. NormalizedRows={NormalizedRows}, DuplicateGroups={DuplicateGroups}, DeletedDevices={DeletedDevices}, RemappedAttendanceLogs={RemappedAttendanceLogs}",
            serialGuardResult.NormalizedRowCount,
            serialGuardResult.DuplicateGroupCount,
            serialGuardResult.DeletedDeviceCount,
            serialGuardResult.RemappedAttendanceLogCount);
    }
    await ZktecoSchemaGuard.EnsureOutboundAttendanceLogTableAsync(dbContext, CancellationToken.None);
    await ZktecoSchemaGuard.EnsureOutboundSystemLogTableAsync(dbContext, CancellationToken.None);
    var avatarRows = await ZktecoSchemaGuard.BackfillEmployeeAvatarsAsync(dbContext, CancellationToken.None);
    if (avatarRows > 0)
    {
        startupLogger.LogInformation("Backfilled employee avatars on startup. UpdatedRows={UpdatedRows}", avatarRows);
    }

    var updatedRows = await ZktecoSchemaGuard.BackfillMissingEmployeeEmailsAsync(dbContext, CancellationToken.None);
    if (updatedRows > 0)
    {
        startupLogger.LogInformation("Backfilled missing employee emails on startup. UpdatedRows={UpdatedRows}", updatedRows);
    }
}

app.UseCors("FrontendSignalR");

// Setup SignalR Hub Endpoint
app.MapHub<Vnta.AttendanceGateway.Hubs.DeviceHub>("/hubs/device");

// Setup Minimal APIs for Gateway Remote Control
app.MapGet("/api/attendance-gateway/status", (ZktecoTcpServerManager manager) => Results.Ok(manager.GetStatus()));

app.MapPost("/api/attendance-gateway/start", async (ZktecoTcpServerManager manager) =>
{
    await manager.StartListeningAsync();
    return Results.Ok(new { message = "TCP Listener Started" });
});

app.MapPost("/api/attendance-gateway/stop", async (ZktecoTcpServerManager manager) =>
{
    await manager.StopListeningAsync();
    return Results.Ok(new { message = "TCP Listener Stopped" });
});

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

static string ResolveLogPath(IConfiguration configuration, IHostEnvironment environment, string defaultRelativePath)
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

static async Task WaitForDatabaseAsync(
    Vnta.AttendanceGateway.Data.ZktecoDbContext dbContext,
    Microsoft.Extensions.Logging.ILogger logger,
    CancellationToken cancellationToken)
{
    const int maxAttempts = 10;
    var retryDelay = TimeSpan.FromSeconds(3);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                if (attempt > 1)
                {
                    logger.LogInformation(
                        "Database connection became available after retry. Attempt={Attempt}",
                        attempt);
                }

                return;
            }
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            if (attempt == maxAttempts)
            {
                throw;
            }

            logger.LogWarning(
                ex,
                "Database is not ready yet during VNTA Attendance Gateway startup. Attempt={Attempt}/{MaxAttempts}. Retrying in {RetryDelaySeconds}s.",
                attempt,
                maxAttempts,
                retryDelay.TotalSeconds);

            await Task.Delay(retryDelay, cancellationToken);
            continue;
        }

        if (attempt == maxAttempts)
        {
            throw new InvalidOperationException("Database connection check failed during VNTA Attendance Gateway startup.");
        }

        logger.LogWarning(
            "Database connection check returned unavailable during VNTA Attendance Gateway startup. Attempt={Attempt}/{MaxAttempts}. Retrying in {RetryDelaySeconds}s.",
            attempt,
            maxAttempts,
            retryDelay.TotalSeconds);

        await Task.Delay(retryDelay, cancellationToken);
    }
}

static bool IsAllowedFrontendOrigin(string origin, string[] allowedOrigins)
{
    if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(uri.Host, "host.docker.internal", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!IPAddress.TryParse(uri.Host, out var ipAddress))
    {
        return false;
    }

    return IPAddress.IsLoopback(ipAddress) || IsPrivateNetworkAddress(ipAddress);
}

static bool IsPrivateNetworkAddress(IPAddress ipAddress)
{
    if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
    {
        var bytes = ipAddress.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
    }

    if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
    {
        return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal || ipAddress.IsIPv6UniqueLocal;
    }

    return false;
}


