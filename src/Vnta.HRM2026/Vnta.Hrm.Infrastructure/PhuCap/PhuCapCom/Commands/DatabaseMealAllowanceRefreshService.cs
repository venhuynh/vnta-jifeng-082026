using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Commands;

public sealed class DatabaseMealAllowanceRefreshService(
    ApplicationDbContext dbContext,
    IMealAllowanceRefreshCalculator refreshCalculator,
    IMealAllowanceRequestValidator requestValidator)
    : IMealAllowanceRefreshService
{
    public async Task<RefreshMealAllowanceResult> RefreshAsync(
        RefreshMealAllowanceRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();
        var actor = MealAllowanceCommandSupport.ResolveActor(request.Actor);
        var targetPayrollMonth = (short)request.TargetPayrollMonth;
        var targetPayrollYear = (short)request.TargetPayrollYear;
        var calculationByEmployeeId = await refreshCalculator.CalculateAsync(
            new MealAllowanceRefreshPeriod(request.TargetPayrollMonth, request.TargetPayrollYear, request.EmployeeId),
            cancellationToken);
        var metricsByEmployeeId = calculationByEmployeeId.ToDictionary(
            pair => pair.Key,
            pair => new MealRefreshMetric(pair.Value.QualifiedMealDays, pair.Value.Overtime1900Days,
                pair.Value.MealAllowancePerQualifiedDay, pair.Value.MealAllowanceAmount));

        var targetSummaryRows = await dbContext.PayrollAllowanceSummaryRecords
            .Where(row => row.PayrollMonth == targetPayrollMonth && row.PayrollYear == targetPayrollYear
                && (!request.EmployeeId.HasValue || row.EmployeeId == request.EmployeeId.Value))
            .ToListAsync(cancellationToken);
        var targetSummaryIds = targetSummaryRows.Select(row => row.Id).ToArray();
        var targetRows = targetSummaryIds.Length == 0
            ? []
            : await dbContext.PayrollMealAllowanceRecords
                .Where(row => targetSummaryIds.Contains(row.PayrollAllowanceSummaryRecordId))
                .ToListAsync(cancellationToken);
        var targetRowsBySummaryId = targetRows.ToDictionary(row => row.PayrollAllowanceSummaryRecordId);

        var now = MealAllowanceCommandSupport.GetDatabaseNow();
        var createdCount = 0;
        var updatedCount = 0;
        var skippedLockedCount = 0;
        var skippedManualAdjustmentCount = 0;

        foreach(var targetSummaryRow in targetSummaryRows)
        {
            var hasMetric = metricsByEmployeeId.TryGetValue(targetSummaryRow.EmployeeId, out var metric);
            if(targetRowsBySummaryId.TryGetValue(targetSummaryRow.Id, out var existingTargetRow))
            {
                if(!request.EmployeeId.HasValue && existingTargetRow.RuleCode == MealAllowancePolicy.ManualAdjustmentRuleCode)
                {
                    skippedManualAdjustmentCount++;
                    continue;
                }

                var requiresChange = hasMetric
                    ? NeedsMealRefreshUpsert(existingTargetRow, metric!)
                    : NeedsMealRefreshReset(existingTargetRow);
                if(!requiresChange)
                    continue;
                if(existingTargetRow.IsLocked || targetSummaryRow.IsLocked)
                {
                    skippedLockedCount++;
                    continue;
                }

                if(hasMetric)
                    ApplyMealRefreshValues(existingTargetRow, metric!, actor, now);
                else
                    ResetMealRefreshValues(existingTargetRow, actor, now);

                MealAllowanceCommandSupport.SetSummaryMealAllowanceAmount(
                    targetSummaryRow, existingTargetRow.MealAllowanceAmount, actor, now);
                updatedCount++;
                continue;
            }

            var newRow = CreateMealRefreshRow(targetSummaryRow, metric, actor, now);
            if(targetSummaryRow.IsLocked)
            {
                newRow.IsLocked = true;
                skippedLockedCount++;
            }

            MealAllowanceCommandSupport.SetSummaryMealAllowanceAmount(
                targetSummaryRow, newRow.MealAllowanceAmount, actor, now);
            dbContext.PayrollMealAllowanceRecords.Add(newRow);
            targetRowsBySummaryId[targetSummaryRow.Id] = newRow;
            createdCount++;
        }

        if(createdCount > 0 || updatedCount > 0)
            await MealAllowanceCommandSupport.SaveChangesWithConcurrencyGuardAsync(dbContext, cancellationToken);

        return new RefreshMealAllowanceResult(targetPayrollMonth, targetPayrollYear, targetSummaryRows.Count,
            metricsByEmployeeId.Count, createdCount, updatedCount, skippedLockedCount, skippedManualAdjustmentCount);
    }

    private static MealRefreshMetric BuildMealRefreshMetric(int matchedRowCount)
    {
        var matchedMealDays = Math.Max(0, matchedRowCount);
        return new MealRefreshMetric(matchedMealDays, matchedMealDays,
            MealAllowancePolicy.DefaultMealAllowancePerQualifiedDay,
            MealAllowancePolicy.CalculateAllowanceAmount(new MealAllowanceAmountInput(
                matchedMealDays, MealAllowancePolicy.DefaultMealAllowancePerQualifiedDay)));
    }

    private static PayrollMealAllowanceRecordRow CreateMealRefreshRow(
        PayrollAllowanceSummaryRecordRow summaryRow,
        MealRefreshMetric? metric,
        string actor,
        DateTime now)
    {
        var resolvedMetric = metric ?? BuildMealRefreshMetric(0);
        return new PayrollMealAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summaryRow.Id,
            QualifiedMealDays = resolvedMetric.QualifiedMealDays,
            Overtime1900Days = resolvedMetric.Overtime1900Days,
            MealAllowancePerQualifiedDay = resolvedMetric.MealAllowancePerQualifiedDay,
            MealAllowanceAmount = resolvedMetric.MealAllowanceAmount,
            RuleCode = MealAllowancePolicy.QualifiedMealRuleCode,
            RuleVersion = MealAllowancePolicy.QualifiedMealRuleVersion,
            IsLocked = false,
            CalculatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = actor
        };
    }

    private static bool NeedsMealRefreshUpsert(PayrollMealAllowanceRecordRow row, MealRefreshMetric metric) =>
        row.QualifiedMealDays != metric.QualifiedMealDays
        || row.Overtime1900Days != metric.Overtime1900Days
        || row.MealAllowancePerQualifiedDay != metric.MealAllowancePerQualifiedDay
        || row.MealAllowanceAmount != metric.MealAllowanceAmount
        || !string.Equals(row.RuleCode, MealAllowancePolicy.QualifiedMealRuleCode, StringComparison.Ordinal)
        || !string.Equals(row.RuleVersion, MealAllowancePolicy.QualifiedMealRuleVersion, StringComparison.Ordinal)
        || !string.IsNullOrWhiteSpace(row.Note);

    private static bool NeedsMealRefreshReset(PayrollMealAllowanceRecordRow row) =>
        row.QualifiedMealDays != 0
        || row.Overtime1900Days != 0
        || row.MealAllowancePerQualifiedDay != MealAllowancePolicy.DefaultMealAllowancePerQualifiedDay
        || row.MealAllowanceAmount != 0m
        || !string.Equals(row.RuleCode, MealAllowancePolicy.QualifiedMealRuleCode, StringComparison.Ordinal)
        || !string.Equals(row.RuleVersion, MealAllowancePolicy.QualifiedMealRuleVersion, StringComparison.Ordinal)
        || !string.IsNullOrWhiteSpace(row.Note);

    private static void ApplyMealRefreshValues(PayrollMealAllowanceRecordRow row, MealRefreshMetric metric, string actor, DateTime now)
    {
        row.QualifiedMealDays = metric.QualifiedMealDays;
        row.Overtime1900Days = metric.Overtime1900Days;
        row.MealAllowancePerQualifiedDay = metric.MealAllowancePerQualifiedDay;
        row.MealAllowanceAmount = metric.MealAllowanceAmount;
        row.RuleCode = MealAllowancePolicy.QualifiedMealRuleCode;
        row.RuleVersion = MealAllowancePolicy.QualifiedMealRuleVersion;
        row.Note = null;
        row.CalculatedAtUtc = now;
        row.UpdatedAtUtc = now;
        row.UpdatedBy = actor;
    }

    private static void ResetMealRefreshValues(PayrollMealAllowanceRecordRow row, string actor, DateTime now)
    {
        row.QualifiedMealDays = 0;
        row.Overtime1900Days = 0;
        row.MealAllowancePerQualifiedDay = MealAllowancePolicy.DefaultMealAllowancePerQualifiedDay;
        row.MealAllowanceAmount = 0m;
        row.RuleCode = MealAllowancePolicy.QualifiedMealRuleCode;
        row.RuleVersion = MealAllowancePolicy.QualifiedMealRuleVersion;
        row.Note = null;
        row.CalculatedAtUtc = now;
        row.UpdatedAtUtc = now;
        row.UpdatedBy = actor;
    }

    private sealed record MealRefreshMetric(
        int QualifiedMealDays,
        int Overtime1900Days,
        decimal MealAllowancePerQualifiedDay,
        decimal MealAllowanceAmount);
}
