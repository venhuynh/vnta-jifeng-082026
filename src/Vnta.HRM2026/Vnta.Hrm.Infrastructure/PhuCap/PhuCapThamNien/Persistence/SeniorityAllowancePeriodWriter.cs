using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Commands;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Persistence;

/// <summary>
/// Persistence-only support for the two snapshot commands. It never changes database schema;
/// table and constraint ownership stays exclusively with the existing EF migrations.
/// </summary>
public sealed class SeniorityAllowancePeriodWriter(
    ApplicationDbContext dbContext,
    IPayrollEmployeeSeniorityAllowanceCalculator calculator,
    IPayrollEmployeeSeniorityAllowanceWorkdayCalculator workdayCalculator,
    IPayrollEmployeeSeniorityAllowanceTenureCalculator tenureCalculator,
    IPayrollEmployeeSeniorityAllowanceWorkdaySource workdaySource)
{
    public async Task PrepareAsync(int year, int month, CancellationToken cancellationToken)
    {
        SeniorityAllowanceCommandSupport.ValidatePeriod(year, month);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({year}, {month});", cancellationToken);
        var summaries = await dbContext.PayrollAllowanceSummaryRecords
            .Where(x => x.PayrollYear == year && x.PayrollMonth == month).OrderBy(x => x.Id).ToListAsync(cancellationToken);
        await AddMissingRowsAsync(summaries, year, month, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<RefreshPayrollEmployeeSeniorityAllowanceResult> RefreshAsync(
        RefreshPayrollEmployeeSeniorityAllowanceRequest request, CancellationToken cancellationToken)
    {
        SeniorityAllowanceCommandSupport.ValidatePeriod(request.PayrollYear, request.PayrollMonth);
        await PrepareAsync(request.PayrollYear, request.PayrollMonth, cancellationToken);
        IQueryable<PayrollEmployeeSeniorityAllowanceRow> query = dbContext.PayrollEmployeeSeniorityAllowances.Where(detail =>
            dbContext.PayrollAllowanceSummaryRecords.Any(summary => summary.Id == detail.PayrollAllowanceSummaryRecordId
                && summary.PayrollYear == request.PayrollYear && summary.PayrollMonth == request.PayrollMonth));
        if(request.PayrollAllowanceSummaryRecordId.HasValue)
            query = query.Where(x => x.PayrollAllowanceSummaryRecordId == request.PayrollAllowanceSummaryRecordId.Value);

        var total = await query.CountAsync(cancellationToken);
        if(request.PayrollAllowanceSummaryRecordId.HasValue && total == 0)
            throw new InvalidOperationException("Dòng phụ cấp thâm niên không thuộc kỳ lương cần làm mới.");
        var skippedLocked = await query.CountAsync(x => x.IsLocked, cancellationToken);
        var rows = await query.Where(x => !x.IsLocked).OrderBy(x => x.PayrollAllowanceSummaryRecordId).ToListAsync(cancellationToken);
        if(rows.Count == 0)
            return new RefreshPayrollEmployeeSeniorityAllowanceResult(request.PayrollYear, request.PayrollMonth, total, 0, skippedLocked);

        var summaryIds = rows.Select(x => x.PayrollAllowanceSummaryRecordId).ToArray();
        var summaries = await dbContext.PayrollAllowanceSummaryRecords.Where(x => summaryIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var employeeIds = summaries.Values.Select(x => x.EmployeeId).Distinct().ToArray();
        var employees = await dbContext.Employees.AsNoTracking().Where(x => employeeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var positionIds = employees.Values.Select(x => x.PositionId).Distinct().ToArray();
        var positionNames = await dbContext.Positions.AsNoTracking().Where(x => positionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var workdays = await LoadWorkdaysAsync(request.PayrollYear, request.PayrollMonth, employeeIds, cancellationToken);
        var now = SeniorityAllowanceCommandSupport.GetDatabaseNow();
        var updated = 0;
        foreach(var row in rows)
        {
            if(!summaries.TryGetValue(row.PayrollAllowanceSummaryRecordId, out var summary) || !employees.TryGetValue(summary.EmployeeId, out var employee))
                continue;
            ApplySnapshot(row, employee, positionNames.GetValueOrDefault(employee.PositionId), workdays.GetValueOrDefault(employee.Id, PayrollEmployeeSeniorityAllowanceWorkdayCalculation.Empty), request.PayrollYear, request.PayrollMonth, now);
            updated++;
        }
        if(updated > 0)
            await dbContext.SaveChangesAsync(cancellationToken);
        return new RefreshPayrollEmployeeSeniorityAllowanceResult(request.PayrollYear, request.PayrollMonth, total, updated, skippedLocked);
    }

    private async Task AddMissingRowsAsync(IReadOnlyList<PayrollAllowanceSummaryRecordRow> summaries, int year, int month, CancellationToken cancellationToken)
    {
        if(summaries.Count == 0)
            return;
        var ids = summaries.Select(x => x.Id).ToArray();
        var existing = await dbContext.PayrollEmployeeSeniorityAllowances.Where(x => ids.Contains(x.PayrollAllowanceSummaryRecordId))
            .Select(x => x.PayrollAllowanceSummaryRecordId).ToHashSetAsync(cancellationToken);
        var missing = summaries.Where(x => !existing.Contains(x.Id)).ToArray();
        if(missing.Length == 0)
            return;
        var employeeIds = missing.Select(x => x.EmployeeId).Distinct().ToArray();
        var employees = await dbContext.Employees.AsNoTracking().Where(x => employeeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var positionIds = employees.Values.Select(x => x.PositionId).Distinct().ToArray();
        var positionNames = await dbContext.Positions.AsNoTracking().Where(x => positionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var workdays = await LoadWorkdaysAsync(year, month, employeeIds, cancellationToken);
        var now = SeniorityAllowanceCommandSupport.GetDatabaseNow();
        foreach(var summary in missing)
        {
            if(!employees.TryGetValue(summary.EmployeeId, out var employee))
                continue;
            var detail = new PayrollEmployeeSeniorityAllowanceRow
            {
                PayrollAllowanceSummaryRecordId = summary.Id, IsLocked = false, CreatedAtUtc = now,
                CreatedBy = SeniorityAllowanceCommandSupport.SystemActor
            };
            ApplySnapshot(detail, employee, positionNames.GetValueOrDefault(employee.PositionId), workdays.GetValueOrDefault(employee.Id, PayrollEmployeeSeniorityAllowanceWorkdayCalculation.Empty), year, month, now);
            dbContext.PayrollEmployeeSeniorityAllowances.Add(detail);
        }
    }

    private async Task<Dictionary<Guid, PayrollEmployeeSeniorityAllowanceWorkdayCalculation>> LoadWorkdaysAsync(
        int year, int month, IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken)
    {
        if(employeeIds.Count == 0)
            return [];
        var inputs = await workdaySource.LoadAsync(new PayrollEmployeeSeniorityAllowanceWorkdaySourceQuery(year, month, employeeIds), cancellationToken);
        return inputs.ToDictionary(x => x.Key, x => workdayCalculator.Calculate(x.Value));
    }

    private void ApplySnapshot(PayrollEmployeeSeniorityAllowanceRow detail,
        AttendanceGatewayEmployeeRow employee, string? positionName, PayrollEmployeeSeniorityAllowanceWorkdayCalculation workdays, int year, int month, DateTime now)
    {
        var start = DateOnly.FromDateTime((employee.SeniorityStartDate ?? employee.HireDate).Date);
        var tenure = tenureCalculator.Calculate(new PayrollEmployeeSeniorityAllowanceTenureInput(start, new DateOnly(year, month, DateTime.DaysInMonth(year, month))));
        var allowance = calculator.Calculate(new PayrollEmployeeSeniorityAllowanceCalculationInput(tenure.CompletedYears, workdays.SalaryWorkDays, positionName));
        detail.EmploymentStartDate = start;
        detail.CompletedSeniorityYears = tenure.CompletedYears;
        detail.CompletedSeniorityMonths = tenure.CompletedMonths;
        detail.AdministrativeWorkDays = workdays.AdministrativeWorkDays;
        detail.LateEarlyLeaveWorkDays = workdays.LateEarlyLeaveWorkDays;
        detail.SalaryWorkDays = workdays.SalaryWorkDays;
        detail.AppliedRuleKey = allowance.AppliedRule.ToStorageKey();
        detail.AllowanceAmount = allowance.AllowanceAmount;
        detail.RefreshedAtUtc = now;
        detail.RefreshedBy = SeniorityAllowanceCommandSupport.SystemActor;
        detail.UpdatedAtUtc = now;
        detail.UpdatedBy = SeniorityAllowanceCommandSupport.SystemActor;
    }
}
