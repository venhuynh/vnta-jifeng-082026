using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Application.QuanTri.TaiKhoanNhanVien;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Integrations.Payroll;
using Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.DependencyInjection;

namespace Vnta.Hrm.Infrastructure;

using Data;
using Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dataProtection = services
            .AddDataProtection()
            .SetApplicationName("Vnta.Hrm");
        var keyRingPath = configuration["DataProtection:KeyRingPath"];

        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            var keyRingDirectory = new DirectoryInfo(keyRingPath);
            keyRingDirectory.Create();
            dataProtection.PersistKeysToFileSystem(keyRingDirectory);
        }

        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("VNTA_DB");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing database connection string. Configure ConnectionStrings:Postgres or VNTA_DB outside source control.");
        }

        // The scopes keep their data in AsyncLocal, so a singleton is safe and keeps one
        // logical audit context across the request or Interactive Server command.
        services.AddSingleton<IAuditScope, AsyncLocalAuditScope>();
        services.AddSingleton<IAuditCorrelationScope, AsyncLocalAuditCorrelationScope>();
        services.AddSingleton<IAuditCorrelationAccessor>(serviceProvider =>
            serviceProvider.GetRequiredService<IAuditCorrelationScope>());
        services.AddSingleton<IAuditPolicy, AuditPolicy>();
        services.AddSingleton<AuditSaveChangesInterceptor>();
        services.AddScoped<IAuditedMutation, AuditedMutation>();
        services.AddScoped<IAuditTrailQueryService, DatabaseAuditTrailQueryService>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.Password.RequiredLength = 9;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
        services.Configure<AttendanceWorkdaySummaryOptions>(
            configuration.GetSection(AttendanceWorkdaySummaryOptions.SectionName));
        services.AddAttendanceGatewayIntegration();
        services.AddPayrollIntegration(configuration);
        services.AddPhuCapPhepLe();
        services.AddScoped<IEmployeeAccountService, DatabaseEmployeeAccountService>();

        return services;
    }
}
