namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Adapter giữ component phụ cấp trách nhiệm độc lập với HTTP transport.
/// Quy tắc nghiệp vụ và xác thực cuối cùng vẫn thuộc workflow service phía server.
/// </summary>
public sealed class PayrollResponsibilityAllowanceDataProvider(
    IPayrollResponsibilityAllowanceGradeConfigurationReadService gradeRead,
    IPayrollResponsibilityAllowanceGradeConfigurationWriteService gradeWrite,
    IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService assignmentCommand,
    IPayrollResponsibilityAllowanceMonthlyAbcQueryService abcQuery,
    IPayrollResponsibilityAllowanceMonthlyAbcRefreshService abcRefresh,
    IPayrollResponsibilityAllowanceRecalculationService abcRecalculate,
    IPayrollResponsibilityAllowanceMonthlyAbcCopyService abcCopy,
    IPayrollResponsibilityAllowanceMonthlyAbcLockService abcLock,
    IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService abcAdjustment,
    IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService performanceBonus)
{
    public Task<PayrollResponsibilityAllowanceGradeConfigDto> GetGradeConfigAsync(int year, int month, CancellationToken cancellationToken = default) =>
        gradeRead.GetGradeConfigAsync(year, month, cancellationToken);

    public Task<PayrollResponsibilityAllowanceGradeDto> SaveGradeAsync(SavePayrollResponsibilityAllowanceGradeRequest request, CancellationToken cancellationToken = default) =>
        gradeWrite.SaveGradeAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceGradePositionDto> SaveMappingAsync(SavePayrollResponsibilityAllowanceGradePositionRequest request, CancellationToken cancellationToken = default) =>
        gradeWrite.SaveMappingAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceGradePositionDto> DeactivateMappingAsync(Guid id, CancellationToken cancellationToken = default) =>
        gradeWrite.DeactivateMappingAsync(id, cancellationToken);

    public Task<PayrollResponsibilityAllowanceEmployeeAssignmentDto> SaveEmployeeAssignmentAsync(SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest request, CancellationToken cancellationToken = default) =>
        assignmentCommand.SaveEmployeeAssignmentAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> ApplyPositionDefaultsToEmployeeAssignmentsAsync(int year, int month, CancellationToken cancellationToken = default) =>
        assignmentCommand.ApplyPositionDefaultsToEmployeeAssignmentsAsync(year, month, cancellationToken);

    public Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto>> GetAbcAsync(PayrollResponsibilityAllowanceAbcFilter filter, CancellationToken cancellationToken = default) =>
        abcQuery.GetAbcAsync(filter, cancellationToken);

    public Task<RefreshPayrollResponsibilityAllowanceAbcResult> RefreshAbcAsync(RefreshPayrollResponsibilityAllowanceAbcRequest request, CancellationToken cancellationToken = default) =>
        abcRefresh.RefreshAbcAsync(request, cancellationToken);

    public Task<CalculatePayrollResponsibilityAllowanceAbcResult> CalculateAbcAsync(RefreshPayrollResponsibilityAllowanceAbcRequest request, CancellationToken cancellationToken = default) =>
        abcRefresh.CalculateAbcAsync(request, cancellationToken);

    public Task<RecalculatePayrollResponsibilityAllowanceAbcResult> RecalculateAbcAsync(RefreshPayrollResponsibilityAllowanceAbcRequest request, CancellationToken cancellationToken = default) =>
        abcRecalculate.RecalculateAbcAsync(request, cancellationToken);

    public Task<CopyPayrollResponsibilityAllowanceAbcFromPreviousResult> CopyAbcFromPreviousMonthAsync(int year, int month, CancellationToken cancellationToken = default) =>
        abcCopy.CopyAbcFromPreviousMonthAsync(year, month, cancellationToken);

    public Task<PayrollResponsibilityAllowanceAbcItemDto> SetLockStateAsync(Guid employeeId, int year, int month, bool isLocked, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default) =>
        abcLock.SetLockStateAsync(employeeId, year, month, isLocked, originalUpdatedAtUtc, cancellationToken);

    public Task<SetPayrollResponsibilityAllowanceAbcBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        abcLock.SetLockStateBatchAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceAbcItemDto> SaveAdjustmentAsync(SavePayrollResponsibilityAllowanceAdjustmentRequest request, CancellationToken cancellationToken = default) =>
        abcAdjustment.SaveAdjustmentAsync(request, cancellationToken);

    public Task<UpdatePayrollResponsibilityPerformanceBonusForPeriodResult> UpdatePerformanceBonusForPeriodAsync(
        int year,
        int month,
        decimal monthlyPerformanceBonusAmount,
        IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? concurrencyTokens,
        CancellationToken cancellationToken = default) =>
        performanceBonus.UpdatePerformanceBonusForPeriodAsync(
            year,
            month,
            monthlyPerformanceBonusAmount,
            concurrencyTokens,
            cancellationToken);

    public Task<PayrollResponsibilityAllowanceUpdateContextDto> GetUpdateContextAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default) =>
        abcQuery.GetUpdateContextAsync(employeeId, year, month, cancellationToken);
}
