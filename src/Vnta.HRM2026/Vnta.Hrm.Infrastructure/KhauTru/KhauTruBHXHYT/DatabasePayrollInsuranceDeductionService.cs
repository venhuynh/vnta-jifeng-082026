using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Obsolete compatibility façade for integrations compiled against the former composite service.
/// Runtime composition registers the capability-specific services in <c>Queries</c> and <c>Commands</c>.
/// </summary>
#pragma warning disable CS0618
public sealed class DatabasePayrollInsuranceDeductionService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IPayrollInsuranceDeductionService
{
#pragma warning restore CS0618
    private readonly PayrollInsuranceDeductionPersistence persistence = new(dbContext, auditScope, auditedMutation);

    public Task<PayrollInsuranceDeductionPageDto> SearchAsync(PayrollInsuranceDeductionFilter filter, CancellationToken cancellationToken = default) =>
        persistence.SearchAsync(filter, cancellationToken);

    public Task<RefreshPayrollInsuranceDeductionResult> RefreshAsync(RefreshPayrollInsuranceDeductionRequest request, CancellationToken cancellationToken = default) =>
        persistence.RefreshAsync(request, cancellationToken);

    public Task<SyncPayrollInsuranceDeductionFromPreviousMonthResult> SyncFromPreviousMonthAsync(SyncPayrollInsuranceDeductionFromPreviousMonthRequest request, CancellationToken cancellationToken = default) =>
        persistence.SyncFromPreviousMonthAsync(request, cancellationToken);

    public Task<PayrollInsuranceDeductionListItemDto> UpdateManualValuesAsync(UpdatePayrollInsuranceDeductionManualValuesRequest request, CancellationToken cancellationToken = default) =>
        persistence.UpdateManualValuesAsync(request, cancellationToken);

    public Task<PayrollInsuranceDeductionListItemDto> SetLockStateAsync(SetPayrollInsuranceDeductionLockStateRequest request, CancellationToken cancellationToken = default) =>
        persistence.SetLockStateAsync(request, cancellationToken);

    public Task<SetPayrollInsuranceDeductionBatchLockStateResult> SetLockStateBatchAsync(SetPayrollInsuranceDeductionBatchLockStateRequest request, CancellationToken cancellationToken = default) =>
        persistence.SetLockStateBatchAsync(request, cancellationToken);

    public Task<string?> ValidateAsync(UpsertPayrollInsuranceDeductionRequest request, CancellationToken cancellationToken = default) =>
        persistence.ValidateAsync(request, cancellationToken);

    public Task<PayrollInsuranceDeductionListItemDto> SaveAsync(UpsertPayrollInsuranceDeductionRequest request, bool isNew, CancellationToken cancellationToken = default) =>
        persistence.SaveAsync(request, isNew, cancellationToken);

    public Task DeleteAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        persistence.DeleteAsync(ids, cancellationToken);
}
