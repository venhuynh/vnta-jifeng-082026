using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapThamNien;

/// <summary>
/// Adapter giữa contract phụ cấp thâm niên và mô hình hiển thị của lưới.
/// Adapter này không xác định quy tắc tính; mọi quyết định nghiệp vụ vẫn thuộc service phía máy chủ.
/// </summary>
public sealed class PayrollEmployeeSeniorityAllowanceDataProvider(
    IPayrollEmployeeSeniorityAllowanceReadService readService,
    IPayrollEmployeeSeniorityAllowanceRangeSummaryService rangeSummaryService,
    IPayrollEmployeeSeniorityAllowancePeriodPreparationService periodPreparationService,
    IPayrollEmployeeSeniorityAllowanceRefreshService refreshService,
    IPayrollEmployeeSeniorityAllowanceManualAdjustmentService manualAdjustmentService,
    IPayrollEmployeeSeniorityAllowanceLockService lockService,
    IPayrollAdministrationAuthorizer payrollAdministrationAuthorizer,
    IInteractiveAuditCommandScopeFactory auditCommandScopeFactory)
    : IPayrollEmployeeSeniorityAllowanceDataProvider
{
    private const int ExportPageSize = 5000;

    public async Task PreparePeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.SeniorityAllowance.PreparePeriod,
            token => periodPreparationService.PreparePeriodAsync(year, month, token),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<PhuCapThamNienRecord>> SearchAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var result = await readService.SearchAsync(filter, cancellationToken);
        return result.Select(PayrollEmployeeSeniorityAllowanceRecordMapper.Map).ToArray();
    }

    public async Task<PhuCapThamNienPage> SearchPageAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = await readService.SearchPageAsync(filter, cancellationToken);
        return new PhuCapThamNienPage(
            page.Rows.Select(PayrollEmployeeSeniorityAllowanceRecordMapper.Map).ToArray(),
            page.TotalCount,
            page.TotalAllowanceAmount);
    }

    public Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>> LoadRangeSummariesAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        rangeSummaryService
            .GetRangeSummariesAsync(filter with { SeniorityRangeKey = null, Take = 1, Skip = 0 }, cancellationToken);

    public async Task<IReadOnlyList<PhuCapThamNienRecord>> LoadAllForPeriodExportAsync(
        int payrollYear,
        int payrollMonth,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<PhuCapThamNienRecord>();
        var totalCount = 0;

        do
        {
            var page = await readService.SearchPageAsync(
                new PayrollEmployeeSeniorityAllowanceFilter(
                    payrollMonth,
                    payrollYear,
                    Take: ExportPageSize,
                    Skip: rows.Count),
                cancellationToken);

            totalCount = page.TotalCount;
            var pageRows = page.Rows.Select(PayrollEmployeeSeniorityAllowanceRecordMapper.Map).ToArray();
            if(pageRows.Length == 0 && rows.Count < totalCount)
            {
                throw new InvalidOperationException("Không thể tải đầy đủ dữ liệu phụ cấp thâm niên để xuất file.");
            }

            rows.AddRange(pageRows);
        }
        while(rows.Count < totalCount);

        return rows;
    }

    /// <summary>
    /// Chuyển phạm vi tính lại sang server nguyên vẹn; client không được tự tính công hoặc số tiền phụ cấp.
    /// </summary>
    public async Task<RefreshPayrollEmployeeSeniorityAllowanceResult> RefreshAsync(
        RefreshPayrollEmployeeSeniorityAllowanceRequest request,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.SeniorityAllowance.Refresh,
            token => refreshService.RefreshAsync(request, token),
            cancellationToken: cancellationToken);
    }

    public async Task<PhuCapThamNienRecord> SetLockStateAsync(
        Guid payrollAllowanceSummaryRecordId,
        bool isLocked,
        DateTime originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var result = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.SeniorityAllowance.LockStateChanged,
            token => lockService.SetLockStateAsync(
                new SetPayrollEmployeeSeniorityAllowanceLockStateRequest(
                    payrollAllowanceSummaryRecordId,
                    isLocked,
                    originalUpdatedAtUtc),
                token),
            cancellationToken: cancellationToken);

        return PayrollEmployeeSeniorityAllowanceRecordMapper.Map(result);
    }

    public async Task<SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.SeniorityAllowance.BatchLockStateChanged,
            token => lockService.SetLockStateBatchAsync(request, token),
            cancellationToken: cancellationToken);
    }

    public async Task<PhuCapThamNienRecord> UpdateManualValuesAsync(
        Guid payrollAllowanceSummaryRecordId,
        decimal allowanceAmount,
        string? note,
        DateTime originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var result = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.SeniorityAllowance.ManualValueUpdated,
            token => manualAdjustmentService.UpdateManualValuesAsync(
                new UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest(
                    payrollAllowanceSummaryRecordId,
                    allowanceAmount,
                    note,
                    originalUpdatedAtUtc),
                token),
            cancellationToken: cancellationToken);

        return PayrollEmployeeSeniorityAllowanceRecordMapper.Map(result);
    }

}
