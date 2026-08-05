using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

/// <summary>
/// Shared EF mechanics for deduction-summary commands.  It deliberately exposes no application
/// contract: each public command capability is implemented by its own concrete use-case service.
/// </summary>
public abstract class PayrollDeductionSummaryCommandServiceBase
{
    protected readonly ApplicationDbContext dbContext;
    protected readonly IAuditScope auditScope;
    protected readonly IAuditedMutation auditedMutation;
    protected readonly IPayrollDeductionSummaryRequestValidator requestValidator;
    protected readonly IPayrollDeductionSummaryTargetRosterPolicy targetRosterPolicy;

    protected PayrollDeductionSummaryCommandServiceBase(
        ApplicationDbContext dbContext,
        IAuditScope auditScope,
        IAuditedMutation auditedMutation,
        IPayrollDeductionSummaryTargetRosterPolicy? targetRosterPolicy = null,
        IPayrollDeductionSummaryRequestValidator? requestValidator = null)
    {
        this.dbContext = dbContext;
        this.auditScope = auditScope;
        this.auditedMutation = auditedMutation;
        this.requestValidator = requestValidator ?? new PayrollDeductionSummaryRequestValidator();
        this.targetRosterPolicy = targetRosterPolicy ?? new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext);
    }

    protected async Task<SyncPayrollDeductionSummaryFromPreviousMonthResult> ExecuteSyncFromPreviousMonthAsync(
        SyncPayrollDeductionSummaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();

        var actor = NormalizeActor(request.Actor);
        var targetPayrollMonth = (short)request.TargetPayrollMonth;
        var targetPayrollYear = (short)request.TargetPayrollYear;
        var sourcePeriod = PayrollDeductionSummaryPeriodPolicy.Previous(targetPayrollYear, targetPayrollMonth);
        // The attendance-backed roster is the source of truth for target snapshot membership.
        var targetRoster = await targetRosterPolicy.GetTargetRosterAsync(
            new PayrollDeductionSummaryTargetRosterRequest(targetPayrollYear, targetPayrollMonth),
            cancellationToken);
        var attendanceEmployeeIds = targetRoster.EmployeeIds.ToArray();
        var attendanceEmployeeIdSet = attendanceEmployeeIds.ToHashSet();

        var sourceRows = await dbContext.PayrollDeductionSummaryRecords
            .AsNoTracking()
            .Where(row => row.PayrollYear == sourcePeriod.Year
                && row.PayrollMonth == sourcePeriod.Month
                && attendanceEmployeeIds.Contains(row.EmployeeId))
            .OrderBy(row => row.EmployeeId)
            .ToListAsync(cancellationToken);

        var sourceRowsByEmployeeId = sourceRows
            .GroupBy(row => row.EmployeeId)
            .ToDictionary(group => group.Key, group => group.First());

        var targetRows = await dbContext.PayrollDeductionSummaryRecords
            .Where(row =>
                row.PayrollYear == targetPayrollYear
                && row.PayrollMonth == targetPayrollMonth)
            .ToListAsync(cancellationToken);

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // Đồng bộ membership theo bảng công. Dòng khóa chỉ được bảo toàn khi nhân viên vẫn còn chấm công.
        var obsoleteRows = targetRows
            .Where(row => !attendanceEmployeeIdSet.Contains(row.EmployeeId))
            .ToArray();
        if(obsoleteRows.Length > 0)
        {
            await RemoveDependentDeductionRowsAsync(
                obsoleteRows.Select(row => row.Id).ToArray(),
                cancellationToken);
            dbContext.PayrollDeductionSummaryRecords.RemoveRange(obsoleteRows);
            targetRows = targetRows.Except(obsoleteRows).ToList();
        }

        var targetRowsByEmployeeId = targetRows.ToDictionary(row => row.EmployeeId);
        var sourceSummaryIds = sourceRows.Select(row => row.Id).ToArray();
        var targetSummaryIds = targetRows.Select(row => row.Id).ToArray();
        var sourceInsuranceBySummaryId = await dbContext.PayrollDeductionInsuranceRecords.AsNoTracking()
            .Where(row => sourceSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, cancellationToken);
        var sourceTaxBySummaryId = await dbContext.PayrollDeductionTaxRecords.AsNoTracking()
            .Where(row => sourceSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, cancellationToken);
        var sourceUnionFeeBySummaryId = await dbContext.PayrollDeductionUnionFeeRecords.AsNoTracking()
            .Where(row => sourceSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, cancellationToken);
        var sourceAdvanceBySummaryId = await dbContext.PayrollDeductionAdvanceRecords.AsNoTracking()
            .Where(row => sourceSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, cancellationToken);
        var sourceOtherBySummaryId = await dbContext.PayrollDeductionOtherRecords.AsNoTracking()
            .Where(row => sourceSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, cancellationToken);
        var targetInsuranceBySummaryId = await dbContext.PayrollDeductionInsuranceRecords
            .Where(row => targetSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, cancellationToken);
        var targetTaxBySummaryId = await dbContext.PayrollDeductionTaxRecords
            .Where(row => targetSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, cancellationToken);
        var targetUnionFeeBySummaryId = await dbContext.PayrollDeductionUnionFeeRecords
            .Where(row => targetSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, cancellationToken);
        var targetAdvanceBySummaryId = await dbContext.PayrollDeductionAdvanceRecords
            .Where(row => targetSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, cancellationToken);
        var targetOtherBySummaryId = await dbContext.PayrollDeductionOtherRecords
            .Where(row => targetSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, cancellationToken);
        var now = GetDatabaseNow();
        var createdCount = 0;
        var updatedCount = 0;
        var skippedLockedCount = 0;

        foreach(var employeeId in attendanceEmployeeIds)
        {
            sourceRowsByEmployeeId.TryGetValue(employeeId, out var sourceRow);
            targetRowsByEmployeeId.TryGetValue(employeeId, out var targetRow);
            var syncAction = PayrollDeductionSummarySyncPolicy.Decide(new(
                targetRow is null
                    ? PayrollDeductionSummarySyncTargetState.Absent
                    : targetRow.IsLocked
                        ? PayrollDeductionSummarySyncTargetState.Locked
                        : PayrollDeductionSummarySyncTargetState.Unlocked,
                sourceRow is null
                    ? PayrollDeductionSummarySyncSourceState.Missing
                    : PayrollDeductionSummarySyncSourceState.Available));
            if(syncAction == PayrollDeductionSummarySyncAction.PreserveLockedTarget)
            {
                skippedLockedCount++;
                continue;
            }

            if(targetRow is not null)
            {
                if(syncAction == PayrollDeductionSummarySyncAction.UpdateUnlockedTargetFromPreviousMonth)
                {
                    ApplySummaryValues(sourceRow!, targetRow, targetPayrollMonth, targetPayrollYear, actor, now);
                    updatedCount++;
                }

                SyncDeductionChildren(
                    sourceRow?.Id ?? Guid.Empty, targetRow.Id, now,
                    sourceInsuranceBySummaryId, sourceTaxBySummaryId, sourceUnionFeeBySummaryId, sourceAdvanceBySummaryId, sourceOtherBySummaryId,
                    targetInsuranceBySummaryId, targetTaxBySummaryId, targetUnionFeeBySummaryId, targetAdvanceBySummaryId, targetOtherBySummaryId);
                continue;
            }

            var newRow = syncAction == PayrollDeductionSummarySyncAction.CreateEmptyTarget
                ? CreateEmptySummaryRow(employeeId, targetPayrollMonth, targetPayrollYear, actor, now)
                : CreateSummaryRowFromSource(sourceRow!, targetPayrollMonth, targetPayrollYear, actor, now);

            dbContext.PayrollDeductionSummaryRecords.Add(newRow);
            targetRowsByEmployeeId[employeeId] = newRow;
            SyncDeductionChildren(
                sourceRow?.Id ?? Guid.Empty, newRow.Id, now,
                sourceInsuranceBySummaryId, sourceTaxBySummaryId, sourceUnionFeeBySummaryId, sourceAdvanceBySummaryId, sourceOtherBySummaryId,
                targetInsuranceBySummaryId, targetTaxBySummaryId, targetUnionFeeBySummaryId, targetAdvanceBySummaryId, targetOtherBySummaryId);
            createdCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if(transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new SyncPayrollDeductionSummaryFromPreviousMonthResult(
            sourcePeriod.Month,
            sourcePeriod.Year,
            targetPayrollMonth,
            targetPayrollYear,
            sourceRows.Count,
            createdCount,
            updatedCount,
            skippedLockedCount,
            attendanceEmployeeIds.Length,
            obsoleteRows.Length);
    }

    protected async Task<RefreshPayrollDeductionSummaryResult> ExecuteRefreshAsync(
        RefreshPayrollDeductionSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if(request.SummaryRecordId == Guid.Empty)
        {
            throw new InvalidOperationException("Thiếu định danh dòng tổng kết khấu trừ cần làm mới.");
        }

        requestValidator.Validate(request).ThrowIfInvalid();
        if(request.OriginalUpdatedAtUtc == default)
        {
            throw new InvalidOperationException("Thiếu phiên bản dữ liệu để làm mới dòng tổng kết khấu trừ.");
        }

        var payrollYear = (short)request.PayrollYear;
        var payrollMonth = (short)request.PayrollMonth;
        var now = GetDatabaseNow();
        var actor = NormalizeActor(request.Actor);
        var command = CreateRefreshAuditCommand(auditScope.Current);
        var outcome = await auditedMutation.ExecuteAsync(
            command,
            async token =>
            {
                var summary = await dbContext.PayrollDeductionSummaryRecords
                    .SingleOrDefaultAsync(row => row.Id == request.SummaryRecordId, token)
                    ?? throw new InvalidOperationException("Không tìm thấy dòng tổng kết khấu trừ cần làm mới.");

                if(summary.PayrollYear != payrollYear || summary.PayrollMonth != payrollMonth)
                {
                    throw new InvalidOperationException("Dòng tổng kết khấu trừ không thuộc kỳ lương được yêu cầu.");
                }

                if(summary.IsLocked)
                {
                    return new RefreshOutcome(Updated: false, SkippedLocked: true, MissingSourceCount: 0);
                }

                if(PayrollDeductionSummaryConcurrencyPolicy.Evaluate(new(
                    GetRecordVersion(summary), request.OriginalUpdatedAtUtc))
                    != PayrollDeductionSummaryConcurrencyStatus.VersionMatches)
                {
                    throw new InvalidOperationException("Dòng tổng kết khấu trừ đã được thay đổi bởi thao tác khác. Vui lòng tải lại dữ liệu.");
                }

                var insurance = await dbContext.PayrollDeductionInsuranceRecords
                    .AsNoTracking()
                    .SingleOrDefaultAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id, token);
                var tax = await dbContext.PayrollDeductionTaxRecords
                    .AsNoTracking()
                    .SingleOrDefaultAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id, token);
                var unionFee = await dbContext.PayrollDeductionUnionFeeRecords
                    .AsNoTracking()
                    .SingleOrDefaultAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id, token);
                var advance = await dbContext.PayrollDeductionAdvanceRecords
                    .AsNoTracking()
                    .SingleOrDefaultAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id, token);
                var other = await dbContext.PayrollDeductionOtherRecords
                    .AsNoTracking()
                    .SingleOrDefaultAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id, token);

                var reconciliation = CalculateReconciliation(summary, insurance, tax, unionFee, advance, other);
                if(reconciliation.Status == PayrollDeductionSummaryReconciliationStatus.AlreadyReconciled)
                {
                    return new RefreshOutcome(Updated: false, SkippedLocked: false, reconciliation.MissingDetailSourceCount);
                }

                // Detail records are the source of the summary snapshot. This only reconciles the parent;
                // it never writes a detail record, so manually entered "Khấu trừ khác" remains intact.
                ApplyRecalculatedAmounts(summary, reconciliation.RecalculatedSnapshot);
                summary.UpdatedAtUtc = now;
                summary.UpdatedBy = actor;
                return new RefreshOutcome(Updated: true, SkippedLocked: false, reconciliation.MissingDetailSourceCount);
            },
            _ => new AuditOperationEvent(
                AuditActions.DeductionSummary.Refreshed,
                AuditEntityTypes.DeductionSummary,
                request.SummaryRecordId.ToString("D"),
                Metadata: new Dictionary<string, string>
                {
                    ["payrollPeriod"] = $"{payrollMonth:D2}/{payrollYear}",
                    ["source"] = "deduction-detail-records"
                }),
            cancellationToken);

        return new RefreshPayrollDeductionSummaryResult(
            request.SummaryRecordId,
            payrollYear,
            payrollMonth,
            outcome.Updated ? 1 : 0,
            outcome.Updated || outcome.SkippedLocked ? 0 : 1,
            outcome.SkippedLocked ? 1 : 0,
            outcome.MissingSourceCount);
    }

    protected async Task<RecalculatePayrollDeductionSummaryPeriodResult> ExecuteRecalculatePeriodAsync(
        RecalculatePayrollDeductionSummaryPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();

        var payrollYear = (short)request.PayrollYear;
        var payrollMonth = (short)request.PayrollMonth;
        var actor = NormalizeActor(request.Actor);
        var now = GetDatabaseNow();
        var command = CreatePeriodRecalculationAuditCommand(auditScope.Current);
        var outcome = await auditedMutation.ExecuteAsync(
            command,
            async token =>
            {
                // Khóa theo kỳ chỉ ở PostgreSQL để các lệnh batch cùng kỳ không xen kẽ snapshot hoặc audit.
                if(dbContext.Database.IsNpgsql())
                {
                    await dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT pg_advisory_xact_lock({GetPeriodLockKey(payrollYear, payrollMonth)})",
                        token);
                }

                var summaries = await dbContext.PayrollDeductionSummaryRecords
                    .Where(row => row.PayrollYear == payrollYear && row.PayrollMonth == payrollMonth)
                    .OrderBy(row => row.EmployeeId)
                    .ToListAsync(token);
                var openSummaryIds = summaries
                    .Where(row => !row.IsLocked)
                    .Select(row => row.Id)
                    .ToArray();

                if(openSummaryIds.Length == 0)
                {
                    return new PeriodRecalculationOutcome(summaries.Count, 0, 0, summaries.Count, 0);
                }

                var insuranceBySummaryId = await dbContext.PayrollDeductionInsuranceRecords
                    .AsNoTracking()
                    .Where(row => openSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
                    .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, token);
                var taxBySummaryId = await dbContext.PayrollDeductionTaxRecords
                    .AsNoTracking()
                    .Where(row => openSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
                    .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, token);
                var unionFeeBySummaryId = await dbContext.PayrollDeductionUnionFeeRecords
                    .AsNoTracking()
                    .Where(row => openSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
                    .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, token);
                var advanceBySummaryId = await dbContext.PayrollDeductionAdvanceRecords
                    .AsNoTracking()
                    .Where(row => openSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
                    .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, token);
                var otherBySummaryId = await dbContext.PayrollDeductionOtherRecords
                    .AsNoTracking()
                    .Where(row => openSummaryIds.Contains(row.PayrollDeductionSummaryRecordId))
                    .ToDictionaryAsync(row => row.PayrollDeductionSummaryRecordId, token);

                var updatedCount = 0;
                var unchangedCount = 0;
                var missingSourceCount = 0;
                foreach(var summary in summaries.Where(row => !row.IsLocked))
                {
                    insuranceBySummaryId.TryGetValue(summary.Id, out var insurance);
                    taxBySummaryId.TryGetValue(summary.Id, out var tax);
                    unionFeeBySummaryId.TryGetValue(summary.Id, out var unionFee);
                    advanceBySummaryId.TryGetValue(summary.Id, out var advance);
                    otherBySummaryId.TryGetValue(summary.Id, out var other);

                    var reconciliation = CalculateReconciliation(summary, insurance, tax, unionFee, advance, other);
                    missingSourceCount += reconciliation.MissingDetailSourceCount;
                    if(reconciliation.Status == PayrollDeductionSummaryReconciliationStatus.AlreadyReconciled)
                    {
                        unchangedCount++;
                        continue;
                    }

                    ApplyRecalculatedAmounts(summary, reconciliation.RecalculatedSnapshot);
                    summary.UpdatedAtUtc = now;
                    summary.UpdatedBy = actor;
                    updatedCount++;
                }

                return new PeriodRecalculationOutcome(
                    summaries.Count,
                    updatedCount,
                    unchangedCount,
                    summaries.Count(row => row.IsLocked),
                    missingSourceCount);
            },
            result => new AuditOperationEvent(
                AuditActions.DeductionSummary.PeriodRecalculated,
                AuditEntityTypes.DeductionSummary,
                $"{payrollYear:D4}-{payrollMonth:D2}",
                Outcome: result.UpdatedCount == 0 ? AuditOperationOutcome.NoChanges : AuditOperationOutcome.Succeeded,
                Metadata: new Dictionary<string, string>
                {
                    ["payrollPeriod"] = $"{payrollMonth:D2}/{payrollYear}",
                    ["scope"] = "existing-summary-records",
                    ["source"] = "deduction-detail-records",
                    ["targetRowCount"] = result.TargetRowCount.ToString(),
                    ["updatedCount"] = result.UpdatedCount.ToString(),
                    ["unchangedCount"] = result.UnchangedCount.ToString(),
                    ["skippedLockedCount"] = result.SkippedLockedCount.ToString(),
                    ["missingSourceCount"] = result.MissingSourceCount.ToString()
                }),
            cancellationToken);

        return new RecalculatePayrollDeductionSummaryPeriodResult(
            request.PayrollYear,
            request.PayrollMonth,
            outcome.TargetRowCount,
            outcome.UpdatedCount,
            outcome.UnchangedCount,
            outcome.SkippedLockedCount,
            outcome.MissingSourceCount);
    }

    protected async Task<PayrollDeductionSummaryListItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query =
            from summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
            where summary.Id == id
            join insurance in dbContext.PayrollDeductionInsuranceRecords.AsNoTracking()
                on summary.Id equals insurance.PayrollDeductionSummaryRecordId into insuranceGroup
            from insurance in insuranceGroup.DefaultIfEmpty()
            join tax in dbContext.PayrollDeductionTaxRecords.AsNoTracking()
                on summary.Id equals tax.PayrollDeductionSummaryRecordId into taxGroup
            from tax in taxGroup.DefaultIfEmpty()
            join unionFee in dbContext.PayrollDeductionUnionFeeRecords.AsNoTracking()
                on summary.Id equals unionFee.PayrollDeductionSummaryRecordId into unionFeeGroup
            from unionFee in unionFeeGroup.DefaultIfEmpty()
            join advance in dbContext.PayrollDeductionAdvanceRecords.AsNoTracking()
                on summary.Id equals advance.PayrollDeductionSummaryRecordId into advanceGroup
            from advance in advanceGroup.DefaultIfEmpty()
            join other in dbContext.PayrollDeductionOtherRecords.AsNoTracking()
                on summary.Id equals other.PayrollDeductionSummaryRecordId into otherGroup
            from other in otherGroup.DefaultIfEmpty()
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select MapToDto(summary, insurance, tax, unionFee, advance, other, employee, department, position);

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    internal static PayrollDeductionSummaryListItemDto MapToDto(
        PayrollDeductionSummaryRecordRow summary,
        PayrollDeductionInsuranceRecordRow? insurance,
        PayrollDeductionTaxRecordRow? tax,
        PayrollDeductionUnionFeeRecordRow? unionFee,
        PayrollDeductionAdvanceRecordRow? advance,
        PayrollDeductionOtherRecordRow? other,
        AttendanceGatewayEmployeeRow? employee,
        AttendanceDepartmentRow? department,
        AttendanceGatewayPositionRow? position) =>
        new(
            summary.Id,
            summary.EmployeeId,
            employee?.EmployeeCode,
            employee is null ? null : BuildEmployeeName(employee),
            department is null ? null : BuildDepartmentName(department),
            position?.Name,
            summary.PayrollMonth,
            summary.PayrollYear,
            summary.SocialInsuranceDeductionAmount,
            summary.PersonalIncomeTaxDeductionAmount,
            summary.UnionFeeDeductionAmount,
            summary.AdvanceDeductionAmount,
            summary.OtherDeductionAmount,
            summary.IsLocked,
            summary.Note,
            summary.CreatedAtUtc,
            summary.CreatedBy,
            summary.UpdatedAtUtc,
            summary.UpdatedBy);

    protected static DateTime GetRecordVersion(PayrollDeductionSummaryRecordRow record) =>
        record.UpdatedAtUtc ?? record.CreatedAtUtc;

    private static PayrollDeductionSummaryReconciliationResult CalculateReconciliation(
        PayrollDeductionSummaryRecordRow summary,
        PayrollDeductionInsuranceRecordRow? insurance,
        PayrollDeductionTaxRecordRow? tax,
        PayrollDeductionUnionFeeRecordRow? unionFee,
        PayrollDeductionAdvanceRecordRow? advance,
        PayrollDeductionOtherRecordRow? other) =>
        PayrollDeductionSummaryReconciliationCalculator.Calculate(new(
            new PayrollDeductionSummaryAmounts(
                summary.SocialInsuranceDeductionAmount,
                summary.PersonalIncomeTaxDeductionAmount,
                summary.UnionFeeDeductionAmount,
                summary.AdvanceDeductionAmount,
                summary.OtherDeductionAmount),
            new PayrollDeductionSummaryDetailAmounts(
                insurance?.TotalDeductionAmount,
                tax?.DeductionAmount,
                unionFee?.DeductionAmount,
                advance?.DeductionAmount,
                other?.DeductionAmount)));

    private static void ApplyRecalculatedAmounts(
        PayrollDeductionSummaryRecordRow summary,
        PayrollDeductionSummaryAmounts amounts)
    {
        summary.SocialInsuranceDeductionAmount = amounts.SocialInsuranceDeductionAmount;
        summary.PersonalIncomeTaxDeductionAmount = amounts.PersonalIncomeTaxDeductionAmount;
        summary.UnionFeeDeductionAmount = amounts.UnionFeeDeductionAmount;
        summary.AdvanceDeductionAmount = amounts.AdvanceDeductionAmount;
        summary.OtherDeductionAmount = amounts.OtherDeductionAmount;
    }

    protected static AuditCommand CreateManualOtherDeductionAuditCommand(AuditCommand? current) =>
        new(
            current?.OperationId ?? Guid.NewGuid(),
            AuditActions.DeductionSummary.ManualOtherDeductionUpdated,
            current?.Actor ?? new AuditActor("system", "system", AuditActorKind.System, AuditSource.Worker),
            current?.CorrelationId ?? Guid.NewGuid().ToString("N"),
            AuditCaptureMode.OperationOnly,
            Metadata: new Dictionary<string, string>
            {
                ["auditScope"] = current is null ? "system-fallback" : "request"
            });

    protected static AuditCommand CreateRefreshAuditCommand(AuditCommand? current) =>
        new(
            current?.OperationId ?? Guid.NewGuid(),
            AuditActions.DeductionSummary.Refreshed,
            current?.Actor ?? new AuditActor("system", "system", AuditActorKind.System, AuditSource.Worker),
            current?.CorrelationId ?? Guid.NewGuid().ToString("N"),
            AuditCaptureMode.OperationOnly,
            Metadata: new Dictionary<string, string>
            {
                ["auditScope"] = current is null ? "system-fallback" : "request"
            });

    protected static AuditCommand CreatePeriodRecalculationAuditCommand(AuditCommand? current) =>
        new(
            current?.OperationId ?? Guid.NewGuid(),
            AuditActions.DeductionSummary.PeriodRecalculated,
            current?.Actor ?? new AuditActor("system", "system", AuditActorKind.System, AuditSource.Worker),
            current?.CorrelationId ?? Guid.NewGuid().ToString("N"),
            AuditCaptureMode.OperationOnly,
            Metadata: new Dictionary<string, string>
            {
                ["auditScope"] = current is null ? "system-fallback" : "request"
            });

    protected static long GetPeriodLockKey(short payrollYear, short payrollMonth) =>
        ((long)payrollYear << 16) | (ushort)payrollMonth;

    protected static AuditCommand CreateLockStateAuditCommand(AuditCommand? current, string action) =>
        new(
            current?.OperationId ?? Guid.NewGuid(),
            action,
            current?.Actor ?? new AuditActor("system", "system", AuditActorKind.System, AuditSource.Worker),
            current?.CorrelationId ?? Guid.NewGuid().ToString("N"),
            AuditCaptureMode.OperationOnly,
            Metadata: new Dictionary<string, string>
            {
                ["auditScope"] = current is null ? "system-fallback" : "request"
            });

    protected static AuditOperationEvent CreateLockStateAuditEvent(
        string action,
        bool isLocked,
        string scope,
        string auditScopeName,
        SetPayrollDeductionSummaryBatchLockStateResult result) =>
        new(
            action,
            AuditEntityTypes.DeductionSummary,
            EntityDisplayName: $"{result.PayrollMonth:00}/{result.PayrollYear}",
            Metadata: new Dictionary<string, string>
            {
                ["payrollPeriod"] = $"{result.PayrollMonth:00}/{result.PayrollYear}",
                ["scope"] = scope,
                ["isLocked"] = isLocked.ToString(),
                ["targetRowCount"] = result.TargetRowCount.ToString(),
                ["updatedCount"] = result.UpdatedCount.ToString(),
                ["skippedCount"] = result.SkippedCount.ToString(),
                ["auditScope"] = auditScopeName
            });

    private static void ApplySummaryValues(
        PayrollDeductionSummaryRecordRow sourceRow,
        PayrollDeductionSummaryRecordRow targetRow,
        short targetPayrollMonth,
        short targetPayrollYear,
        string actor,
        DateTime now)
    {
        targetRow.EmployeeId = sourceRow.EmployeeId;
        targetRow.PayrollMonth = targetPayrollMonth;
        targetRow.PayrollYear = targetPayrollYear;
        targetRow.SocialInsuranceDeductionAmount = sourceRow.SocialInsuranceDeductionAmount;
        targetRow.PersonalIncomeTaxDeductionAmount = sourceRow.PersonalIncomeTaxDeductionAmount;
        targetRow.UnionFeeDeductionAmount = sourceRow.UnionFeeDeductionAmount;
        targetRow.AdvanceDeductionAmount = sourceRow.AdvanceDeductionAmount;
        targetRow.OtherDeductionAmount = sourceRow.OtherDeductionAmount;
        targetRow.IsLocked = false;
        targetRow.Note = sourceRow.Note;
        targetRow.UpdatedAtUtc = now;
        targetRow.UpdatedBy = actor;
    }

    private static PayrollDeductionSummaryRecordRow CreateSummaryRowFromSource(
        PayrollDeductionSummaryRecordRow sourceRow,
        short targetPayrollMonth,
        short targetPayrollYear,
        string actor,
        DateTime now)
    {
        var targetRow = CreateEmptySummaryRow(
            sourceRow.EmployeeId,
            targetPayrollMonth,
            targetPayrollYear,
            actor,
            now);
        ApplySummaryValues(sourceRow, targetRow, targetPayrollMonth, targetPayrollYear, actor, now);
        targetRow.UpdatedAtUtc = null;
        targetRow.UpdatedBy = null;
        return targetRow;
    }

    private static PayrollDeductionSummaryRecordRow CreateEmptySummaryRow(
        Guid employeeId,
        short targetPayrollMonth,
        short targetPayrollYear,
        string actor,
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayrollMonth = targetPayrollMonth,
            PayrollYear = targetPayrollYear,
            IsLocked = false,
            CreatedAtUtc = now,
            CreatedBy = actor
        };

    private async Task RemoveDependentDeductionRowsAsync(
        IReadOnlyCollection<Guid> summaryIds,
        CancellationToken cancellationToken)
    {
        if(summaryIds.Count == 0)
        {
            return;
        }

        var insuranceRows = await dbContext.PayrollDeductionInsuranceRecords
            .Where(row => summaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToListAsync(cancellationToken);
        var taxRows = await dbContext.PayrollDeductionTaxRecords
            .Where(row => summaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToListAsync(cancellationToken);
        var unionFeeRows = await dbContext.PayrollDeductionUnionFeeRecords
            .Where(row => summaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToListAsync(cancellationToken);
        var advanceRows = await dbContext.PayrollDeductionAdvanceRecords
            .Where(row => summaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToListAsync(cancellationToken);
        var otherRows = await dbContext.PayrollDeductionOtherRecords
            .Where(row => summaryIds.Contains(row.PayrollDeductionSummaryRecordId))
            .ToListAsync(cancellationToken);

        dbContext.PayrollDeductionInsuranceRecords.RemoveRange(insuranceRows);
        dbContext.PayrollDeductionTaxRecords.RemoveRange(taxRows);
        dbContext.PayrollDeductionUnionFeeRecords.RemoveRange(unionFeeRows);
        dbContext.PayrollDeductionAdvanceRecords.RemoveRange(advanceRows);
        dbContext.PayrollDeductionOtherRecords.RemoveRange(otherRows);
    }

    private void SyncDeductionChildren(
        Guid sourceSummaryId,
        Guid targetSummaryId,
        DateTime now,
        IReadOnlyDictionary<Guid, PayrollDeductionInsuranceRecordRow> sourceInsuranceBySummaryId,
        IReadOnlyDictionary<Guid, PayrollDeductionTaxRecordRow> sourceTaxBySummaryId,
        IReadOnlyDictionary<Guid, PayrollDeductionUnionFeeRecordRow> sourceUnionFeeBySummaryId,
        IReadOnlyDictionary<Guid, PayrollDeductionAdvanceRecordRow> sourceAdvanceBySummaryId,
        IReadOnlyDictionary<Guid, PayrollDeductionOtherRecordRow> sourceOtherBySummaryId,
        IDictionary<Guid, PayrollDeductionInsuranceRecordRow> targetInsuranceBySummaryId,
        IDictionary<Guid, PayrollDeductionTaxRecordRow> targetTaxBySummaryId,
        IDictionary<Guid, PayrollDeductionUnionFeeRecordRow> targetUnionFeeBySummaryId,
        IDictionary<Guid, PayrollDeductionAdvanceRecordRow> targetAdvanceBySummaryId,
        IDictionary<Guid, PayrollDeductionOtherRecordRow> targetOtherBySummaryId)
    {
        if(sourceInsuranceBySummaryId.TryGetValue(sourceSummaryId, out var sourceInsurance))
        {
            if(!targetInsuranceBySummaryId.TryGetValue(targetSummaryId, out var targetInsurance))
            {
                targetInsurance = new PayrollDeductionInsuranceRecordRow { PayrollDeductionSummaryRecordId = targetSummaryId, CreatedAtUtc = now };
                dbContext.PayrollDeductionInsuranceRecords.Add(targetInsurance);
                targetInsuranceBySummaryId[targetSummaryId] = targetInsurance;
            }

            if(!targetInsurance.IsLocked)
            {
                targetInsurance.InsuranceSalaryBaseAmount = sourceInsurance.InsuranceSalaryBaseAmount;
                targetInsurance.SocialInsuranceRate = sourceInsurance.SocialInsuranceRate;
                targetInsurance.HealthInsuranceRate = sourceInsurance.HealthInsuranceRate;
                targetInsurance.UnemploymentInsuranceRate = sourceInsurance.UnemploymentInsuranceRate;
                targetInsurance.TotalInsuranceRate = sourceInsurance.TotalInsuranceRate;
                targetInsurance.SocialInsuranceAmount = sourceInsurance.SocialInsuranceAmount;
                targetInsurance.HealthInsuranceAmount = sourceInsurance.HealthInsuranceAmount;
                targetInsurance.UnemploymentInsuranceAmount = sourceInsurance.UnemploymentInsuranceAmount;
                targetInsurance.TotalDeductionAmount = sourceInsurance.TotalDeductionAmount;
                targetInsurance.IsParticipating = sourceInsurance.IsParticipating;
                targetInsurance.ParticipationChangeType = sourceInsurance.ParticipationChangeType;
                targetInsurance.EffectiveDate = sourceInsurance.EffectiveDate;
                targetInsurance.IsLocked = false;
                targetInsurance.UpdatedAtUtc = now;
            }
        }

        SyncAmountRecord(sourceSummaryId, targetSummaryId, now, sourceTaxBySummaryId, targetTaxBySummaryId, dbContext.PayrollDeductionTaxRecords, (id, createdAt) => new PayrollDeductionTaxRecordRow { PayrollDeductionSummaryRecordId = id, CreatedAtUtc = createdAt });
        SyncAmountRecord(sourceSummaryId, targetSummaryId, now, sourceUnionFeeBySummaryId, targetUnionFeeBySummaryId, dbContext.PayrollDeductionUnionFeeRecords, (id, createdAt) => new PayrollDeductionUnionFeeRecordRow { PayrollDeductionSummaryRecordId = id, CreatedAtUtc = createdAt });
        SyncAmountRecord(sourceSummaryId, targetSummaryId, now, sourceAdvanceBySummaryId, targetAdvanceBySummaryId, dbContext.PayrollDeductionAdvanceRecords, (id, createdAt) => new PayrollDeductionAdvanceRecordRow { PayrollDeductionSummaryRecordId = id, CreatedAtUtc = createdAt });
        SyncAmountRecord(sourceSummaryId, targetSummaryId, now, sourceOtherBySummaryId, targetOtherBySummaryId, dbContext.PayrollDeductionOtherRecords, (id, createdAt) => new PayrollDeductionOtherRecordRow { PayrollDeductionSummaryRecordId = id, CreatedAtUtc = createdAt });

        EnsureDeductionChildrenExist(
            targetSummaryId,
            now,
            targetInsuranceBySummaryId,
            targetTaxBySummaryId,
            targetUnionFeeBySummaryId,
            targetAdvanceBySummaryId,
            targetOtherBySummaryId);
    }

    private void EnsureDeductionChildrenExist(
        Guid targetSummaryId,
        DateTime now,
        IDictionary<Guid, PayrollDeductionInsuranceRecordRow> targetInsuranceBySummaryId,
        IDictionary<Guid, PayrollDeductionTaxRecordRow> targetTaxBySummaryId,
        IDictionary<Guid, PayrollDeductionUnionFeeRecordRow> targetUnionFeeBySummaryId,
        IDictionary<Guid, PayrollDeductionAdvanceRecordRow> targetAdvanceBySummaryId,
        IDictionary<Guid, PayrollDeductionOtherRecordRow> targetOtherBySummaryId)
    {
        if(!targetInsuranceBySummaryId.ContainsKey(targetSummaryId))
        {
            var row = new PayrollDeductionInsuranceRecordRow
            {
                PayrollDeductionSummaryRecordId = targetSummaryId,
                CreatedAtUtc = now
            };
            dbContext.PayrollDeductionInsuranceRecords.Add(row);
            targetInsuranceBySummaryId[targetSummaryId] = row;
        }

        EnsureAmountRecordExists(targetSummaryId, now, targetTaxBySummaryId, dbContext.PayrollDeductionTaxRecords,
            (id, createdAt) => new PayrollDeductionTaxRecordRow { PayrollDeductionSummaryRecordId = id, CreatedAtUtc = createdAt });
        EnsureAmountRecordExists(targetSummaryId, now, targetUnionFeeBySummaryId, dbContext.PayrollDeductionUnionFeeRecords,
            (id, createdAt) => new PayrollDeductionUnionFeeRecordRow { PayrollDeductionSummaryRecordId = id, CreatedAtUtc = createdAt });
        EnsureAmountRecordExists(targetSummaryId, now, targetAdvanceBySummaryId, dbContext.PayrollDeductionAdvanceRecords,
            (id, createdAt) => new PayrollDeductionAdvanceRecordRow { PayrollDeductionSummaryRecordId = id, CreatedAtUtc = createdAt });
        EnsureAmountRecordExists(targetSummaryId, now, targetOtherBySummaryId, dbContext.PayrollDeductionOtherRecords,
            (id, createdAt) => new PayrollDeductionOtherRecordRow { PayrollDeductionSummaryRecordId = id, CreatedAtUtc = createdAt });
    }

    private static void SyncAmountRecord<TEntity>(
        Guid sourceSummaryId,
        Guid targetSummaryId,
        DateTime now,
        IReadOnlyDictionary<Guid, TEntity> sourceBySummaryId,
        IDictionary<Guid, TEntity> targetBySummaryId,
        DbSet<TEntity> records,
        Func<Guid, DateTime, TEntity> create)
        where TEntity : class, IPayrollDeductionAmountRecord
    {
        if(!sourceBySummaryId.TryGetValue(sourceSummaryId, out var source))
        {
            return;
        }

        if(!targetBySummaryId.TryGetValue(targetSummaryId, out var target))
        {
            target = create(targetSummaryId, now);
            records.Add(target);
            targetBySummaryId[targetSummaryId] = target;
        }

        if(target.IsLocked)
        {
            return;
        }

        target.DeductionAmount = source.DeductionAmount;
        target.IsLocked = false;
        target.UpdatedAtUtc = now;
    }

    private static void EnsureAmountRecordExists<TEntity>(
        Guid targetSummaryId,
        DateTime now,
        IDictionary<Guid, TEntity> targetBySummaryId,
        DbSet<TEntity> records,
        Func<Guid, DateTime, TEntity> create)
        where TEntity : class, IPayrollDeductionAmountRecord
    {
        if(targetBySummaryId.ContainsKey(targetSummaryId))
        {
            return;
        }

        var row = create(targetSummaryId, now);
        records.Add(row);
        targetBySummaryId[targetSummaryId] = row;
    }

    internal static string BuildEmployeeName(AttendanceGatewayEmployeeRow employee)
    {
        var parts = new[] { employee.LastName, employee.FirstName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part.Trim());

        return string.Join(" ", parts);
    }

    internal static string BuildDepartmentName(AttendanceDepartmentRow department) =>
        NormalizeOptional(department.GroupName)
        ?? NormalizeOptional(department.TeamName)
        ?? NormalizeOptional(department.DepartmentOrWorkshopName)
        ?? NormalizeOptional(department.CenterName)
        ?? string.Empty;

    protected static string NormalizeActor(string? actor)
    {
        var normalizedActor = NormalizeOptional(actor);
        if(string.IsNullOrWhiteSpace(normalizedActor))
        {
            return "system";
        }

        return normalizedActor.Length <= 128
            ? normalizedActor
            : normalizedActor[..128];
    }

    internal static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected static DateTime GetDatabaseNow() =>
        DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified);

    protected static async Task<T> MapConcurrencyAsync<T>(Func<Task<T>> operation)
    {
        try { return await operation(); }
        catch(DbUpdateConcurrencyException)
        {
            throw new PayrollDeductionSummaryConcurrencyException(
                "Dòng tổng kết khấu trừ đã được thay đổi bởi thao tác khác. Vui lòng tải lại dữ liệu.");
        }
    }

    private readonly record struct RefreshOutcome(bool Updated, bool SkippedLocked, int MissingSourceCount);

    private readonly record struct PeriodRecalculationOutcome(
        int TargetRowCount,
        int UpdatedCount,
        int UnchangedCount,
        int SkippedLockedCount,
        int MissingSourceCount);
}
