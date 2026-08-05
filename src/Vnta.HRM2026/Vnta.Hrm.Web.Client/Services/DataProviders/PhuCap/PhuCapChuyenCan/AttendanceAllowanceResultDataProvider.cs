using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapChuyenCan;

/// <summary>
/// Adapter của màn Phụ cấp chuyên cần Interactive Server. Provider chỉ điều phối
/// view-state và audit scope, còn quy tắc lương và persistence thuộc server.
/// </summary>
public sealed class AttendanceAllowanceResultDataProvider(
    IAttendanceAllowanceReadService attendanceAllowanceReadService,
    IAttendanceAllowanceExportService exportService,
    IAttendanceAllowanceRefreshService refreshService,
    IAttendanceAllowanceManualAdjustmentService manualAdjustmentService,
    IAttendanceAllowanceLockService lockService,
    IInteractiveAuditCommandScopeFactory auditCommandScopeFactory)
    : IAttendanceAllowanceResultDataProvider
{
    /// <summary>
    /// Đọc danh sách mã CTL đã được cấu hình từ server cho popup quy tắc.
    /// </summary>
    public Task<AttendanceAllowanceRuleDto> GetRuleAsync(
        CancellationToken cancellationToken = default) =>
        attendanceAllowanceReadService.GetRuleAsync(cancellationToken);

    /// <summary>
    /// Server sở hữu paging và tổng số bản ghi; adapter chỉ đổi DTO thành record hiển thị.
    /// </summary>
    public async Task<AttendanceAllowanceResultLoadResult> SearchPageAsync(
        AttendanceAllowanceResultFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = await attendanceAllowanceReadService.SearchPageAsync(filter, cancellationToken);
        return new AttendanceAllowanceResultLoadResult(
            page.Rows.Select(AttendanceAllowanceResultRecordMapper.MapRecord).ToArray(),
            page.TotalCount,
            page.OpenCount,
            page.LockedCount,
            page.AttendanceClassACount,
            page.AttendanceClassBCount,
            page.AttendanceClassCCount,
            page.PeriodTotalCount,
            page.PeriodCanLockCount,
            page.PeriodCanUnlockCount,
            page.PeriodSummaryLockedCount);
    }

    /// <summary>Tải allowlist toàn kỳ đã áp dụng để xuất; backend xác nhận scope và ghi audit.</summary>
    public Task<IReadOnlyList<AttendanceAllowanceExportRowDto>> ExportAsync(
        int payrollYear,
        int payrollMonth,
        AttendanceAllowanceExportFormat format,
        CancellationToken cancellationToken = default) =>
        auditCommandScopeFactory.ExecuteAsync(
            AuditActions.AttendanceAllowance.Exported,
            token => exportService.ExportAsync(
                new AttendanceAllowanceExportRequest(payrollYear, payrollMonth, format),
                token),
            cancellationToken: cancellationToken);

    public Task<RefreshAttendanceAllowanceResult> RefreshAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        CancellationToken cancellationToken = default) =>
        auditCommandScopeFactory.ExecuteAsync(
            AuditActions.AttendanceAllowance.Refresh,
            token => refreshService.RefreshAsync(
                new RefreshAttendanceAllowanceRequest(targetPayrollMonth, targetPayrollYear),
                token),
            cancellationToken: cancellationToken);

    public Task<RefreshAttendanceAllowanceResult> RefreshRowAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        Guid payrollAllowanceSummaryRecordId,
        CancellationToken cancellationToken = default) =>
        auditCommandScopeFactory.ExecuteAsync(
            AuditActions.AttendanceAllowance.Refresh,
            token => refreshService.RefreshAsync(
                new RefreshAttendanceAllowanceRequest(
                    targetPayrollMonth,
                    targetPayrollYear,
                    payrollAllowanceSummaryRecordId),
                token),
            cancellationToken: cancellationToken);

    public async Task<AttendanceAllowanceResultRecord> UpdateActualWorkdayAsync(
        Guid id,
        decimal actualWorkdayCount,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var updated = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.AttendanceAllowance.Save,
            token => manualAdjustmentService.UpdateActualWorkdayAsync(
                new UpdateAttendanceAllowanceActualWorkdayRequest(
                    id,
                    actualWorkdayCount,
                    originalUpdatedAtUtc),
                token),
            cancellationToken: cancellationToken);

        return AttendanceAllowanceResultRecordMapper.MapRecord(updated);
    }

    public async Task<AttendanceAllowanceResultRecord> UpdateStandardWorkdayAsync(
        Guid id,
        decimal standardWorkdayCount,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var updated = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.AttendanceAllowance.Save,
            token => manualAdjustmentService.UpdateStandardWorkdayAsync(
                new UpdateAttendanceAllowanceStandardWorkdayRequest(
                    id,
                    standardWorkdayCount,
                    originalUpdatedAtUtc),
                token),
            cancellationToken: cancellationToken);

        return AttendanceAllowanceResultRecordMapper.MapRecord(updated);
    }

    public Task<SetAttendanceAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        int payrollYear,
        int payrollMonth,
        bool isLocked,
        IEnumerable<AttendanceAllowanceResultRecord>? records = null,
        CancellationToken cancellationToken = default)
    {
        // null biểu thị thao tác toàn kỳ; mảng rỗng vẫn là tập dòng đã chọn rỗng để không vô tình khóa toàn kỳ.
        var targetRecords = records?
            .Where(record => record.Id != Guid.Empty)
            .DistinctBy(record => record.Id)
            .Where(record => record.IsLocked != isLocked)
            .ToArray();

        return auditCommandScopeFactory.ExecuteAsync(
            AuditActions.AttendanceAllowance.SetLockStateBatch,
            token => lockService.SetLockStateBatchAsync(
                new SetAttendanceAllowanceBatchLockStateRequest(
                    payrollYear,
                    payrollMonth,
                    isLocked,
                    targetRecords?.Select(record => record.Id).ToArray(),
                    targetRecords?
                        .Select(record => new AttendanceAllowanceLockItem(
                            record.Id,
                            record.UpdatedAtUtc))
                        .ToArray()),
                token),
            cancellationToken: cancellationToken);
    }

}

/// <summary>
/// Feature composition boundary for the attendance-allowance UI provider.
/// Keeping this registration next to the provider prevents the global client
/// composition root from owning feature-specific implementation details.
/// </summary>
public static class AttendanceAllowanceResultDataProviderServiceCollectionExtensions
{
    public static IServiceCollection AddAttendanceAllowanceResultDataProvider(this IServiceCollection services)
    {
        services.AddScoped<AttendanceAllowanceResultDataProvider>();
        services.AddScoped<IAttendanceAllowanceResultDataProvider>(sp =>
            sp.GetRequiredService<AttendanceAllowanceResultDataProvider>());
        services.AddScoped<IAttendanceAllowanceReadDataProvider>(sp =>
            sp.GetRequiredService<AttendanceAllowanceResultDataProvider>());
        services.AddScoped<IAttendanceAllowanceExportDataProvider>(sp =>
            sp.GetRequiredService<AttendanceAllowanceResultDataProvider>());
        services.AddScoped<IAttendanceAllowanceRefreshDataProvider>(sp =>
            sp.GetRequiredService<AttendanceAllowanceResultDataProvider>());
        services.AddScoped<IAttendanceAllowanceManualAdjustmentDataProvider>(sp =>
            sp.GetRequiredService<AttendanceAllowanceResultDataProvider>());
        services.AddScoped<IAttendanceAllowanceLockDataProvider>(sp =>
            sp.GetRequiredService<AttendanceAllowanceResultDataProvider>());
        return services;
    }
}
