using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT.Queries;

/// <summary>Read-side capability. The underlying persistence query is projection-only and no-tracking.</summary>
public sealed class DatabasePayrollInsuranceDeductionReadService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IPayrollInsuranceDeductionReadService
{
    private readonly PayrollInsuranceDeductionPersistence persistence = new(dbContext, auditScope, auditedMutation);

    public Task<PayrollInsuranceDeductionPageDto> SearchAsync(
        PayrollInsuranceDeductionFilter filter,
        CancellationToken cancellationToken = default) =>
        persistence.SearchAsync(filter, cancellationToken);
}
