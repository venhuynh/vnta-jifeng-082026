using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

/// <summary>Refreshes the hazard snapshot and its summary amount as one EF save transaction.</summary>
public sealed class DatabaseHazardAllowanceRefreshService(
    ApplicationDbContext dbContext,
    IHazardAllowanceCalculationPolicy calculationPolicy,
    IHazardAllowanceWorkdayMetricsCalculator workdayMetricsCalculator,
    IHazardAllowanceRequestValidator requestValidator)
    : IHazardAllowanceRefreshService
{
    public async Task<RefreshHazardAllowanceResult> RefreshAsync(
        RefreshHazardAllowanceRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();

        var actor = HazardAllowancePersistence.NormalizeOptional(request.RequestedBy) ?? "system";
        var now = HazardAllowancePersistence.ToDatabaseTimestamp(DateTime.UtcNow);
        var fromDate = new DateOnly(request.PayrollYear, request.PayrollMonth, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);
        var month = (short)request.PayrollMonth;
        var year = (short)request.PayrollYear;

        var summaries = await dbContext.PayrollAllowanceSummaryRecords
            .Where(row => row.PayrollMonth == month && row.PayrollYear == year)
            .ToListAsync(cancellationToken);
        if (request.PayrollAllowanceSummaryRecordId is Guid requestedId)
        {
            var requested = summaries.SingleOrDefault(row => row.Id == requestedId)
                ?? throw new InvalidOperationException("Không tìm thấy dòng phụ cấp độc hại thuộc kỳ lương đang áp dụng.");
            summaries = [requested];
        }

        var employeeIds = summaries.Select(row => row.EmployeeId).Distinct().OrderBy(id => id).ToArray();
        if (employeeIds.Length == 0)
            return new RefreshHazardAllowanceResult(request.PayrollMonth, request.PayrollYear, 0, 0, 0, 0, 0, 0);

        var attendance = await (
            from workday in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking() on workday.EmployeeId equals employee.Id
            join department in dbContext.Departments.AsNoTracking() on employee.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking() on employee.PositionId equals position.Id into positions
            from position in positions.DefaultIfEmpty()
            join status in dbContext.AttendanceStatusCodes.AsNoTracking() on workday.CodeKetQuaTinhCongId equals status.Id into statuses
            from status in statuses.DefaultIfEmpty()
            where !employee.IsDeleted && workday.WorkDate >= fromDate && workday.WorkDate <= toDate && employeeIds.Contains(workday.EmployeeId)
            select new WorkdaySource(
                workday.EmployeeId, workday.LateMinutes, workday.EarlyLeaveMinutes,
                status != null && status.PhuCapDocHai,
                department == null ? null : HazardAllowancePersistence.BuildDepartmentPath(department),
                position == null ? null : position.Name))
            .ToListAsync(cancellationToken);
        var metrics = attendance.GroupBy(row => row.EmployeeId).ToDictionary(
            group => group.Key,
            group => workdayMetricsCalculator.Calculate(group.Select(row => new HazardAllowanceWorkday(
                row.DepartmentPath, row.LateMinutes, row.EarlyLeaveMinutes, row.IsHazardStatus,
                row.PositionName))));

        var summariesByEmployee = summaries.ToDictionary(row => row.EmployeeId);
        var summaryIds = summaries.Select(row => row.Id).ToArray();
        var details = await dbContext.PayrollHazardAllowanceRecords
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollAllowanceSummaryRecordId, cancellationToken);
        var created = 0;
        var updated = 0;
        var skippedLocked = 0;
        var ineligible = 0;
        var zeroWorkdays = 0;

        foreach (var employeeId in employeeIds)
        {
            metrics.TryGetValue(employeeId, out var metric);
            var summary = summariesByEmployee[employeeId];
            details.TryGetValue(summary.Id, out var detail);
            if (metric is not { QualifiedWorkdayCount: > 0m }) zeroWorkdays++;
            if (summary.IsLocked || detail?.IsLocked == true) { skippedLocked++; continue; }

            var snapshot = calculationPolicy.Calculate(new HazardAllowanceCalculationInput(
                metric?.DepartmentPath, metric?.QualifiedWorkdayCount ?? 0m, metric?.LateEarlyDeductionDays ?? 0m,
                detail?.IsEligibleForAllowance,
                metric?.PositionName));
            if (!snapshot.IsEligibleForAllowance) ineligible++;

            if (detail is not null)
            {
                var detailChanged = HazardAllowancePersistence.ApplyDetailSnapshot(detail, snapshot, now, actor);
                var summaryChanged = summary.HazardAllowanceAmount != snapshot.HazardAllowanceAmount;
                if (detailChanged || summaryChanged)
                {
                    summary.HazardAllowanceAmount = snapshot.HazardAllowanceAmount;
                    summary.UpdatedAtUtc = now;
                    summary.UpdatedBy = actor;
                    updated++;
                }
            }
            else
            {
                summary.HazardAllowanceAmount = snapshot.HazardAllowanceAmount;
                summary.UpdatedAtUtc = now;
                summary.UpdatedBy = actor;
                detail = new PayrollHazardAllowanceRecordRow
                {
                    PayrollAllowanceSummaryRecordId = summary.Id, CreatedAtUtc = now, CreatedBy = actor,
                    UpdatedAtUtc = now, UpdatedBy = actor
                };
                HazardAllowancePersistence.ApplySnapshot(detail, snapshot);
                dbContext.PayrollHazardAllowanceRecords.Add(detail);
                created++;
            }
        }
        if (created > 0 || updated > 0 || dbContext.ChangeTracker.HasChanges())
            await dbContext.SaveChangesWithConcurrencyGuardAsync(cancellationToken);

        return new RefreshHazardAllowanceResult(request.PayrollMonth, request.PayrollYear, summariesByEmployee.Count,
            created, updated, skippedLocked, ineligible, zeroWorkdays);
    }

    private sealed record WorkdaySource(Guid EmployeeId, int LateMinutes, int EarlyLeaveMinutes,
        bool IsHazardStatus, string? DepartmentPath, string? PositionName);
}
