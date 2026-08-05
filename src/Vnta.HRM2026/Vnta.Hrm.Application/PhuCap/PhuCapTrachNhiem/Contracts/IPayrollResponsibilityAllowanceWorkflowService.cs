namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Compatibility facade for existing consumers. New server endpoints depend on the focused contracts.
/// </summary>
[Obsolete("Compatibility facade only; use focused PhuCapTrachNhiem capability interfaces and remove after legacy consumers are retired.")]
public interface IPayrollResponsibilityAllowanceWorkflowService :
    IPayrollResponsibilityAllowanceGradeConfigurationService,
    IPayrollResponsibilityAllowanceEmployeeAssignmentService,
    IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService,
    IPayrollResponsibilityAllowanceEmployeeAssignmentExportService,
    IPayrollResponsibilityAllowanceMonthlyAbcQueryService,
    IPayrollResponsibilityAllowanceMonthlyAbcExportService,
    IPayrollResponsibilityAllowanceMonthlyAbcCommandService,
    IPayrollResponsibilityAllowanceRecalculationService
{
}
