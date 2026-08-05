using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;

// Each EF Core entry point implements one application capability.  The inherited
// operations intentionally keep aggregate mutation helpers in one place so lock,
// audit, concurrency and downstream summary updates cannot drift between commands.
public abstract class PayrollResponsibilityAllowanceServiceBase(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger logger)
    : PayrollResponsibilityAllowancePersistenceOperations(
        dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger);

internal sealed class DatabasePayrollResponsibilityAllowanceGradeConfigurationReadService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceGradeConfigurationReadService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceGradeConfigurationReadService;

internal sealed class DatabasePayrollResponsibilityAllowanceGradeConfigurationWriteService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceGradeConfigurationWriteService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceGradeConfigurationWriteService;

internal sealed class DatabasePayrollResponsibilityAllowanceEmployeeAssignmentCommandService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceEmployeeAssignmentCommandService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService;

internal sealed class DatabasePayrollResponsibilityAllowanceEmployeeAssignmentReadService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceEmployeeAssignmentReadService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService,
        IPayrollResponsibilityAllowanceEmployeeAssignmentExportService;

internal sealed class DatabasePayrollResponsibilityAllowanceMonthlyAbcReadService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceMonthlyAbcReadService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceMonthlyAbcQueryService,
        IPayrollResponsibilityAllowanceMonthlyAbcExportService;

internal sealed class DatabasePayrollResponsibilityAllowanceMonthlyAbcRefreshCommandService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceMonthlyAbcRefreshCommandService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceMonthlyAbcRefreshService,
        IPayrollResponsibilityAllowanceRecalculationService;

internal sealed class DatabasePayrollResponsibilityAllowanceMonthlyAbcCopyCommandService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceMonthlyAbcCopyCommandService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceMonthlyAbcCopyService;

internal sealed class DatabasePayrollResponsibilityAllowanceMonthlyAbcLockCommandService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceMonthlyAbcLockCommandService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceMonthlyAbcLockService;

internal sealed class DatabasePayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentCommandService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentCommandService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService;

internal sealed class DatabasePayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusCommandService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusCommandService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService;

/// <summary>Legacy-only facade retained for direct callers during contract migration; it is not registered by the feature DI extension.</summary>
[Obsolete("Use focused responsibility allowance service contracts.")]
public sealed class DatabasePayrollResponsibilityAllowanceWorkflowService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger<DatabasePayrollResponsibilityAllowanceWorkflowService> logger)
    : PayrollResponsibilityAllowanceServiceBase(dbContext, auditScope, auditedMutation, basicSalaryWorkdaySource, logger),
        IPayrollResponsibilityAllowanceWorkflowService;

/// <summary>Composition-only bridge for the legacy aggregate command contract.</summary>
internal sealed class PayrollResponsibilityAllowanceMonthlyAbcCommandCompatibilityAdapter(
    IPayrollResponsibilityAllowanceMonthlyAbcRefreshService refreshService,
    IPayrollResponsibilityAllowanceMonthlyAbcCopyService copyService,
    IPayrollResponsibilityAllowanceMonthlyAbcLockService lockService,
    IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService manualAdjustmentService,
    IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService performanceBonusService)
    : IPayrollResponsibilityAllowanceMonthlyAbcCommandService
{
    public Task<RefreshPayrollResponsibilityAllowanceAbcResult> RefreshAbcAsync(RefreshPayrollResponsibilityAllowanceAbcRequest request, CancellationToken cancellationToken = default) =>
        refreshService.RefreshAbcAsync(request, cancellationToken);

    public Task<CalculatePayrollResponsibilityAllowanceAbcResult> CalculateAbcAsync(RefreshPayrollResponsibilityAllowanceAbcRequest request, CancellationToken cancellationToken = default) =>
        refreshService.CalculateAbcAsync(request, cancellationToken);

    public Task<CopyPayrollResponsibilityAllowanceAbcFromPreviousResult> CopyAbcFromPreviousMonthAsync(int year, int month, CancellationToken cancellationToken = default) =>
        copyService.CopyAbcFromPreviousMonthAsync(year, month, cancellationToken);

    public Task<PayrollResponsibilityAllowanceAbcItemDto> SetLockStateAsync(Guid employeeId, int year, int month, bool isLocked, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default) =>
        lockService.SetLockStateAsync(employeeId, year, month, isLocked, originalUpdatedAtUtc, cancellationToken);

    public Task<SetPayrollResponsibilityAllowanceAbcBatchLockStateResult> SetLockStateBatchAsync(SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest request, CancellationToken cancellationToken = default) =>
        lockService.SetLockStateBatchAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceAbcItemDto> SaveAdjustmentAsync(SavePayrollResponsibilityAllowanceAdjustmentRequest request, CancellationToken cancellationToken = default) =>
        manualAdjustmentService.SaveAdjustmentAsync(request, cancellationToken);

    public Task<PayrollResponsibilityAllowanceAbcItemDto> UpdatePerformanceBonusAsync(Guid employeeId, int year, int month, decimal monthlyPerformanceBonusAmount, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default) =>
        performanceBonusService.UpdatePerformanceBonusAsync(employeeId, year, month, monthlyPerformanceBonusAmount, originalUpdatedAtUtc, cancellationToken);

    public Task<PayrollResponsibilityAllowanceAbcItemDto> UpdatePerformanceBonusExclusionAsync(Guid employeeId, int year, int month, bool isPerformanceBonusExcluded, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default) =>
        performanceBonusService.UpdatePerformanceBonusExclusionAsync(employeeId, year, month, isPerformanceBonusExcluded, originalUpdatedAtUtc, cancellationToken);

    public Task<UpdatePayrollResponsibilityPerformanceBonusForPeriodResult> UpdatePerformanceBonusForPeriodAsync(int year, int month, decimal monthlyPerformanceBonusAmount, IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? concurrencyTokens, CancellationToken cancellationToken = default) =>
        performanceBonusService.UpdatePerformanceBonusForPeriodAsync(year, month, monthlyPerformanceBonusAmount, concurrencyTokens, cancellationToken);
}
