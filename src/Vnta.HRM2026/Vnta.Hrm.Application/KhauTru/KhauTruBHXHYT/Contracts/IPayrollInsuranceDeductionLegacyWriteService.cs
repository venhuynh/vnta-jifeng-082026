namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>Compatibility capability for the existing validate/upsert/delete API.</summary>
public interface IPayrollInsuranceDeductionLegacyWriteService
{
    Task<string?> ValidateAsync(
        UpsertPayrollInsuranceDeductionRequest request,
        CancellationToken cancellationToken = default);

    Task<PayrollInsuranceDeductionListItemDto> SaveAsync(
        UpsertPayrollInsuranceDeductionRequest request,
        bool isNew,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
