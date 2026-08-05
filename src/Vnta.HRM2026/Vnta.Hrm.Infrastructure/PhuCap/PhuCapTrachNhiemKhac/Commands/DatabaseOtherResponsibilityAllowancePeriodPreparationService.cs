using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac;

public sealed class DatabaseOtherResponsibilityAllowancePeriodPreparationService(ApplicationDbContext dbContext)
    : IOtherResponsibilityAllowancePeriodPreparationService
{
    public async Task PreparePeriodAsync(
        int year,
        int month,
        string? requestedBy,
        CancellationToken cancellationToken = default)
    {
        OtherResponsibilityAllowancePeriodPolicy.Validate(year, month);

        if(dbContext.Database.IsNpgsql())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({year}, {month});",
                cancellationToken);
            await EnsureDetailRowsForPeriodAsync(year, month, requestedBy, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await EnsureDetailRowsForPeriodAsync(year, month, requestedBy, cancellationToken);
    }

    private async Task EnsureDetailRowsForPeriodAsync(
        int payrollYear,
        int payrollMonth,
        string? requestedBy,
        CancellationToken cancellationToken)
    {
        var summaryRows = await dbContext.PayrollAllowanceSummaryRecords
            .Where(row => row.PayrollYear == payrollYear && row.PayrollMonth == payrollMonth)
            .ToListAsync(cancellationToken);
        if(summaryRows.Count == 0) return;

        var now = OtherResponsibilityAllowancePersistenceSupport.GetDatabaseNow();
        var summaryIds = summaryRows.Select(row => row.Id).ToArray();
        var existingDetails = await dbContext.PayrollAllowanceOtherResponsibilityRecords
            .Where(row => summaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollAllowanceSummaryRecordId, cancellationToken);
        var missingSummaryRows = summaryRows.Where(row => !existingDetails.ContainsKey(row.Id)).ToArray();
        if(missingSummaryRows.Length == 0) return;

        dbContext.PayrollAllowanceOtherResponsibilityRecords.AddRange(missingSummaryRows.Select(summary =>
            new PayrollAllowanceOtherResponsibilityRecordRow
            {
                PayrollAllowanceSummaryRecordId = summary.Id,
                AllowanceWorkdayCount = 0m,
                StandardResponsibilityAllowanceAmount = 0m,
                ActualResponsibilityAllowanceAmount = 0m,
                Note = null,
                IsLocked = false,
                CreatedAtUtc = now,
                CreatedBy = OtherResponsibilityAllowancePersistenceSupport.NormalizeActor(requestedBy)
            }));

        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch(DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            foreach(var entry in dbContext.ChangeTracker.Entries<PayrollAllowanceOtherResponsibilityRecordRow>()
                        .Where(entry => entry.State == EntityState.Added))
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
