using Vnta.Hrm.Application.Common.Security;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Registers the stable responsibility-allowance HTTP contract.
/// Route declarations live here; handlers are separated into query and command
/// boundaries so their dependencies remain limited to the use case they invoke.
/// </summary>
public static class PhuCapTrachNhiemEndpoints
{
    public static RouteGroupBuilder MapPhuCapTrachNhiemEndpoints(this RouteGroupBuilder payrollGroup)
    {
        var featureGroup = payrollGroup.MapGroup("/responsibility-allowance")
            .RequireAuthorization(InternalAccountPolicies.PayrollAdministration);

        featureGroup.MapGet("/grade-config", ResponsibilityAllowanceQueryEndpoints.GetGradeConfigAsync);
        featureGroup.MapPost("/grade-config/grades", ResponsibilityAllowanceCommandEndpoints.SaveGradeAsync);
        featureGroup.MapPost("/grade-config/mappings", ResponsibilityAllowanceCommandEndpoints.SaveMappingAsync);
        featureGroup.MapPost("/grade-config/mappings/{id:guid}/deactivate", ResponsibilityAllowanceCommandEndpoints.DeactivateMappingAsync);
        featureGroup.MapPost("/grade-config/copy-from-previous", ResponsibilityAllowanceCommandEndpoints.CopyConfigurationFromPreviousMonthAsync);

        featureGroup.MapPost("/grade-config/employee-assignments", ResponsibilityAllowanceCommandEndpoints.SaveEmployeeAssignmentAsync);
        featureGroup.MapPost("/grade-config/employee-assignments/synchronize-summaries", ResponsibilityAllowanceCommandEndpoints.SynchronizeEmployeeAssignmentsForSummariesAsync);
        featureGroup.MapPost("/employee-assignments/load-from-previous-month", ResponsibilityAllowanceCommandEndpoints.LoadEmployeeAssignmentsFromPreviousMonthAsync);
        featureGroup.MapPost("/grade-config/employee-assignments/apply-position-defaults", ResponsibilityAllowanceCommandEndpoints.ApplyPositionDefaultsAsync);
        featureGroup.MapPost("/employee-assignments/recalculate", ResponsibilityAllowanceCommandEndpoints.RecalculateEmployeeAssignmentsAsync);
        featureGroup.MapPost("/employee-assignments/search", ResponsibilityAllowanceQueryEndpoints.SearchEmployeeAssignmentsAsync);
        featureGroup.MapPost("/employee-assignments/export", ResponsibilityAllowanceCommandEndpoints.ExportEmployeeAssignmentsAsync);
        featureGroup.MapPost("/employee-assignments/update-and-refresh", ResponsibilityAllowanceCommandEndpoints.UpdateAndRefreshEmployeeAssignmentAsync);

        featureGroup.MapGet("", ResponsibilityAllowanceQueryEndpoints.GetMonthlyAbcAsync);
        featureGroup.MapPost("/search", ResponsibilityAllowanceQueryEndpoints.SearchMonthlyAbcAsync);
        featureGroup.MapPost("/export", ResponsibilityAllowanceCommandEndpoints.ExportMonthlyAbcAsync);
        featureGroup.MapPost("/refresh", ResponsibilityAllowanceCommandEndpoints.RefreshMonthlyAbcAsync);
        featureGroup.MapPost("/calculate-abc", ResponsibilityAllowanceCommandEndpoints.CalculateMonthlyAbcAsync);
        featureGroup.MapPost("/recalculate", ResponsibilityAllowanceCommandEndpoints.RecalculateMonthlyAbcAsync);
        featureGroup.MapPost("/{year:int}/{month:int}/copy-from-previous", ResponsibilityAllowanceCommandEndpoints.CopyMonthlyAbcFromPreviousAsync);
        featureGroup.MapPost("/{employeeId:guid}/{year:int}/{month:int}/lock", ResponsibilityAllowanceCommandEndpoints.LockMonthlyAbcAsync);
        featureGroup.MapPost("/{employeeId:guid}/{year:int}/{month:int}/unlock", ResponsibilityAllowanceCommandEndpoints.UnlockMonthlyAbcAsync);
        featureGroup.MapPost("/lock-state/batch", ResponsibilityAllowanceCommandEndpoints.SetMonthlyAbcBatchLockStateAsync);
        featureGroup.MapPost("/adjustments", ResponsibilityAllowanceCommandEndpoints.SaveMonthlyAbcAdjustmentAsync);
        featureGroup.MapGet("/update-context", ResponsibilityAllowanceQueryEndpoints.GetMonthlyAbcUpdateContextAsync);
        featureGroup.MapPost("/{employeeId:guid}/{year:int}/{month:int}/performance-bonus", ResponsibilityAllowanceCommandEndpoints.UpdatePerformanceBonusAsync);
        featureGroup.MapPost("/{employeeId:guid}/{year:int}/{month:int}/performance-bonus-exclusion", ResponsibilityAllowanceCommandEndpoints.UpdatePerformanceBonusExclusionAsync);
        featureGroup.MapPost("/{year:int}/{month:int}/performance-bonus", ResponsibilityAllowanceCommandEndpoints.UpdatePerformanceBonusForPeriodAsync);
        return payrollGroup;
    }
}
