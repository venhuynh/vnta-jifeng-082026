using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT.Commands;

/// <summary>Manual-value command boundary; preserves optimistic concurrency and audit semantics.</summary>
public sealed class DatabasePayrollInsuranceDeductionManualAdjustmentService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IPayrollInsuranceDeductionManualAdjustmentService
{
    private readonly PayrollInsuranceDeductionPersistence persistence = new(dbContext, auditScope, auditedMutation);

    public Task<PayrollInsuranceDeductionListItemDto> UpdateManualValuesAsync(
        UpdatePayrollInsuranceDeductionManualValuesRequest request,
        CancellationToken cancellationToken = default) =>
        persistence.UpdateManualValuesAsync(request, cancellationToken);
}
