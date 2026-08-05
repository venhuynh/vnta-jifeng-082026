using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Persistence;

#pragma warning disable CS0618 // The retained clear/sync endpoint contracts are compatibility-only.

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Commands;

public sealed class DatabaseLeaveHolidayAllowancePeriodPreparationService(
    ApplicationDbContext dbContext,
    ILeaveHolidayAllowanceRequestValidator requestValidator)
    : ILeaveHolidayAllowancePeriodPreparationService
{
    public async Task PreparePeriodAsync(int payrollYear, int payrollMonth, CancellationToken cancellationToken = default)
    {
        requestValidator.ValidatePeriod(payrollMonth, payrollYear).ThrowIfInvalid();
        var persistence = new LeaveHolidayAllowancePersistence(dbContext);
        await persistence.EnsurePeriodAsync(payrollYear, payrollMonth, cancellationToken);
        if (dbContext.ChangeTracker.HasChanges()) await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DatabaseLeaveHolidayAllowanceClearManualValuesService(
    ApplicationDbContext dbContext,
    ILeaveHolidayAllowanceRequestValidator requestValidator)
    : ILeaveHolidayAllowanceClearManualValuesService
{
    public async Task<ClearLeaveHolidayAllowanceManualValuesResult> ClearManualValuesAsync(
        ClearLeaveHolidayAllowanceManualValuesRequest request, CancellationToken cancellationToken = default)
    {
        requestValidator.Validate(request).ThrowIfInvalid();
        var requestedIds = request.PayrollAllowanceSummaryRecordIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (requestedIds.Length == 0) return new(0, 0, 0, 0);

        var actor = LeaveHolidayAllowancePersistence.NormalizeOptional(request.Actor) ?? LeaveHolidayAllowancePersistence.SystemActor;
        var now = LeaveHolidayAllowancePersistence.GetDatabaseNow();
        var summaries = await dbContext.PayrollAllowanceSummaryRecords.Where(row => requestedIds.Contains(row.Id)).ToListAsync(cancellationToken);
        var details = await new LeaveHolidayAllowancePersistence(dbContext).EnsureDetailsAsync(summaries, now, actor, cancellationToken);
        var cleared = 0;
        var locked = 0;
        var empty = 0;
        foreach (var summary in summaries)
        {
            if (summary.IsLocked) { locked++; continue; }
            var detail = details[summary.Id];
            if (!LeaveHolidayAllowancePersistence.HasManualInput(detail)) { empty++; continue; }
            LeaveHolidayAllowancePersistence.ApplyValues(summary, detail, 0m, 0m, 0m, null, now, actor);
            cleared++;
        }
        if (dbContext.ChangeTracker.HasChanges()) await dbContext.SaveChangesAsync(cancellationToken);
        return new(requestedIds.Length, cleared, locked, empty);
    }
}

public sealed class DatabaseLeaveHolidayAllowancePreviousMonthSyncService(
    ApplicationDbContext dbContext,
    ILeaveHolidayAllowanceRequestValidator requestValidator)
    : ILeaveHolidayAllowancePreviousMonthSyncService
{
    public async Task<SyncLeaveHolidayAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncLeaveHolidayAllowanceFromPreviousMonthRequest request, CancellationToken cancellationToken = default)
    {
        requestValidator.Validate(request).ThrowIfInvalid();
        var sourcePeriod = request.TargetPayrollMonth == 1
            ? (Month: 12, Year: request.TargetPayrollYear - 1)
            : (Month: request.TargetPayrollMonth - 1, Year: request.TargetPayrollYear);
        var actor = LeaveHolidayAllowancePersistence.NormalizeOptional(request.Actor) ?? LeaveHolidayAllowancePersistence.SystemActor;
        var now = LeaveHolidayAllowancePersistence.GetDatabaseNow();
        var persistence = new LeaveHolidayAllowancePersistence(dbContext);
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await persistence.EnsurePeriodAsync(sourcePeriod.Year, sourcePeriod.Month, cancellationToken);
        await persistence.EnsurePeriodAsync(request.TargetPayrollYear, request.TargetPayrollMonth, cancellationToken);
        // The two periods must be materialized before their no-tracking projection is read.
        if (dbContext.ChangeTracker.HasChanges()) await dbContext.SaveChangesAsync(cancellationToken);

        var sourceRows = await LeaveHolidayAllowanceReadProjection.CreateItemsForPeriod(
                dbContext,
                sourcePeriod.Year,
                sourcePeriod.Month)
            .ToListAsync(cancellationToken);
        var targetRows = await LeaveHolidayAllowanceReadProjection.CreateItemsForPeriod(
                dbContext,
                request.TargetPayrollYear,
                request.TargetPayrollMonth)
            .ToListAsync(cancellationToken);
        var targetIds = targetRows.Select(row => row.PayrollAllowanceSummaryRecordId).Distinct().ToArray();
        var summaries = await dbContext.PayrollAllowanceSummaryRecords.Where(row => targetIds.Contains(row.Id)).ToListAsync(cancellationToken);
        var summaryById = summaries.ToDictionary(row => row.Id);
        var details = await persistence.EnsureDetailsAsync(summaries, now, actor, cancellationToken);
        var sourceByEmployee = sourceRows
            .Select(row => (Key: EmployeeKey(row.EmployeeId, row.EmployeeCode, row.EmployeeName), Row: row))
            .Where(pair => pair.Key is not null)
            .GroupBy(pair => pair.Key!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Row, StringComparer.OrdinalIgnoreCase);

        var updated = 0;
        var locked = 0;
        var missing = 0;
        var unchanged = 0;
        foreach (var target in targetRows)
        {
            if (!summaryById.TryGetValue(target.PayrollAllowanceSummaryRecordId, out var summary)) { missing++; continue; }
            if (summary.IsLocked) { locked++; continue; }
            var key = EmployeeKey(target.EmployeeId, target.EmployeeCode, target.EmployeeName);
            if (key is null || !sourceByEmployee.TryGetValue(key, out var source)) { missing++; continue; }
            if (SameValues(target, source)) { unchanged++; continue; }
            LeaveHolidayAllowancePersistence.ApplyValues(summary, details[summary.Id], source.DailyWageAmount, source.LeaveDayCount, source.HolidayDayCount, source.Note, now, actor);
            updated++;
        }
        if (dbContext.ChangeTracker.HasChanges()) await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new(sourcePeriod.Month, sourcePeriod.Year, request.TargetPayrollMonth, request.TargetPayrollYear, sourceRows.Count, targetRows.Count, updated, locked, missing, unchanged);
    }

    private static bool SameValues(LeaveHolidayAllowanceListItemDto left, LeaveHolidayAllowanceListItemDto right) =>
        left.DailyWageAmount == right.DailyWageAmount && left.LeaveDayCount == right.LeaveDayCount && left.HolidayDayCount == right.HolidayDayCount
        && string.Equals(LeaveHolidayAllowancePersistence.NormalizeOptional(left.Note), LeaveHolidayAllowancePersistence.NormalizeOptional(right.Note), StringComparison.Ordinal);

    private static string? EmployeeKey(Guid employeeId, string? employeeCode, string? employeeName)
    {
        if (employeeId != Guid.Empty) return $"id:{employeeId:D}";
        var code = LeaveHolidayAllowancePersistence.NormalizeOptional(employeeCode);
        var name = LeaveHolidayAllowancePersistence.NormalizeOptional(employeeName);
        return code is not null || name is not null ? $"code:{code}|name:{name}" : null;
    }
}

public sealed class DatabaseLeaveHolidayAllowanceRecalculationService(
    ApplicationDbContext dbContext,
    ILeaveHolidayAllowanceRecalculationSource recalculationSource,
    ILeaveHolidayAllowanceRequestValidator requestValidator)
    : ILeaveHolidayAllowanceRecalculationService
{
    private const string MissingBasicSalaryReferenceNote = "Không tồn tại lương căn bản để tham chiếu.";

    public async Task<RecalculateLeaveHolidayAllowanceResult> RecalculateAsync(
        RecalculateLeaveHolidayAllowanceRequest request, CancellationToken cancellationToken = default)
    {
        requestValidator.Validate(request).ThrowIfInvalid();
        if (request.PayrollAllowanceSummaryRecordId == Guid.Empty)
            throw new InvalidOperationException("Dòng tổng hợp phụ cấp cần làm mới không hợp lệ.");
        var actor = LeaveHolidayAllowancePersistence.NormalizeOptional(request.Actor) ?? LeaveHolidayAllowancePersistence.SystemActor;
        var now = LeaveHolidayAllowancePersistence.GetDatabaseNow();
        var persistence = new LeaveHolidayAllowancePersistence(dbContext);
        await persistence.EnsurePeriodAsync(request.PayrollYear, request.PayrollMonth, cancellationToken);
        var summaries = await dbContext.PayrollAllowanceSummaryRecords
            .Where(row => row.PayrollYear == request.PayrollYear && row.PayrollMonth == request.PayrollMonth
                && (!request.PayrollAllowanceSummaryRecordId.HasValue || row.Id == request.PayrollAllowanceSummaryRecordId.Value))
            .ToListAsync(cancellationToken);
        var details = await persistence.EnsureDetailsAsync(summaries, now, actor, cancellationToken);
        var employeeIds = summaries.Select(row => row.EmployeeId).Where(id => id != Guid.Empty).Distinct().ToArray();
        var sourceByEmployee = await recalculationSource.GetSourceValuesAsync(
            new(request.PayrollMonth, request.PayrollYear, employeeIds), cancellationToken);
        var updated = 0;
        var locked = 0;
        foreach (var summary in summaries)
        {
            if (summary.IsLocked) { locked++; continue; }
            var source = sourceByEmployee.GetValueOrDefault(summary.EmployeeId);
            var hasSalary = source?.DailyWageAmount is not null;
            LeaveHolidayAllowancePersistence.ApplyValues(summary, details[summary.Id], source?.DailyWageAmount ?? 0m, source?.LeaveDayCount ?? 0m,
                details[summary.Id].HolidayDayCount, hasSalary ? ClearMissingSalaryNote(details[summary.Id].Note) : MissingBasicSalaryReferenceNote, now, actor);
            updated++;
        }
        if (dbContext.ChangeTracker.HasChanges()) await dbContext.SaveChangesAsync(cancellationToken);
        return new(request.PayrollMonth, request.PayrollYear, summaries.Count, updated, locked);
    }

    private static string? ClearMissingSalaryNote(string? note) => string.Equals(LeaveHolidayAllowancePersistence.NormalizeOptional(note), MissingBasicSalaryReferenceNote, StringComparison.Ordinal) ? null : note;
}

public sealed class DatabaseLeaveHolidayAllowanceManualAdjustmentService(
    ApplicationDbContext dbContext,
    ILeaveHolidayAllowanceRequestValidator requestValidator)
    : ILeaveHolidayAllowanceManualAdjustmentService
{
    public async Task<LeaveHolidayAllowanceListItemDto> UpdateManualValuesAsync(
        UpdateLeaveHolidayAllowanceManualValuesRequest request, CancellationToken cancellationToken = default)
    {
        requestValidator.Validate(request).ThrowIfInvalid();
        if (request.PayrollAllowanceSummaryRecordId == Guid.Empty) throw new InvalidOperationException("Thiếu dòng tổng hợp phụ cấp để cập nhật Phép - Lễ.");
        if (request.DailyWageAmount < 0 || request.LeaveDayCount < 0 || request.HolidayDayCount < 0) throw new InvalidOperationException("Các giá trị phụ cấp Phép - Lễ không được nhỏ hơn 0.");
        var note = LeaveHolidayAllowancePersistence.NormalizeOptional(request.Note);
        if (note is { Length: > LeaveHolidayAllowancePersistence.MaxNoteLength }) throw new InvalidOperationException($"Ghi chú không được vượt quá {LeaveHolidayAllowancePersistence.MaxNoteLength} ký tự.");
        var actor = LeaveHolidayAllowancePersistence.NormalizeOptional(request.Actor) ?? LeaveHolidayAllowancePersistence.SystemActor;
        var now = LeaveHolidayAllowancePersistence.GetDatabaseNow();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleOrDefaultAsync(row => row.Id == request.PayrollAllowanceSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng tổng hợp phụ cấp liên quan.");
        if (summary.IsLocked) throw new InvalidOperationException("Dòng Phụ cấp Phép - Lễ đã khóa, không thể nhập tay.");
        var detail = await new LeaveHolidayAllowancePersistence(dbContext).EnsureDetailAsync(summary, now, actor, cancellationToken);
        EnsureVersion(detail.UpdatedAtUtc ?? detail.CreatedAtUtc, request.OriginalUpdatedAtUtc);
        if (LeaveHolidayAllowanceManualAdjustmentPolicy.Evaluate(new(false, detail.DailyWageAmount, detail.LeaveDayCount, request.DailyWageAmount, request.LeaveDayCount, request.HolidayDayCount))
            == LeaveHolidayAllowanceManualAdjustmentDecision.CalculatedSourceValuesChanged)
            throw new InvalidOperationException("Lương ngày và Công HC được tính từ dữ liệu nguồn. Chỉ được nhập tay số ngày Lễ.");
        LeaveHolidayAllowancePersistence.ApplyValues(summary, detail, detail.DailyWageAmount, detail.LeaveDayCount, request.HolidayDayCount, note, now, actor);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetItemAsync(request.PayrollAllowanceSummaryRecordId, cancellationToken);
    }

    private async Task<LeaveHolidayAllowanceListItemDto> GetItemAsync(Guid summaryId, CancellationToken cancellationToken) =>
        await LeaveHolidayAllowanceReadProjection.CreateItem(dbContext, summaryId).SingleOrDefaultAsync(cancellationToken)
        ?? throw new InvalidOperationException("Không tìm thấy dòng Phụ cấp Phép - Lễ vừa cập nhật.");

    internal static void EnsureVersion(DateTime currentVersion, DateTime? expectedVersion)
    {
        if (!expectedVersion.HasValue) return;
        if (LeaveHolidayAllowancePersistence.ToDatabaseTimestamp(currentVersion) != LeaveHolidayAllowancePersistence.ToDatabaseTimestamp(expectedVersion.Value))
            throw new LeaveHolidayAllowanceConflictException("Dòng Phụ cấp Phép - Lễ đã được thay đổi bởi người dùng khác. Vui lòng tải lại và thực hiện lại thao tác.");
    }
}

public sealed class DatabaseLeaveHolidayAllowanceLockService(
    ApplicationDbContext dbContext,
    ILeaveHolidayAllowanceRequestValidator requestValidator)
    : ILeaveHolidayAllowanceLockService
{
    public async Task<LeaveHolidayAllowanceListItemDto> SetLockStateAsync(SetLeaveHolidayAllowanceLockStateRequest request, CancellationToken cancellationToken = default)
    {
        requestValidator.Validate(request).ThrowIfInvalid();
        if (request.PayrollAllowanceSummaryRecordId == Guid.Empty) throw new InvalidOperationException("Thiếu dòng tổng hợp phụ cấp để khóa hoặc mở khóa.");
        var actor = LeaveHolidayAllowancePersistence.NormalizeOptional(request.Actor) ?? LeaveHolidayAllowancePersistence.SystemActor;
        var now = LeaveHolidayAllowancePersistence.GetDatabaseNow();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleOrDefaultAsync(row => row.Id == request.PayrollAllowanceSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng tổng hợp phụ cấp liên quan.");
        var detail = await new LeaveHolidayAllowancePersistence(dbContext).EnsureDetailAsync(summary, now, actor, cancellationToken);
        DatabaseLeaveHolidayAllowanceManualAdjustmentService.EnsureVersion(summary.UpdatedAtUtc ?? detail.UpdatedAtUtc ?? detail.CreatedAtUtc, request.OriginalUpdatedAtUtc);
        if (summary.IsLocked != request.IsLocked) { summary.IsLocked = request.IsLocked; summary.UpdatedAtUtc = now; summary.UpdatedBy = actor; }
        if (dbContext.ChangeTracker.HasChanges()) await dbContext.SaveChangesAsync(cancellationToken);
        return await LeaveHolidayAllowanceReadProjection.CreateItem(dbContext, summary.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng Phụ cấp Phép - Lễ vừa cập nhật.");
    }

    public async Task<SetLeaveHolidayAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetLeaveHolidayAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default)
    {
        requestValidator.Validate(request).ThrowIfInvalid();
        var hasTargets = request.PayrollAllowanceSummaryRecordIds is not null;
        var distinctIds = request.PayrollAllowanceSummaryRecordIds?.Distinct().ToArray();
        var ids = distinctIds?.Where(id => id != Guid.Empty).ToArray();
        var invalid = distinctIds?.Count(id => id == Guid.Empty) ?? 0;
        if (hasTargets && (ids is null || ids.Length == 0)) return new(request.PayrollYear, request.PayrollMonth, 0, 0, invalid);
        var query = dbContext.PayrollAllowanceSummaryRecords.Where(row => row.PayrollYear == request.PayrollYear && row.PayrollMonth == request.PayrollMonth);
        if (hasTargets) query = query.Where(row => ids!.Contains(row.Id));
        var summaries = await query.ToListAsync(cancellationToken);
        var actor = LeaveHolidayAllowancePersistence.NormalizeOptional(request.Actor) ?? LeaveHolidayAllowancePersistence.SystemActor;
        var now = LeaveHolidayAllowancePersistence.GetDatabaseNow();
        var updated = 0;
        foreach (var summary in summaries.Where(row => row.IsLocked != request.IsLocked))
        {
            summary.IsLocked = request.IsLocked;
            summary.UpdatedAtUtc = now;
            summary.UpdatedBy = actor;
            updated++;
        }
        if (dbContext.ChangeTracker.HasChanges()) await dbContext.SaveChangesAsync(cancellationToken);
        var requested = ids?.Length ?? 0;
        return new(request.PayrollYear, request.PayrollMonth, summaries.Count, updated, hasTargets ? invalid + Math.Max(0, requested - summaries.Count) : 0);
    }
}
