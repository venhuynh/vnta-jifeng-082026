using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT.Commands;

/// <summary>Transactional previous-month copy command boundary.</summary>
public sealed class DatabasePayrollInsuranceDeductionPreviousMonthSyncService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IPayrollInsuranceDeductionPreviousMonthSyncService
{
    private readonly PayrollInsuranceDeductionPersistence persistence = new(dbContext, auditScope, auditedMutation);

    public Task<SyncPayrollInsuranceDeductionFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncPayrollInsuranceDeductionFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.SyncFromPreviousMonthAsync(request, cancellationToken);
}
