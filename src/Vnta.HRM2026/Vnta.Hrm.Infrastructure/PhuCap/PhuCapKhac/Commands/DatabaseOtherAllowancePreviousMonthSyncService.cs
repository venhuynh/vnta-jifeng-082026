using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;

/// <summary>Creates only missing target lines from the immediately preceding payroll period.</summary>
public sealed class DatabaseOtherAllowancePreviousMonthSyncService(ApplicationDbContext dbContext)
    : IOtherAllowancePreviousMonthSyncService
{
    public async Task<SyncOtherAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncOtherAllowanceFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OtherAllowanceSearchPolicy.ValidatePayrollPeriod(request.TargetPayrollYear, request.TargetPayrollMonth);

        var sourcePeriod = GetPreviousPeriod(request.TargetPayrollMonth, request.TargetPayrollYear);
        var actor = OtherAllowanceAuditPolicy.ResolveActor(request.RequestedBy);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var sourceRows = await (
            from detail in dbContext.PayrollOtherAllowanceRecords.AsNoTracking()
            join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                on detail.PayrollAllowanceSummaryRecordId equals summary.Id
            where summary.PayrollMonth == sourcePeriod.Month && summary.PayrollYear == sourcePeriod.Year
            orderby summary.EmployeeId, detail.AllowanceName, detail.Id
            select new SourceRow(summary.EmployeeId, detail.AllowanceName, detail.IsFixedAmount, detail.AllowanceAmount, detail.Note))
            .ToListAsync(cancellationToken);

        if(sourceRows.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return CreateResult(sourcePeriod, request, 0, 0, 0, 0, 0, 0, 0);
        }

        var targetSummaries = await dbContext.PayrollAllowanceSummaryRecords
            .Where(summary => summary.PayrollMonth == request.TargetPayrollMonth && summary.PayrollYear == request.TargetPayrollYear)
            .ToListAsync(cancellationToken);
        var targetSummariesByEmployee = targetSummaries
            .GroupBy(summary => summary.EmployeeId)
            .ToDictionary(group => group.Key, group => group.First());
        var targetSummaryIds = targetSummaries.Select(summary => summary.Id).ToArray();
        var existingRows = await dbContext.PayrollOtherAllowanceRecords
            .Where(detail => targetSummaryIds.Contains(detail.PayrollAllowanceSummaryRecordId))
            .ToListAsync(cancellationToken);
        var targetEmployeeBySummaryId = targetSummaries.ToDictionary(summary => summary.Id, summary => summary.EmployeeId);
        var existingRowsByKey = existingRows
            .GroupBy(row => CreateAllowanceKey(targetEmployeeBySummaryId[row.PayrollAllowanceSummaryRecordId], row.AllowanceName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var createdRows = new List<PayrollOtherAllowanceRecordRow>();
        var changedSummaries = new HashSet<PayrollAllowanceSummaryRecordRow>();
        var updatedFixedCount = 0;
        var skippedExistingCount = 0;
        var skippedTargetSummaryLockedCount = 0;
        var skippedTargetDetailLockedCount = 0;
        var skippedMissingTargetSummaryCount = 0;

        foreach(var sourceRow in sourceRows)
        {
            if(!targetSummariesByEmployee.TryGetValue(sourceRow.EmployeeId, out var targetSummary))
            {
                skippedMissingTargetSummaryCount++;
                continue;
            }

            if(targetSummary.IsLocked)
            {
                skippedTargetSummaryLockedCount++;
                continue;
            }

            var key = CreateAllowanceKey(sourceRow.EmployeeId, sourceRow.AllowanceName);
            if(existingRowsByKey.TryGetValue(key, out var existingRow))
            {
                if(sourceRow.IsFixedAmount && !existingRow.IsLocked)
                {
                    existingRow.AllowanceName = sourceRow.AllowanceName;
                    existingRow.IsFixedAmount = true;
                    existingRow.AllowanceAmount = sourceRow.AllowanceAmount;
                    existingRow.Note = sourceRow.Note;
                    existingRow.UpdatedAtUtc = now;
                    existingRow.UpdatedBy = actor.Value;
                    changedSummaries.Add(targetSummary);
                    updatedFixedCount++;
                    continue;
                }

                if(sourceRow.IsFixedAmount && existingRow.IsLocked)
                {
                    skippedTargetDetailLockedCount++;
                    continue;
                }

                skippedExistingCount++;
                continue;
            }

            createdRows.Add(new PayrollOtherAllowanceRecordRow
            {
                Id = Guid.NewGuid(),
                PayrollAllowanceSummaryRecordId = targetSummary.Id,
                AllowanceName = sourceRow.AllowanceName,
                IsFixedAmount = sourceRow.IsFixedAmount,
                AllowanceAmount = sourceRow.AllowanceAmount,
                Note = sourceRow.Note,
                IsLocked = false,
                CreatedAtUtc = now,
                CreatedBy = actor.Value
            });
            changedSummaries.Add(targetSummary);
            existingRowsByKey.Add(key, createdRows[^1]);
        }

        if(changedSummaries.Count > 0)
        {
            if(createdRows.Count > 0) dbContext.PayrollOtherAllowanceRecords.AddRange(createdRows);
            await dbContext.SaveChangesAsync(cancellationToken);
            foreach(var summary in changedSummaries)
            {
                await OtherAllowanceSummarySynchronizer.SyncAsync(dbContext, summary, cancellationToken);
                summary.UpdatedAtUtc = now;
                summary.UpdatedBy = actor.Value;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return CreateResult(
            sourcePeriod,
            request,
            sourceRows.Count,
            createdRows.Count,
            updatedFixedCount,
            skippedExistingCount,
            skippedTargetSummaryLockedCount,
            skippedTargetDetailLockedCount,
            skippedMissingTargetSummaryCount);
    }

    private static (int Month, int Year) GetPreviousPeriod(int month, int year) =>
        month == 1 ? (12, year - 1) : (month - 1, year);

    private static string CreateAllowanceKey(Guid employeeId, string allowanceName) =>
        $"{employeeId:N}\u001f{allowanceName.Trim()}";

    private static SyncOtherAllowanceFromPreviousMonthResult CreateResult(
        (int Month, int Year) sourcePeriod,
        SyncOtherAllowanceFromPreviousMonthRequest request,
        int sourceRowCount,
        int createdCount,
        int updatedFixedCount,
        int skippedExistingCount,
        int skippedTargetSummaryLockedCount,
        int skippedTargetDetailLockedCount,
        int skippedMissingTargetSummaryCount) =>
        new(
            sourcePeriod.Month,
            sourcePeriod.Year,
            request.TargetPayrollMonth,
            request.TargetPayrollYear,
            sourceRowCount,
            createdCount,
            updatedFixedCount,
            skippedExistingCount,
            skippedTargetSummaryLockedCount,
            skippedTargetDetailLockedCount,
            skippedMissingTargetSummaryCount);

    private sealed record SourceRow(
        Guid EmployeeId,
        string AllowanceName,
        bool IsFixedAmount,
        decimal AllowanceAmount,
        string? Note);
}
