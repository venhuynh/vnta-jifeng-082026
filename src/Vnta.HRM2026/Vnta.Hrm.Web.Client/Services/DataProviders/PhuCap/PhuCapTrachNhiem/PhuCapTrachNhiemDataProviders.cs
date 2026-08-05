namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Read boundary for the ABC list and its read-only adjustment context.
/// </summary>
public sealed class PhuCapTrachNhiemAbcQueryDataProvider(
    IPayrollResponsibilityAllowanceMonthlyAbcQueryService queryService,
    IPayrollResponsibilityAllowanceMonthlyAbcExportService exportService)
{
    public Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto>> LoadAllAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        queryService.GetAbcAsync(new PayrollResponsibilityAllowanceAbcFilter(year, month), cancellationToken);

    public Task<PayrollResponsibilityAllowanceAbcPageDto> SearchAsync(
        PayrollResponsibilityAllowanceAbcQuery query,
        CancellationToken cancellationToken = default) =>
        queryService.SearchAbcAsync(query, cancellationToken);

    public Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>> ExportAsync(
        PayrollResponsibilityAllowanceAbcExportRequest request,
        CancellationToken cancellationToken = default) =>
        exportService.ExportAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceUpdateContextDto> GetUpdateContextAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        queryService.GetUpdateContextAsync(employeeId, year, month, cancellationToken);
}
/// <summary>
/// Command boundary for ABC snapshot calculations, adjustments, locks and THS.
/// </summary>
public sealed class PhuCapTrachNhiemAbcCommandDataProvider(
    IPayrollResponsibilityAllowanceMonthlyAbcRefreshService refreshService,
    IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService adjustmentService,
    IPayrollResponsibilityAllowanceMonthlyAbcLockService lockService,
    IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService performanceBonusService,
    IPayrollResponsibilityAllowanceRecalculationService recalculationService)
{
    public Task<RefreshPayrollResponsibilityAllowanceAbcResult> RefreshAsync(
        RefreshPayrollResponsibilityAllowanceAbcRequest request,
        CancellationToken cancellationToken = default) =>
        refreshService.RefreshAbcAsync(request, cancellationToken);

    public Task<CalculatePayrollResponsibilityAllowanceAbcResult> CalculateAsync(
        RefreshPayrollResponsibilityAllowanceAbcRequest request,
        CancellationToken cancellationToken = default) =>
        refreshService.CalculateAbcAsync(request, cancellationToken);

    public Task<RecalculatePayrollResponsibilityAllowanceAbcResult> RecalculateAsync(
        RefreshPayrollResponsibilityAllowanceAbcRequest request,
        CancellationToken cancellationToken = default) =>
        recalculationService.RecalculateAbcAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceAbcItemDto> SaveAdjustmentAsync(
        SavePayrollResponsibilityAllowanceAdjustmentRequest request,
        CancellationToken cancellationToken = default) =>
        adjustmentService.SaveAdjustmentAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceAbcItemDto> SetLockStateAsync(
        Guid employeeId,
        int year,
        int month,
        bool isLocked,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default) =>
        lockService.SetLockStateAsync(employeeId, year, month, isLocked, originalUpdatedAtUtc, cancellationToken);

    public Task<SetPayrollResponsibilityAllowanceAbcBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        lockService.SetLockStateBatchAsync(request, cancellationToken);

    public Task<UpdatePayrollResponsibilityPerformanceBonusForPeriodResult> UpdatePerformanceBonusForPeriodAsync(
        int year,
        int month,
        decimal amount,
        IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? concurrencyTokens,
        CancellationToken cancellationToken = default) =>
        performanceBonusService.UpdatePerformanceBonusForPeriodAsync(year, month, amount, concurrencyTokens, cancellationToken);
}

/// <summary>
/// Command/query boundary for grade configuration and employee assignments.
/// </summary>
public sealed class PhuCapTrachNhiemConfigurationDataProvider(
    IPayrollResponsibilityAllowanceGradeConfigurationReadService gradeConfigurationReadService,
    IPayrollResponsibilityAllowanceGradeConfigurationWriteService gradeConfigurationWriteService,
    IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService employeeAssignmentService)
{
    public Task<PayrollResponsibilityAllowanceGradeConfigDto> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        gradeConfigurationReadService.GetGradeConfigAsync(year, month, cancellationToken);

    public Task<PayrollResponsibilityAllowanceGradeDto> SaveGradeAsync(
        SavePayrollResponsibilityAllowanceGradeRequest request,
        CancellationToken cancellationToken = default) =>
        gradeConfigurationWriteService.SaveGradeAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceGradePositionDto> SaveMappingAsync(
        SavePayrollResponsibilityAllowanceGradePositionRequest request,
        CancellationToken cancellationToken = default) =>
        gradeConfigurationWriteService.SaveMappingAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceGradePositionDto> DeactivateMappingAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        gradeConfigurationWriteService.DeactivateMappingAsync(id, cancellationToken);

    public Task<PayrollResponsibilityAllowanceConfigCopyResult> CopyFromPreviousMonthAsync(
        int year,
        int month,
        bool copyMappings,
        CancellationToken cancellationToken = default) =>
        gradeConfigurationWriteService.CopyFromPreviousMonthAsync(year, month, copyMappings, cancellationToken);

    public Task<PayrollResponsibilityAllowanceEmployeeAssignmentDto> SaveEmployeeAssignmentAsync(
        SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest request,
        CancellationToken cancellationToken = default) =>
        employeeAssignmentService.SaveEmployeeAssignmentAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> ApplyPositionDefaultsAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        employeeAssignmentService.ApplyPositionDefaultsToEmployeeAssignmentsAsync(year, month, cancellationToken);
}

