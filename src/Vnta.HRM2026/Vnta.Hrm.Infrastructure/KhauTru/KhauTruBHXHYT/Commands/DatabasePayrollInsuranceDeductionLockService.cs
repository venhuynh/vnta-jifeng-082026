using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT.Commands;

/// <summary>Lock/unlock command boundary; parent-summary lock and concurrency checks stay intact.</summary>
public sealed class DatabasePayrollInsuranceDeductionLockService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IPayrollInsuranceDeductionLockService
{
    private readonly PayrollInsuranceDeductionPersistence persistence = new(dbContext, auditScope, auditedMutation);

    public Task<PayrollInsuranceDeductionListItemDto> SetLockStateAsync(
        SetPayrollInsuranceDeductionLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.SetLockStateAsync(request, cancellationToken);

    public Task<SetPayrollInsuranceDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollInsuranceDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.SetLockStateBatchAsync(request, cancellationToken);
}
