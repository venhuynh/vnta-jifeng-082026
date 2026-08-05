using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT.Commands;

/// <summary>Compatibility-only create/validate/delete command boundary.</summary>
public sealed class DatabasePayrollInsuranceDeductionLegacyWriteService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IPayrollInsuranceDeductionLegacyWriteService
{
    private readonly PayrollInsuranceDeductionPersistence persistence = new(dbContext, auditScope, auditedMutation);

    public Task<string?> ValidateAsync(UpsertPayrollInsuranceDeductionRequest request, CancellationToken cancellationToken = default) =>
        persistence.ValidateAsync(request, cancellationToken);

    public Task<PayrollInsuranceDeductionListItemDto> SaveAsync(UpsertPayrollInsuranceDeductionRequest request, bool isNew, CancellationToken cancellationToken = default) =>
        persistence.SaveAsync(request, isNew, cancellationToken);

    public Task DeleteAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        persistence.DeleteAsync(ids, cancellationToken);
}
