using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Application.ChamCong.DashboardBangChamCong;
using Vnta.Hrm.Infrastructure.ChamCong.DashboardBangChamCong;
using Vnta.Hrm.Infrastructure.NhanSu.ChiTietNhanVien;

namespace Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

public static class AttendanceGatewayIntegrationModule
{
    public static IServiceCollection AddAttendanceGatewayIntegration(this IServiceCollection services)
    {
        services.AddSingleton<AdmsMonitorMemoryStore>();
        services.AddSingleton<AttendanceBiometricDataRefreshProgressTracker>();
        services.AddSingleton<IAdmsMonitorRuntimeState, AdmsMonitorRuntimeState>();
        services.AddSingleton<IAdmsMonitorReadService, AdmsMonitorReadService>();
        services.AddScoped<DatabaseEmployeeService>();
        services.AddScoped<IEmployeeService>(serviceProvider => serviceProvider.GetRequiredService<DatabaseEmployeeService>());
        services.AddScoped<INhanVienListReadService>(serviceProvider => serviceProvider.GetRequiredService<DatabaseEmployeeService>());
        services.AddScoped<INhanVienSummaryReadService>(serviceProvider => serviceProvider.GetRequiredService<DatabaseEmployeeService>());
        services.AddScoped<INhanVienCreateService>(serviceProvider => serviceProvider.GetRequiredService<DatabaseEmployeeService>());
        services.AddScoped<INhanVienEditService>(serviceProvider => serviceProvider.GetRequiredService<DatabaseEmployeeService>());
        services.AddScoped<INhanVienDeleteService>(serviceProvider => serviceProvider.GetRequiredService<DatabaseEmployeeService>());
        services.AddScoped<INhanVienStatusService>(serviceProvider => serviceProvider.GetRequiredService<DatabaseEmployeeService>());
        services.AddScoped<INhanVienExportReadService>(serviceProvider => serviceProvider.GetRequiredService<DatabaseEmployeeService>());
        services.AddScoped<INhanSuWorkbookPreviewService, DatabaseNhanSuWorkbookPreviewService>();
        services.AddScoped<IChiTietNhanVienService, DatabaseChiTietNhanVienService>();
        services.AddScoped<DatabaseEmployeeRefreshService>();
        services.AddScoped<IEmployeeRefreshService>(serviceProvider => serviceProvider.GetRequiredService<DatabaseEmployeeRefreshService>());
        services.AddScoped<INhanVienRefreshService>(serviceProvider => serviceProvider.GetRequiredService<DatabaseEmployeeRefreshService>());
        services.AddScoped<IAttendanceDepartmentService, DatabaseAttendanceDepartmentService>();
        services.AddScoped<IAttendanceDeviceService, DatabaseAttendanceDeviceService>();
        services.AddScoped<IAttendancePositionService, DatabaseAttendancePositionService>();
        services.AddScoped<IAttendanceShiftService, DatabaseAttendanceShiftService>();
        services.AddScoped<IAttendanceShiftAssignmentReadService, DatabaseAttendanceShiftAssignmentReadService>();
        services.AddScoped<IAttendanceShiftAssignmentEnsureService, DatabaseAttendanceShiftAssignmentEnsureService>();
        services.AddScoped<IAttendanceShiftAssignmentManualEditService, DatabaseAttendanceShiftAssignmentManualEditService>();
        services.AddScoped<IShiftSchedulingSettingService, DatabaseShiftSchedulingSettingService>();
        services.AddScoped<IAttendanceStatusCodeService, DatabaseAttendanceStatusCodeService>();
        // Cùng contract được endpoint HTTP và component Interactive Server dùng để đọc lịch làm việc.
        services.AddScoped<IAttendanceWorkCalendarService, DatabaseAttendanceWorkCalendarService>();
        services.AddScoped<IAdmsDeviceCommandService, DatabaseAdmsDeviceCommandService>();
        services.AddScoped<IAttendanceBiometricDeviceQueueService, DatabaseAttendanceBiometricDeviceQueueService>();
        services.AddScoped<IAttendanceLogReadService, DatabaseAttendanceLogReadService>();
        services.AddScoped<IAttendanceBiometricDataReadService, DatabaseAttendanceBiometricDataReadService>();
        services.AddScoped<IAttendanceBiometricDataRefreshService, DatabaseAttendanceBiometricDataRefreshService>();
        services.AddScoped<IAttendanceDailySummaryReadService, DatabaseAttendanceDailySummaryReadService>();
        services.AddScoped<IAttendanceDailySummaryService, DatabaseAttendanceDailySummaryService>();
        services.AddScoped<IOvertimeRegistrationService, DatabaseOvertimeRegistrationService>();
        services.AddScoped<IAttendanceWorkdaySummaryReadService, DatabaseAttendanceWorkdaySummaryReadService>();
        services.AddScoped<IAttendanceWorkdaySummaryService, DatabaseAttendanceWorkdaySummaryService>();
        services.AddScoped<IAttendanceTimesheetDashboardService, DatabaseAttendanceTimesheetDashboardService>();
        services.AddSingleton<IAttendanceGatewayInboundService, AttendanceGatewayInboundService>();
        services.TryAddSingleton<IAdmsMonitorEventPublisher, NullAdmsMonitorEventPublisher>();
        return services;
    }
}
