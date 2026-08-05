namespace Vnta.Hrm.Web.Endpoints;

/// <summary>
/// Maps the dashboard's read-only HTTP surface under the existing allowance-summary group.
/// The caller owns the group so URLs and authorization remain unchanged.
/// </summary>
internal static class PhuCapDashboardEndpointMappings
{
    internal static RouteGroupBuilder MapPhuCapDashboardEndpoints(this RouteGroupBuilder allowanceSummaryGroup)
    {
        allowanceSummaryGroup.MapPost("/dashboard", PhuCapDashboardQueryEndpoints.GetDashboardAsync);
        allowanceSummaryGroup.MapPost("/dashboard/breakdown", PhuCapDashboardQueryEndpoints.GetBreakdownAsync);
        allowanceSummaryGroup.MapPost("/dashboard/trend", PhuCapDashboardQueryEndpoints.GetTrendAsync);
        allowanceSummaryGroup.MapPost("/dashboard/monthly-comparison", PhuCapDashboardQueryEndpoints.GetMonthlyComparisonAsync);
        allowanceSummaryGroup.MapPost("/dashboard/department-monthly-comparison", PhuCapDashboardQueryEndpoints.GetDepartmentMonthlyComparisonAsync);
        return allowanceSummaryGroup;
    }
}
