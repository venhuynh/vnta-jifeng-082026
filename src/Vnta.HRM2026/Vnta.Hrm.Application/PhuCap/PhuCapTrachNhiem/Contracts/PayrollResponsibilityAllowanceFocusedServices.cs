namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Cung cấp các use case cấu hình bậc trách nhiệm và ánh xạ chức vụ theo kỳ lương.
/// </summary>
public interface IPayrollResponsibilityAllowanceGradeConfigurationReadService
{
    Task<PayrollResponsibilityAllowanceGradeConfigDto> GetGradeConfigAsync(int year, int month, CancellationToken cancellationToken = default);
}

public interface IPayrollResponsibilityAllowanceGradeConfigurationWriteService
{
    Task<PayrollResponsibilityAllowanceGradeDto> SaveGradeAsync(SavePayrollResponsibilityAllowanceGradeRequest request, CancellationToken cancellationToken = default);
    Task<PayrollResponsibilityAllowanceGradePositionDto> SaveMappingAsync(SavePayrollResponsibilityAllowanceGradePositionRequest request, CancellationToken cancellationToken = default);
    Task<PayrollResponsibilityAllowanceGradePositionDto> DeactivateMappingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PayrollResponsibilityAllowanceConfigCopyResult> CopyFromPreviousMonthAsync(int year, int month, bool copyMappings, CancellationToken cancellationToken = default);
}

[Obsolete("Use the focused grade configuration read/write capabilities; remove after legacy consumers are retired.")]
public interface IPayrollResponsibilityAllowanceGradeConfigurationService :
    IPayrollResponsibilityAllowanceGradeConfigurationReadService,
    IPayrollResponsibilityAllowanceGradeConfigurationWriteService
{
}

/// <summary>
/// Cung cấp các use case gán phụ cấp trách nhiệm theo từng nhân viên.
/// </summary>
public interface IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService
{
    Task<PayrollResponsibilityAllowanceEmployeeAssignmentDto> SaveEmployeeAssignmentAsync(SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> EnsureEmployeeAssignmentsForSummariesAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> LoadEmployeeAssignmentsFromPreviousMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> RecalculateEmployeeAssignmentsAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> ApplyPositionDefaultsToEmployeeAssignmentsAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult> UpdateAndRefreshEmployeeAssignmentAsync(UpdatePayrollResponsibilityAllowanceEmployeeAssignmentRequest request, CancellationToken cancellationToken = default);
}

[Obsolete("Use IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService; remove after legacy consumers are retired.")]
public interface IPayrollResponsibilityAllowanceEmployeeAssignmentService : IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService
{
}

/// <summary>Read model của màn hình gán cấp bậc nhân viên; không trả toàn bộ cấu hình kỳ.</summary>
public interface IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService
{
    Task<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto> SearchEmployeeAssignmentsAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentQuery query,
        CancellationToken cancellationToken = default);
}

public interface IPayrollResponsibilityAllowanceEmployeeAssignmentExportService
{
    Task<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>> ExportEmployeeAssignmentsAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest request,
        CancellationToken cancellationToken = default);
}

[Obsolete("Use the focused employee assignment query/export capabilities; remove after legacy consumers are retired.")]
public interface IPayrollResponsibilityAllowanceEmployeeAssignmentReadService :
    IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService,
    IPayrollResponsibilityAllowanceEmployeeAssignmentExportService
{
}

/// <summary>
/// Chỉ đọc snapshot ABC và ngữ cảnh điều chỉnh; không được phát sinh thay đổi dữ liệu.
/// </summary>
public interface IPayrollResponsibilityAllowanceMonthlyAbcQueryService
{
    Task<PayrollResponsibilityAllowanceAbcPageDto> SearchAbcAsync(
        PayrollResponsibilityAllowanceAbcQuery query,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto>> GetAbcAsync(PayrollResponsibilityAllowanceAbcFilter filter, CancellationToken cancellationToken = default);
    Task<PayrollResponsibilityAllowanceUpdateContextDto> GetUpdateContextAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
}

public interface IPayrollResponsibilityAllowanceMonthlyAbcExportService
{
    Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>> ExportAsync(
        PayrollResponsibilityAllowanceAbcExportRequest request,
        CancellationToken cancellationToken = default);
}

public interface IPayrollResponsibilityAllowanceMonthlyAbcRefreshService
{
    Task<RefreshPayrollResponsibilityAllowanceAbcResult> RefreshAbcAsync(RefreshPayrollResponsibilityAllowanceAbcRequest request, CancellationToken cancellationToken = default);
    Task<CalculatePayrollResponsibilityAllowanceAbcResult> CalculateAbcAsync(RefreshPayrollResponsibilityAllowanceAbcRequest request, CancellationToken cancellationToken = default);
}

public interface IPayrollResponsibilityAllowanceMonthlyAbcCopyService
{
    Task<CopyPayrollResponsibilityAllowanceAbcFromPreviousResult> CopyAbcFromPreviousMonthAsync(int year, int month, CancellationToken cancellationToken = default);
}

public interface IPayrollResponsibilityAllowanceMonthlyAbcLockService
{
    Task<PayrollResponsibilityAllowanceAbcItemDto> SetLockStateAsync(Guid employeeId, int year, int month, bool isLocked, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default);
    Task<SetPayrollResponsibilityAllowanceAbcBatchLockStateResult> SetLockStateBatchAsync(SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest request, CancellationToken cancellationToken = default);
}

public interface IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService
{
    Task<PayrollResponsibilityAllowanceAbcItemDto> SaveAdjustmentAsync(SavePayrollResponsibilityAllowanceAdjustmentRequest request, CancellationToken cancellationToken = default);
}

public interface IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService
{
    Task<PayrollResponsibilityAllowanceAbcItemDto> UpdatePerformanceBonusAsync(Guid employeeId, int year, int month, decimal monthlyPerformanceBonusAmount, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default);
    Task<PayrollResponsibilityAllowanceAbcItemDto> UpdatePerformanceBonusExclusionAsync(Guid employeeId, int year, int month, bool isPerformanceBonusExcluded, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default);
    Task<UpdatePayrollResponsibilityPerformanceBonusForPeriodResult> UpdatePerformanceBonusForPeriodAsync(int year, int month, decimal monthlyPerformanceBonusAmount, IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? concurrencyTokens, CancellationToken cancellationToken = default);
}

/// <summary>
/// Thực thi các command làm thay đổi snapshot ABC của phụ cấp trách nhiệm.
/// </summary>
public interface IPayrollResponsibilityAllowanceMonthlyAbcCommandService :
    IPayrollResponsibilityAllowanceMonthlyAbcRefreshService,
    IPayrollResponsibilityAllowanceMonthlyAbcCopyService,
    IPayrollResponsibilityAllowanceMonthlyAbcLockService,
    IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService,
    IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService
{
    Task<PayrollResponsibilityAllowanceAbcItemDto> SetLockStateAsync(Guid employeeId, int year, int month, bool isLocked, CancellationToken cancellationToken = default) =>
        SetLockStateAsync(employeeId, year, month, isLocked, null, cancellationToken);
    Task<PayrollResponsibilityAllowanceAbcItemDto> UpdatePerformanceBonusAsync(Guid employeeId, int year, int month, decimal monthlyPerformanceBonusAmount, CancellationToken cancellationToken = default) =>
        UpdatePerformanceBonusAsync(employeeId, year, month, monthlyPerformanceBonusAmount, null, cancellationToken);
    Task<PayrollResponsibilityAllowanceAbcItemDto> UpdatePerformanceBonusExclusionAsync(Guid employeeId, int year, int month, bool isPerformanceBonusExcluded, CancellationToken cancellationToken = default) =>
        UpdatePerformanceBonusExclusionAsync(employeeId, year, month, isPerformanceBonusExcluded, null, cancellationToken);
    Task<UpdatePayrollResponsibilityPerformanceBonusForPeriodResult> UpdatePerformanceBonusForPeriodAsync(int year, int month, decimal monthlyPerformanceBonusAmount, CancellationToken cancellationToken = default) =>
        UpdatePerformanceBonusForPeriodAsync(year, month, monthlyPerformanceBonusAmount, null, cancellationToken);
}
