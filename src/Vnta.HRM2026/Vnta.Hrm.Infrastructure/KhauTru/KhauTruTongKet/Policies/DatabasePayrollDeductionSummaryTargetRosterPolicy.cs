using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

/// <summary>EF adapter for the attendance-backed target roster used by previous-month synchronization.</summary>
public sealed class DatabasePayrollDeductionSummaryTargetRosterPolicy(ApplicationDbContext dbContext)
    : IPayrollDeductionSummaryTargetRosterPolicy
{
    public async Task<PayrollDeductionSummaryTargetRoster> GetTargetRosterAsync(
        PayrollDeductionSummaryTargetRosterRequest request,
        CancellationToken cancellationToken = default)
    {
        var targetPeriodStart = new DateOnly(request.TargetPayrollYear, request.TargetPayrollMonth, 1);
        var targetPeriodEndExclusive = targetPeriodStart.AddMonths(1);
        var employeeIds = await dbContext.AttendanceWorkdaySummaries
            .AsNoTracking()
            .Where(row => row.WorkDate >= targetPeriodStart && row.WorkDate < targetPeriodEndExclusive)
            .Select(row => row.EmployeeId)
            .Distinct()
            .OrderBy(employeeId => employeeId)
            .ToArrayAsync(cancellationToken);

        return new PayrollDeductionSummaryTargetRoster(employeeIds);
    }
}
