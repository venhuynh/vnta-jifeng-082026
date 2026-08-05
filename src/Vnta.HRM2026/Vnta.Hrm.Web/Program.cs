using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Net;
using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Vnta.Hrm.Infrastructure;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Web.Audit;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Services.Adms;
using Vnta.Hrm.Web.Client.Services.Api;
using Vnta.Hrm.Web.Client.Services.Api.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Utils;
using Vnta.Hrm.Web.Components;
using Vnta.Hrm.Web.Components.Account;
using Vnta.Hrm.Web.Endpoints;
using Vnta.Hrm.Web.Endpoints.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Endpoints.NhanSu.NhanVien;
using Vnta.Hrm.Web.Endpoints.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Endpoints.QuanTri.TaiKhoanNhanVien;
using Vnta.Hrm.Web.ErrorHandling;
using Vnta.Hrm.Web.Hubs;
using Vnta.Hrm.Web.HostedServices;
using Vnta.Hrm.Web.Security;
using Vnta.Hrm.Web.Services;
using Vnta.Hrm.Web.Services.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Services.NhanSu.NhanVien;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:dd/MM/yyyy HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var vietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
    CultureInfo.DefaultThreadCurrentCulture = vietnameseCulture;
    CultureInfo.DefaultThreadCurrentUICulture = vietnameseCulture;
    builder.Configuration
        .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .AddCommandLine(args);
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            // This is a browser-facing HTTPS endpoint.  Requesting an optional
            // client certificate causes Chrome/Edge to show a certificate picker
            // on every visit.  Gateway authentication is handled by HMAC below.
            httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
        });
    });

    var logsPath = ResolveLogPath(
        builder.Configuration,
        builder.Environment,
        "Logs/vnta-hrm");
    Directory.CreateDirectory(logsPath);
    var retainedFileCountLimit = builder.Configuration.GetValue("Serilog:RetainedFileCountLimit", 14);
    var fileSizeLimitBytes = builder.Configuration.GetValue<long?>("Serilog:FileSizeLimitBytes") ?? 104_857_600;

    builder.Host.UseSerilog(
        (context, services, loggerConfiguration) =>
        {
            loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "VNTA.HRM")
            .Enrich.WithProperty("Service", "Vnta.Hrm.Web")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:dd/MM/yyyy HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.Async(writeTo => writeTo.File(
                formatter: new CompactJsonFormatter(),
                path: Path.Combine(logsPath, "application-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedFileCountLimit,
                fileSizeLimitBytes: fileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                shared: true))
                .WriteTo.Async(writeTo => writeTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: Path.Combine(logsPath, "error-.log"),
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    retainedFileCountLimit: retainedFileCountLimit,
                    fileSizeLimitBytes: fileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    shared: true));
        },
        preserveStaticLogger: builder.Environment.IsEnvironment("Testing"));

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new RequestCulture(vietnameseCulture);
        options.SupportedCultures = [vietnameseCulture];
        options.SupportedUICultures = [vietnameseCulture];
    });
    builder.Services.AddProblemDetails();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<HrmResilienceMetrics>();
    builder.Services.AddExceptionHandler<HrmExceptionHandler>();
    builder.Services.AddSignalR();
    builder.Services.Configure<GatewayInboundSecurityOptions>(
        builder.Configuration.GetSection(GatewayInboundSecurityOptions.SectionName));
    builder.Services.AddSingleton<GatewayInboundReplayStore>();
    builder.Services.AddScoped<GatewayInboundHmacEndpointFilter>();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = (context, _) =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Security.RateLimiting");
            var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                : (int?)null;

            if (retryAfterSeconds.HasValue)
            {
                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString();
            }

            logger.LogWarning(
                "Security rate limit rejected request. Path={Path} Method={Method} RetryAfterSeconds={RetryAfterSeconds}",
                context.HttpContext.Request.Path,
                context.HttpContext.Request.Method,
                retryAfterSeconds);
            return ValueTask.CompletedTask;
        };
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            if (!httpContext.Request.Path.StartsWithSegments("/Account/Login"))
            {
                return RateLimitPartition.GetNoLimiter("default");
            }

            var clientKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                clientKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
        options.AddFixedWindowLimiter("gateway-inbound", limiterOptions =>
        {
            limiterOptions.PermitLimit = 120;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
            limiterOptions.AutoReplenishment = true;
        });
    });

    builder.Services.AddAppServices();
    builder.Services.Configure<AdmsGatewayMonitorOptions>(builder.Configuration.GetSection("AdmsGateway"));
    builder.Services.AddSingleton<IAdmsMonitorEventPublisher, AdmsMonitorEventPublisher>();

    var azureOpenAIEndpoint = builder.Configuration.GetSection("AIIntegrationSettings")["EndpointUrl"];
    var azureOpenAIKey = builder.Configuration.GetSection("AIIntegrationSettings")["Key"];
    var deploymentName = builder.Configuration.GetSection("AIIntegrationSettings")["DeploymentName"];

    if (!string.IsNullOrWhiteSpace(azureOpenAIEndpoint)
        && !string.IsNullOrWhiteSpace(azureOpenAIKey)
        && !string.IsNullOrWhiteSpace(deploymentName))
    {
        builder.Services.AddChatClient(azureOpenAIEndpoint, azureOpenAIKey, deploymentName);
    }

    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<IdentityUserAccessor>();
    builder.Services.AddScoped<IdentityRedirectManager>();
    builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();
    builder.Services.AddScoped<CookieEvents>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddTransient<AuthenticatedApiCookieHandler>();
    builder.Services.AddHttpClient();
    builder.Services
        .AddHttpClient("VntaHrmAuthenticatedApi")
        .AddHttpMessageHandler<AuthenticatedApiCookieHandler>();
    builder.Services.ConfigureApplicationCookie(o =>
    {
        o.EventsType = typeof(CookieEvents);
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        // LAN Debug uses HTTP so other machines can access the development host by IP.
        // Keep authentication cookies HTTPS-only in every non-development environment.
        o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(
            InternalAccountPolicies.AuditRead,
            policy => policy.RequireAssertion(context =>
                InternalAccountCapabilityResolver.HasCapability(context.User, InternalAccountCapabilities.AuditRead)));
        options.AddPolicy(
            InternalAccountPolicies.AuditSensitiveRead,
            policy => policy.RequireAssertion(context =>
                InternalAccountCapabilityResolver.HasCapability(context.User, InternalAccountCapabilities.AuditSensitiveRead)));
        options.AddPolicy(
            InternalAccountPolicies.EmployeeAccountAdministration,
            policy => policy.RequireAssertion(context =>
                InternalAccountCapabilityResolver.HasCapability(context.User, InternalAccountCapabilities.EmployeeAccountsOpen)));
        options.AddPolicy(
            InternalAccountPolicies.EmployeeAccountApproval,
            policy => policy.RequireAssertion(context =>
                InternalAccountCapabilityResolver.HasCapability(context.User, InternalAccountCapabilities.EmployeeAccountsApprove)));
        options.AddPolicy(
            InternalAccountPolicies.HumanResourcesAdministration,
            policy => policy.RequireRole(InternalAccountRoles.HumanResourcesRoles.ToArray()));
        options.AddPolicy(
            InternalAccountPolicies.ShiftManagement,
            policy => policy.RequireRole(InternalAccountRoles.ShiftManagementRoles.ToArray()));
        options.AddPolicy(
            InternalAccountPolicies.AttendanceAdministration,
            policy => policy.RequireRole(InternalAccountRoles.AttendanceAdministrationRoles.ToArray()));
        options.AddPolicy(
            InternalAccountPolicies.PayrollAdministration,
            policy => policy.RequireRole(InternalAccountRoles.PayrollAdministrationRoles.ToArray()));
        options.AddPolicy(
            InternalAccountPolicies.ManageAttendanceAllowance,
            policy => policy.RequireRole(InternalAccountRoles.PayrollAdministrationRoles.ToArray()));
        options.AddPolicy(
            InternalAccountPolicies.DeviceAdministration,
            policy => policy.RequireRole(InternalAccountRoles.DeviceAdministrationRoles.ToArray()));
    });

    var configuration = builder.Configuration;
    builder.Services.AddInfrastructureServices(configuration);
    builder.Services.AddOptions<HazardAllowanceExportJobOptions>()
        .Bind(configuration.GetSection(HazardAllowanceExportJobOptions.SectionName));
    builder.Services.AddHostedService<HazardAllowanceExportJobWorker>();
    builder.Services.AddScoped<IInteractiveAuditCommandScopeFactory, InteractiveAuditCommandScopeFactory>();
    builder.Services.AddScoped<IPayrollAdministrationAuthorizer, PayrollAdministrationAuthorizer>();
    builder.Services.AddScoped<IAttendanceMonthlyWorkReadAuthorizer, AttendanceMonthlyWorkReadAuthorizer>();
    builder.Services.AddScoped<IEmployeeApiService, ServerNhanVienApiService>();
    builder.Services.AddScoped<IChiTietNhanVienApiService, ServerChiTietNhanVienApiService>();
    builder.Services.AddScoped<IAttendanceLogReadService, DatabaseAttendanceLogReadService>();
    builder.Services.AddScoped<IAttendanceMonthlyWorkSummaryGridReadService, DatabaseAttendanceMonthlyWorkSummaryGridReadService>();
    builder.Services.AddScoped<IAttendanceWorkdaySummaryReadService, DatabaseAttendanceWorkdaySummaryReadService>();
    builder.Services.AddScoped<IAttendanceWorkdaySummaryService, DatabaseAttendanceWorkdaySummaryService>();
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.WebHost.UseStaticWebAssets();

    var app = builder.Build();

    await TryApplyDevelopmentMigrationsAsync(app);

    var enableSchemaGuards = app.Environment.IsDevelopment()
        && configuration.GetValue<bool>("DatabaseStartup:EnableSchemaGuards");
    if (enableSchemaGuards)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await ApplicationSchemaGuard.EnsureEmployeeAvatarColumnAsync(dbContext, CancellationToken.None);
        var employeeCodeGuardResult = await ApplicationSchemaGuard.EnsureUniqueActiveEmployeeCodesAsync(dbContext, CancellationToken.None);
        if (employeeCodeGuardResult.DuplicateGroupCount > 0)
        {
            Log.Warning(
                "Chưa thể khóa unique EmployeeCode cho employees vì còn {DuplicateGroupCount} nhóm mã trùng đang hoạt động. Mẫu mã: {DuplicateCodes}",
                employeeCodeGuardResult.DuplicateGroupCount,
                string.Join(", ", employeeCodeGuardResult.DuplicateCodes));
        }
        else if (employeeCodeGuardResult.HasChanges)
        {
            Log.Information(
                "Đã chuẩn hóa EmployeeCode khi khởi động HRM. NormalizedRows={NormalizedRows}, UniqueIndexEnsured={UniqueIndexEnsured}",
                employeeCodeGuardResult.NormalizedRowCount,
                employeeCodeGuardResult.UniqueIndexEnsured);
        }

        var serialGuardResult = await ApplicationSchemaGuard.EnsureUniqueDeviceSerialNumbersAsync(dbContext, CancellationToken.None);
        if (serialGuardResult.HasChanges)
        {
            Log.Information(
                "Chuẩn hóa dữ liệu serial máy chấm công khi khởi động HRM. NormalizedRows={NormalizedRows}, DuplicateGroups={DuplicateGroups}, DeletedDevices={DeletedDevices}, RemappedAttendanceLogs={RemappedAttendanceLogs}",
                serialGuardResult.NormalizedRowCount,
                serialGuardResult.DuplicateGroupCount,
                serialGuardResult.DeletedDeviceCount,
                serialGuardResult.RemappedAttendanceLogCount);
        }
    }

    string? pathBase = configuration.GetValue<string>("pathbase");
    if (!string.IsNullOrEmpty(pathBase))
    {
        string pathString = pathBase.StartsWith('/') ? pathBase : "/" + pathBase;
        app.UsePathBase(pathString);
    }

    app.UseExceptionHandler();
    app.UseRequestLocalization();

    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseHsts();
    }

    app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd("X-Trace-Id", context.TraceIdentifier);
            return Task.CompletedTask;
        });
        await next();
    });
    app.UseMiddleware<AuditRequestContextMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.UseRateLimiter();

    if (!string.IsNullOrWhiteSpace(azureOpenAIEndpoint)
        && !string.IsNullOrWhiteSpace(azureOpenAIKey)
        && !string.IsNullOrWhiteSpace(deploymentName))
    {
        app.MapPost("/api/chat/{*path}", async (string path, HttpContext context, CancellationToken ct) =>
        {
            var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes("Not authorized"), ct);
                return;
            }

            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new(azureOpenAIEndpoint);
            client.DefaultRequestHeaders.Authorization = new("Bearer", azureOpenAIKey);

            var newPath = path.Replace("proxychat", deploymentName);
            var endpointUri = new Uri(azureOpenAIEndpoint);
            var uriBuilder = new UriBuilder(endpointUri)
            {
                Path = $"{endpointUri.AbsolutePath}/{newPath}",
                Query = context.Request.QueryString.Value
            };
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync(ct);

            var response = await client.PostAsync(uriBuilder.Uri, new StringContent(body, Encoding.UTF8, "application/json"), ct);
            context.Response.StatusCode = (int)response.StatusCode;
            await response.Content.CopyToAsync(context.Response.Body, ct);
        }).RequireAuthorization();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapStaticAssets();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(Vnta.Hrm.Web.Client.Components._Imports).Assembly);

    app.MapAdditionalIdentityEndpoints();
    app.MapAttendanceGatewayIntegrationEndpoints();
    app.MapChiTietNhanVienEndpoints();
    app.MapNhanVienEndpoints();
    app.MapPayrollEndpoints();
    app.MapTaiKhoanNhanVienEndpoints();
    app.MapAuditTrailEndpoints();
    // Keep malformed or retired API paths out of the Razor-components fallback,
    // which otherwise reports a method mismatch (405) for a POST to an unknown API.
    app.MapFallback("/api/{**path}", () => Results.NotFound());
    app.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
        .AllowAnonymous();
    app.MapGet("/health/ready", async (ApplicationDbContext dbContext, HttpContext context, CancellationToken cancellationToken) =>
    {
        try
        {
            if (await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                return Results.Ok(new { status = "ready" });
            }
        }
        catch (Exception ex)
        {
            context.RequestServices.GetRequiredService<HrmResilienceMetrics>()
                .RecordReadinessFailure();
            context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Health.Readiness")
                .LogWarning(ex, "Readiness check failed. TraceId={TraceId}", context.TraceIdentifier);
        }

        return Results.Problem(
            title: "Dịch vụ dữ liệu chưa sẵn sàng.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "dependency-unavailable",
                ["traceId"] = context.TraceIdentifier
            });
    }).AllowAnonymous();
    app.MapHub<AdmsMonitorHub>("/hubs/adms-monitor")
        .RequireAuthorization(InternalAccountPolicies.DeviceAdministration);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Ứng dụng HRM dừng đột ngột trong lúc khởi động");
    throw;
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

static async Task TryApplyDevelopmentMigrationsAsync(WebApplication app)
{
    if (!app.Environment.IsDevelopment())
    {
        return;
    }

    var autoMigrateOnStartup = app.Configuration.GetValue<bool>("DatabaseStartup:AutoMigrateOnStartup");
    if (!autoMigrateOnStartup)
    {
        app.Logger.LogInformation("Bo qua tu dong migrate database vi DatabaseStartup:AutoMigrateOnStartup = false.");
        return;
    }

    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();

        if (pendingMigrations.Length == 0)
        {
            app.Logger.LogInformation("Khong co migration nao dang cho. Database da dong bo.");
            return;
        }

        app.Logger.LogInformation(
            "Dang tu dong ap {MigrationCount} migration cho moi truong Development: {Migrations}",
            pendingMigrations.Length,
            string.Join(", ", pendingMigrations));

        await dbContext.Database.MigrateAsync();

        app.Logger.LogInformation("Da tu dong ap migration thanh cong cho moi truong Development.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Khong the tu dong migrate database trong moi truong Development. Ung dung se tiep tuc khoi dong.");
    }
}

public partial class Program
{
}
