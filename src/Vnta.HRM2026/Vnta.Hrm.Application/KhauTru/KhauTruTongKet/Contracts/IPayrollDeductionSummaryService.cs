using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

/// <summary>
/// Compatibility composite kept for existing client providers. New endpoint code depends on
/// <see cref="IPayrollDeductionSummaryReadService"/> or <see cref="IPayrollDeductionSummaryCommands"/>.
/// Removal plan: migrate remaining legacy providers/tests to the capability interfaces, then delete
/// this facade and its compatibility DI registration.
/// </summary>
[Obsolete("Inject the capability-specific deduction-summary contracts instead; remove after legacy consumers are retired.")]
public interface IPayrollDeductionSummaryService :
    IPayrollDeductionSummaryReadService,
    IPayrollDeductionSummaryExportService,
    IPayrollDeductionSummaryCommands
{
}
