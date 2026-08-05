namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Composite compatibility contract for integrations compiled against the original feature API.
/// New consumers must inject the narrow capability contract required by their use case.
/// </summary>
/// <remarks>
/// Planned removal: remove this contract in the next breaking Application-contract release,
/// once integrations have migrated to the capability-specific interfaces.
/// </remarks>
[Obsolete("Inject a capability-specific insurance deduction contract instead; remove after legacy consumers are retired.")]
public interface IPayrollInsuranceDeductionService :
    IPayrollInsuranceDeductionReadService,
    IPayrollInsuranceDeductionRefreshService,
    IPayrollInsuranceDeductionPreviousMonthSyncService,
    IPayrollInsuranceDeductionManualAdjustmentService,
    IPayrollInsuranceDeductionLockService,
    IPayrollInsuranceDeductionLegacyWriteService
{
}
