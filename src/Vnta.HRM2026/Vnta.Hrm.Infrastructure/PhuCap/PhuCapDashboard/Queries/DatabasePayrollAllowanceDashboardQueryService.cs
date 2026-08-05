using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDashboard.Queries;

/// <summary>Read-side implementation. All EF queries are projected and no-tracking in the shared persistence query set.</summary>
internal sealed class DatabasePayrollAllowanceDashboardQueryService(PayrollAllowanceSummaryPersistence persistence)
    : IPayrollAllowanceDashboardReadService,
      IPayrollAllowanceDashboardBreakdownQueryService,
      IPayrollAllowanceDashboardTrendQueryService,
      IPayrollAllowanceDashboardMonthlyComparisonQueryService,
      IPayrollAllowanceDashboardDepartmentComparisonQueryService
{
    public Task<PayrollAllowanceDashboardDto> GetDashboardAsync(PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default) =>
        persistence.GetDashboardAsync(filter, cancellationToken);

    public Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceBreakdownDto>> GetAllowanceBreakdownAsync(PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default) => persistence.GetAllowanceBreakdownAsync(filter, cancellationToken);
    public Task<IReadOnlyList<PayrollAllowanceDashboardTrendPointDto>> GetTrendAsync(PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default) => persistence.GetTrendAsync(filter, cancellationToken);
    public Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceComparisonDto>> GetAllowanceMonthlyComparisonAsync(PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default) => persistence.GetAllowanceMonthlyComparisonAsync(filter, cancellationToken);
    public Task<IReadOnlyList<PayrollAllowanceDashboardDepartmentTreeNodeDto>> GetDepartmentMonthlyComparisonAsync(PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default) => persistence.GetDepartmentMonthlyComparisonAsync(filter, cancellationToken);
}
