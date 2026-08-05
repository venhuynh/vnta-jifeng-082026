using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT.Commands;

/// <summary>Refresh/recalculation command boundary for BHXH-YT deductions.</summary>
public sealed class DatabasePayrollInsuranceDeductionRefreshService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IPayrollInsuranceDeductionRefreshService
{
    private readonly PayrollInsuranceDeductionPersistence persistence = new(dbContext, auditScope, auditedMutation);

    public Task<RefreshPayrollInsuranceDeductionResult> RefreshAsync(
        RefreshPayrollInsuranceDeductionRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.RefreshAsync(request, cancellationToken);
}
