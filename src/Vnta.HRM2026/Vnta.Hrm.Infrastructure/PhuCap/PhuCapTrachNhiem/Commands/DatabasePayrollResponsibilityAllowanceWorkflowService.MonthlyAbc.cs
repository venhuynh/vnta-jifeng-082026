using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;

public abstract partial class PayrollResponsibilityAllowancePersistenceOperations
{
    #region Command workflow ABC theo kỳ lương


    /// <summary>
    /// Làm mới snapshot ABC từ các nguồn gốc của kỳ lương: nhân viên, chức vụ,
    /// bậc, chấm công, công chuẩn và summary. Thao tác này không tính lại ABC
    /// hoặc số tiền phụ cấp; các dòng đã khóa được giữ nguyên.
    /// </summary>
    public async Task<RefreshPayrollResponsibilityAllowanceAbcResult> RefreshAbcAsync(
        RefreshPayrollResponsibilityAllowanceAbcRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        var result = await RefreshCoreAsync(request.Year, request.Month, request.EmployeeId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Tính lại hạng ABC và tiền thực tế trên các snapshot đã tồn tại. Dòng khóa
    /// vẫn được đếm trong kết quả nhưng tuyệt đối không bị ghi đè.
    /// </summary>
    public async Task<CalculatePayrollResponsibilityAllowanceAbcResult> CalculateAbcAsync(
        RefreshPayrollResponsibilityAllowanceAbcRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        var rows = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .Where(x => x.Year == request.Year && x.Month == request.Month)
            .Where(x => !request.EmployeeId.HasValue || x.EmployeeId == request.EmployeeId.Value)
            .Where(x => !x.IsPerformanceBonusExcluded)
            .ToListAsync(cancellationToken);

        // Tách ID trước để chỉ tải chấm công và lương cơ bản cho đúng tập dòng cần tính.
        var employeeIds = rows.Select(row => row.EmployeeId).ToArray();
        var workdayAggregates = await LoadWorkdayAggregateAsync(
            request.Year,
            request.Month,
            employeeIds,
            cancellationToken);
        // Công chuẩn là dữ liệu nguồn ở bảng lương cơ bản, không tin vào snapshot cũ.
        var standardWorkdaysByEmployeeId = await basicSalaryWorkdaySource.LoadStandardWorkingDaysAsync(
            request.Year,
            request.Month,
            employeeIds,
            cancellationToken);

        var updated = 0;
        var skippedLocked = 0;
        var ratedA = 0;
        var ratedB = 0;
        var ratedC = 0;
        var ratedD = 0;
        var now = GetDatabaseNow();
        var updatedRows = new List<PayrollResponsibilityAllowanceAbcRow>(rows.Count);

        // Tái tính từng dòng mở, đồng thời thống kê số lượng theo hạng để trả về UI.
        foreach (var row in rows)
        {
            if (row.IsLocked)
            {
                skippedLocked++;
                continue;
            }

            // CTL = Công HC - ĐTVS và cờ KP đều được đóng gói trong aggregate của bảng công tháng.
            workdayAggregates.TryGetValue(row.EmployeeId, out var aggregate);
            row.ActualWorkDays = aggregate?.SalaryWorkdays ?? 0m;
            row.StandardWorkDays = standardWorkdaysByEmployeeId.TryGetValue(
                row.EmployeeId,
                out var standardWorkdays)
                ? standardWorkdays
                : 0m;
            row.AbcRating = ComputeAbcRating(
                row.StandardWorkDays,
                row.ActualWorkDays,
                aggregate?.HasUnexcusedAbsence ?? false);
            row.ActualResponsibilityAllowanceAmount = CalculateActualResponsibilityAllowanceAmount(
                row.StandardResponsibilityAllowanceAmount,
                row.StandardWorkDays,
                row.ActualWorkDays,
                row.AbcRating,
                row.MonthlyPerformanceBonusAmount,
                row.IsPerformanceBonusExcluded);
            row.CalculatedAtUtc = now;
            row.CalculatedBy = CurrentAuditUser;
            row.UpdatedAtUtc = now;
            row.UpdatedBy = CurrentAuditUser;
            updated++;
            updatedRows.Add(row);

            switch (row.AbcRating)
            {
                case "A":
                    ratedA++;
                    break;
                case "B":
                    ratedB++;
                    break;
                case "C":
                    ratedC++;
                    break;
                case "D":
                    ratedD++;
                    break;
            }
        }

        await ApplyDownstreamSnapshotsAsync(updatedRows, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CalculatePayrollResponsibilityAllowanceAbcResult(
            request.Year,
            request.Month,
            rows.Count,
            updated,
            skippedLocked,
            ratedA,
            ratedB,
            ratedC,
            ratedD);
    }

    /// <summary>
    /// Thực hiện lệnh “Tính lại” nguyên tử: làm mới dữ liệu nguồn rồi xếp ABC và
    /// đồng bộ summary trong cùng transaction. Có thể giới hạn cho một nhân viên;
    /// khi đó kiểm tra optimistic concurrency bằng mốc cập nhật từ giao diện.
    /// </summary>
    public async Task<RecalculatePayrollResponsibilityAllowanceAbcResult> RecalculateAbcAsync(
        RefreshPayrollResponsibilityAllowanceAbcRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        // Khi tính lại một dòng, kiểm tra version trước khi bắt đầu transaction dài hơn.
        if (request.EmployeeId.HasValue)
        {
            var currentRow = await dbContext.PayrollResponsibilityAllowanceAbcRows
                .SingleOrDefaultAsync(
                    row => row.EmployeeId == request.EmployeeId.Value
                        && row.Year == request.Year
                        && row.Month == request.Month,
                    cancellationToken);

            if (currentRow is not null)
            {
                // Dòng khóa trả kết quả “bỏ qua” thay vì báo lỗi để thao tác hàng loạt có thể tiếp tục.
                if (currentRow.IsLocked)
                {
                    return new RecalculatePayrollResponsibilityAllowanceAbcResult(
                        request.Year,
                        request.Month,
                        new RefreshPayrollResponsibilityAllowanceAbcResult(
                            request.Year,
                            request.Month,
                            1,
                            0,
                            0,
                            1),
                        new CalculatePayrollResponsibilityAllowanceAbcResult(
                            request.Year,
                            request.Month,
                            1,
                            0,
                            1,
                            0,
                            0,
                            0,
                            0));
                }

                EnsureConcurrency(currentRow, request.OriginalUpdatedAtUtc);
            }
        }

        // Không để snapshot mới được commit nếu bước tính ABC hoặc đồng bộ summary thất bại.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Refresh dựng lại snapshot từ nguồn trước, để bước tính sau không sử dụng số liệu cũ.
        var refresh = await RefreshCoreAsync(
            request.Year,
            request.Month,
            request.EmployeeId,
            cancellationToken,
            recalculateAbc: true);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Không có bậc nguồn cho dòng lẻ: commit phần refresh hợp lệ và kết thúc, không tính “rỗng”.
        if (request.EmployeeId.HasValue && refresh.SkippedMissingSource > 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new RecalculatePayrollResponsibilityAllowanceAbcResult(
                request.Year,
                request.Month,
                refresh,
                new CalculatePayrollResponsibilityAllowanceAbcResult(
                    request.Year,
                    request.Month,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0));
        }

        var rows = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .Where(x => x.Year == request.Year && x.Month == request.Month)
            .Where(x => !request.EmployeeId.HasValue || x.EmployeeId == request.EmployeeId.Value)
            .ToListAsync(cancellationToken);

        var updated = 0;
        var skippedLocked = 0;
        var ratedA = 0;
        var ratedB = 0;
        var ratedC = 0;
        var ratedD = 0;
        var now = GetDatabaseNow();

        foreach (var row in rows)
        {
            if (row.IsLocked)
            {
                skippedLocked++;
                continue;
            }

            // Hạng ABC đổi thì tiền thực tế và summary phụ cấp phải đổi theo cùng lần lưu.
            row.ActualResponsibilityAllowanceAmount = CalculateActualResponsibilityAllowanceAmount(
                row.StandardResponsibilityAllowanceAmount,
                row.StandardWorkDays,
                row.ActualWorkDays,
                row.AbcRating,
                row.MonthlyPerformanceBonusAmount,
                row.IsPerformanceBonusExcluded);
            row.CalculatedAtUtc = now;
            row.CalculatedBy = CurrentAuditUser;
            row.UpdatedAtUtc = now;
            row.UpdatedBy = CurrentAuditUser;
            updated++;

            switch (row.AbcRating)
            {
                case "A":
                    ratedA++;
                    break;
                case "B":
                    ratedB++;
                    break;
                case "C":
                    ratedC++;
                    break;
                case "D":
                    ratedD++;
                    break;
            }
        }

        await ApplyDownstreamSnapshotsAsync(rows, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RecalculatePayrollResponsibilityAllowanceAbcResult(
            request.Year,
            request.Month,
            refresh,
            new CalculatePayrollResponsibilityAllowanceAbcResult(
                request.Year,
                request.Month,
                rows.Count,
                updated,
                skippedLocked,
                ratedA,
                ratedB,
                ratedC,
                ratedD));
    }

    /// <summary>
    /// Khởi tạo kỳ hiện tại từ nguồn mới nhất rồi chỉ sao chép các giá trị được
    /// phép kế thừa (THS và ghi chú) của tháng trước. Mức bậc/công/ABC vẫn lấy
    /// từ dữ liệu kỳ hiện tại; dòng khóa được giữ nguyên.
    /// </summary>
    public async Task<CopyPayrollResponsibilityAllowanceAbcFromPreviousResult> CopyAbcFromPreviousMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existingDestinationEmployeeIds = (await dbContext.PayrollResponsibilityAllowanceAbcRows
                .Where(x => x.Year == year && x.Month == month)
                .Select(x => x.EmployeeId)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var refreshResult = await RefreshCoreAsync(year, month, employeeId: null, cancellationToken);
        var currentPeriod = new ResponsibilityAllowancePeriod(year, month);
        var previousPeriod = currentPeriod.GetPreviousPeriod();

        var currentRows = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .Where(x => x.Year == year && x.Month == month)
            .ToListAsync(cancellationToken);

        // Tháng trước chỉ là nguồn kế thừa THS/ghi chú, không phải nguồn công hoặc bậc của tháng mới.
        var previousRows = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .AsNoTracking()
            .Where(x => x.Year == previousPeriod.Year && x.Month == previousPeriod.Month)
            .ToDictionaryAsync(x => x.EmployeeId, cancellationToken);

        var copiedFromPreviousRows = 0;
        var initializedWithoutPrevious = 0;
        var copiedRows = new List<PayrollResponsibilityAllowanceAbcRow>();
        var now = GetDatabaseNow();

        foreach (var row in currentRows)
        {
            // Merge semantics: an existing destination row is never overwritten.
            if (existingDestinationEmployeeIds.Contains(row.EmployeeId))
            {
                continue;
            }

            if (row.IsLocked)
            {
                continue;
            }

            // Không tìm thấy nhân viên ở tháng trước thì khởi tạo THS = 0 thay vì giữ giá trị không xác định.
            if (previousRows.TryGetValue(row.EmployeeId, out var previous))
            {
                row.MonthlyPerformanceBonusAmount = previous.MonthlyPerformanceBonusAmount;
                row.Note = previous.Note;
                copiedFromPreviousRows++;
            }
            else
            {
                row.MonthlyPerformanceBonusAmount = 0m;
                initializedWithoutPrevious++;
            }

            row.ActualResponsibilityAllowanceAmount = CalculateActualResponsibilityAllowanceAmount(
                row.StandardResponsibilityAllowanceAmount,
                row.StandardWorkDays,
                row.ActualWorkDays,
                row.AbcRating,
                row.MonthlyPerformanceBonusAmount,
                row.IsPerformanceBonusExcluded);
            row.CalculatedAtUtc = now;
            row.CalculatedBy = CurrentAuditUser;
            row.UpdatedAtUtc = now;
            row.UpdatedBy = CurrentAuditUser;
            copiedRows.Add(row);
        }

        await ApplyDownstreamSnapshotsAsync(copiedRows, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CopyPayrollResponsibilityAllowanceAbcFromPreviousResult(
            year,
            month,
            previousPeriod.Year,
            previousPeriod.Month,
            currentRows.Count,
            copiedFromPreviousRows,
            refreshResult.Inserted,
            refreshResult.Updated,
            initializedWithoutPrevious,
            refreshResult.SkippedLocked);
    }

    /// <summary>
    /// Khóa hoặc mở khóa một dòng ABC sau khi xác nhận nó chưa bị người khác cập
    /// nhật. Sau thay đổi, summary liên kết được đồng bộ nếu summary còn mở.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceAbcItemDto> SetLockStateAsync(
        Guid employeeId,
        int year,
        int month,
        bool isLocked,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        var row = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .SingleOrDefaultAsync(x => x.EmployeeId == employeeId && x.Year == year && x.Month == month, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng trách nhiệm cần khóa hoặc mở khóa.");

        // Khóa/mở khóa cũng là thay đổi nghiệp vụ nên phải được bảo vệ khỏi dữ liệu stale.
        EnsureConcurrency(row, originalUpdatedAtUtc);

        var now = GetDatabaseNow();
        row.IsLocked = isLocked;
        row.LockedAtUtc = isLocked ? now : null;
        row.LockedBy = isLocked ? CurrentAuditUser : null;
        row.UpdatedAtUtc = now;
        row.UpdatedBy = CurrentAuditUser;

        await ApplyDownstreamSnapshotsAsync([row], now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapAbcDto(row);
    }

    /// <summary>
    /// Khóa/mở khóa theo danh sách nhân viên hoặc toàn kỳ. Command được audit và
    /// yêu cầu đủ token concurrency cho đúng tập dòng đích để tránh ghi đè hàng
    /// loạt trên dữ liệu stale.
    /// </summary>
    public async Task<SetPayrollResponsibilityAllowanceAbcBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        // null nghĩa là toàn kỳ; danh sách rỗng nghĩa người dùng đã chọn phạm vi nhưng không chọn dòng nào.
        var hasExplicitTargets = request.EmployeeIds is not null;
        var employeeIds = request.EmployeeIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (hasExplicitTargets && (employeeIds is null || employeeIds.Length == 0))
        {
            logger.LogInformation(
                "Responsibility allowance batch lock-state has no valid selected targets for period {PayrollMonth}/{PayrollYear}; target state {IsLocked}.",
                request.Month,
                request.Year,
                request.IsLocked);
            return new SetPayrollResponsibilityAllowanceAbcBatchLockStateResult(
                request.Year,
                request.Month,
                0,
                0);
        }

        var requestAuditCommand = auditScope.Current;
        // Background worker không có audit scope vẫn phải để lại audit event xác định được actor hệ thống.
        var command = requestAuditCommand ?? new AuditCommand(
            Guid.NewGuid(),
            AuditActions.ResponsibilityAllowance.BatchLockStateChanged,
            new AuditActor(SystemAuditUser, SystemAuditUser, AuditActorKind.System, AuditSource.Worker),
            Guid.NewGuid().ToString("N"),
            AuditCaptureMode.OperationOnly,
            Metadata: new Dictionary<string, string>
            {
                [BatchLockAuditScopeMetadataKey] = "system-fallback"
            });

        var result = await auditedMutation.ExecuteAsync(
            command with { ActionIntent = AuditActions.ResponsibilityAllowance.BatchLockStateChanged },
            async token =>
            {
                var query = dbContext.PayrollResponsibilityAllowanceAbcRows
                    .Where(row => row.Year == request.Year && row.Month == request.Month);
                if (hasExplicitTargets)
                {
                    query = query.Where(row => employeeIds!.Contains(row.EmployeeId));
                }

                var targetRows = await query.ToListAsync(token);
                logger.LogInformation(
                    "Responsibility allowance batch lock-state resolved targets for period {PayrollMonth}/{PayrollYear}; target state {IsLocked}; scope {Scope}; target row count {TargetRowCount}; concurrency token count {ConcurrencyTokenCount}.",
                    request.Month,
                    request.Year,
                    request.IsLocked,
                    hasExplicitTargets ? "selected-employees" : "whole-period",
                    targetRows.Count,
                    request.ConcurrencyTokens?.Count ?? 0);
                try
                {
                    EnsureBatchConcurrency(targetRows, request.ConcurrencyTokens);
                }
                catch (ResponsibilityAllowanceConflictException ex)
                {
                    logger.LogInformation(
                        "Responsibility allowance batch lock-state concurrency validation failed for period {PayrollMonth}/{PayrollYear}; target state {IsLocked}; scope {Scope}; target row count {TargetRowCount}; concurrency token count {ConcurrencyTokenCount}; reason {Reason}.",
                        request.Month,
                        request.Year,
                        request.IsLocked,
                        hasExplicitTargets ? "selected-employees" : "whole-period",
                        targetRows.Count,
                        request.ConcurrencyTokens?.Count ?? 0,
                        ex.Message);
                    throw;
                }
                // Chỉ ghi những dòng thực sự đổi trạng thái để audit phản ánh đúng tác động.
                var rowsToUpdate = targetRows
                    .Where(row => row.IsLocked != request.IsLocked)
                    .ToArray();
                if (rowsToUpdate.Length == 0)
                {
                    return new SetPayrollResponsibilityAllowanceAbcBatchLockStateResult(
                        request.Year,
                        request.Month,
                        targetRows.Count,
                        0);
                }

                var now = GetDatabaseNow();
                foreach (var row in rowsToUpdate)
                {
                    row.IsLocked = request.IsLocked;
                    row.LockedAtUtc = request.IsLocked ? now : null;
                    row.LockedBy = request.IsLocked ? CurrentAuditUser : null;
                    row.UpdatedAtUtc = now;
                    row.UpdatedBy = CurrentAuditUser;
                }

                await ApplyDownstreamSnapshotsAsync(rowsToUpdate, now, token);
                return new SetPayrollResponsibilityAllowanceAbcBatchLockStateResult(
                    request.Year,
                    request.Month,
                    targetRows.Count,
                    rowsToUpdate.Length);
            },
            result => new AuditOperationEvent(
                AuditActions.ResponsibilityAllowance.BatchLockStateChanged,
                AuditEntityTypes.ResponsibilityAllowance,
                EntityDisplayName: $"{result.Month:00}/{result.Year}",
                Metadata: new Dictionary<string, string>
                {
                    ["isLocked"] = request.IsLocked.ToString(),
                    ["scope"] = hasExplicitTargets ? "selected-employees" : "whole-period",
                    ["targetRowCount"] = result.TargetRowCount.ToString(),
                    ["updatedCount"] = result.UpdatedCount.ToString(),
                    [BatchLockAuditScopeMetadataKey] = requestAuditCommand is null ? "system-fallback" : "request"
                }),
            cancellationToken);

        logger.LogInformation(
            "Responsibility allowance batch lock-state persisted for period {PayrollMonth}/{PayrollYear}; target state {IsLocked}; scope {Scope}; target row count {TargetRowCount}; updated count {UpdatedCount}.",
            result.Month,
            result.Year,
            request.IsLocked,
            hasExplicitTargets ? "selected-employees" : "whole-period",
            result.TargetRowCount,
            result.UpdatedCount);
        return result;
    }

    /// <summary>
    /// Lưu điều chỉnh toàn diện cho một nhân viên: lưu nguồn gán riêng, làm mới
    /// snapshot, cập nhật THS/ghi chú và đồng bộ summary trong một transaction.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceAbcItemDto> SaveAdjustmentAsync(
        SavePayrollResponsibilityAllowanceAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        if (request.MonthlyPerformanceBonusAmount < 0)
        {
            throw new InvalidOperationException("Thưởng hiệu suất phải là số không âm.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var currentRow = await GetEditableAbcRowAsync(request.EmployeeId, request.Year, request.Month, cancellationToken);
        EnsureConcurrency(currentRow, request.OriginalUpdatedAtUtc);

        // Điều chỉnh popup có thể đổi bậc/nguồn. Không hưởng được biểu diễn bằng xóa
        // assignment riêng, để refresh có thể quay về mapping chức vụ (nếu có).
        if (!request.IsActive)
        {
            var assignment = await (
                    from assignmentRow in dbContext.PayrollResponsibilityAllowanceEmployeeAssignments
                    join summary in dbContext.PayrollAllowanceSummaryRecords
                        on assignmentRow.PayrollAllowanceSummaryRecordId equals summary.Id
                    where summary.EmployeeId == request.EmployeeId
                        && summary.PayrollYear == request.Year
                        && summary.PayrollMonth == request.Month
                    select assignmentRow)
                .SingleOrDefaultAsync(cancellationToken);
            if (assignment is not null)
            {
                dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.Remove(assignment);
            }
        }
        else
        {
            await SaveEmployeeAssignmentCoreAsync(
                new SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest(
                    request.EmployeeAssignmentId,
                    request.Year,
                    request.Month,
                    request.EmployeeId,
                    request.GradeId,
                    request.Note),
                cancellationToken);
        }

        await RefreshCoreAsync(request.Year, request.Month, request.EmployeeId, cancellationToken);

        var row = await GetEditableAbcRowAsync(request.EmployeeId, request.Year, request.Month, cancellationToken);
        var now = GetDatabaseNow();
        row.MonthlyPerformanceBonusAmount = decimal.Round(request.MonthlyPerformanceBonusAmount, 4, MidpointRounding.AwayFromZero);
        row.IsPerformanceBonusExcluded = request.IsPerformanceBonusExcluded;
        row.Note = NormalizeOptional(request.Note);
        row.ActualResponsibilityAllowanceAmount = CalculateActualResponsibilityAllowanceAmount(
            row.StandardResponsibilityAllowanceAmount,
            row.StandardWorkDays,
            row.ActualWorkDays,
            row.AbcRating,
            row.MonthlyPerformanceBonusAmount,
            row.IsPerformanceBonusExcluded);
        row.CalculatedAtUtc = now;
        row.CalculatedBy = CurrentAuditUser;
        row.UpdatedAtUtc = now;
        row.UpdatedBy = CurrentAuditUser;

        await ApplyDownstreamSnapshotsAsync([row], now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MapAbcDto(row);
    }


    /// <summary>
    /// Cập nhật riêng THS của một dòng mở, tính lại tiền theo hạng ABC hiện có và
    /// đồng bộ ngay số tiền thực tế sang summary phụ cấp.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceAbcItemDto> UpdatePerformanceBonusAsync(
        Guid employeeId,
        int year,
        int month,
        decimal monthlyPerformanceBonusAmount,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        if (monthlyPerformanceBonusAmount < 0)
        {
            throw new InvalidOperationException("Thưởng hiệu suất phải là số không âm.");
        }

        var row = await GetEditableAbcRowAsync(employeeId, year, month, cancellationToken);
        EnsureConcurrency(row, originalUpdatedAtUtc);
        var now = GetDatabaseNow();
        row.MonthlyPerformanceBonusAmount = decimal.Round(monthlyPerformanceBonusAmount, 4, MidpointRounding.AwayFromZero);
        row.ActualResponsibilityAllowanceAmount = CalculateActualResponsibilityAllowanceAmount(
            row.StandardResponsibilityAllowanceAmount,
            row.StandardWorkDays,
            row.ActualWorkDays,
            row.AbcRating,
            row.MonthlyPerformanceBonusAmount,
            row.IsPerformanceBonusExcluded);
        row.CalculatedAtUtc = now;
        row.CalculatedBy = CurrentAuditUser;
        row.UpdatedAtUtc = now;
        row.UpdatedBy = CurrentAuditUser;

        await ApplyDownstreamSnapshotsAsync([row], now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapAbcDto(row);
    }

    /// <summary>
    /// Bật/tắt việc loại THS cho một nhân viên. Cờ này là trạng thái tính toán của
    /// snapshot ABC, độc lập với assignment cấp bậc.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceAbcItemDto> UpdatePerformanceBonusExclusionAsync(
        Guid employeeId,
        int year,
        int month,
        bool isPerformanceBonusExcluded,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        var row = await GetEditableAbcRowAsync(employeeId, year, month, cancellationToken);
        EnsureConcurrency(row, originalUpdatedAtUtc);

        var now = GetDatabaseNow();
        row.IsPerformanceBonusExcluded = isPerformanceBonusExcluded;
        row.ActualResponsibilityAllowanceAmount = CalculateActualResponsibilityAllowanceAmount(
            row.StandardResponsibilityAllowanceAmount,
            row.StandardWorkDays,
            row.ActualWorkDays,
            row.AbcRating,
            row.MonthlyPerformanceBonusAmount,
            row.IsPerformanceBonusExcluded);
        row.CalculatedAtUtc = now;
        row.CalculatedBy = CurrentAuditUser;
        row.UpdatedAtUtc = now;
        row.UpdatedBy = CurrentAuditUser;
        await ApplyDownstreamSnapshotsAsync([row], now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapAbcDto(row);
    }

    /// <summary>
    /// Áp một giá trị THS cho toàn bộ dòng trong kỳ. Dòng khóa bị bỏ qua; dòng
    /// loại THS vẫn lưu giá trị nhập để nhất quán nhưng công thức dùng hệ số 1.
    /// </summary>
    public async Task<UpdatePayrollResponsibilityPerformanceBonusForPeriodResult> UpdatePerformanceBonusForPeriodAsync(
        int year,
        int month,
        decimal monthlyPerformanceBonusAmount,
        IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? concurrencyTokens,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        if (monthlyPerformanceBonusAmount < 0)
        {
            throw new InvalidOperationException("Thưởng hiệu suất phải là số không âm.");
        }

        var rows = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .Where(x => x.Year == year && x.Month == month)
            .ToListAsync(cancellationToken);
        // Nhập một THS cho cả kỳ cần token của mọi dòng vì giá trị này ghi đè hàng loạt.
        EnsureBatchConcurrency(rows, concurrencyTokens);

        var updated = 0;
        var skippedLocked = 0;
        var performanceBonusExcludedRows = 0;
        var now = GetDatabaseNow();

        foreach (var row in rows)
        {
            if (row.IsLocked)
            {
                skippedLocked++;
                continue;
            }

            if (row.IsPerformanceBonusExcluded)
            {
                performanceBonusExcludedRows++;
            }

            row.MonthlyPerformanceBonusAmount = decimal.Round(monthlyPerformanceBonusAmount, 4, MidpointRounding.AwayFromZero);
            row.ActualResponsibilityAllowanceAmount = CalculateActualResponsibilityAllowanceAmount(
                row.StandardResponsibilityAllowanceAmount,
                row.StandardWorkDays,
                row.ActualWorkDays,
                row.AbcRating,
                row.MonthlyPerformanceBonusAmount,
                row.IsPerformanceBonusExcluded);
            row.CalculatedAtUtc = now;
            row.CalculatedBy = CurrentAuditUser;
            row.UpdatedAtUtc = now;
            row.UpdatedBy = CurrentAuditUser;
            updated++;
        }

        await ApplyDownstreamSnapshotsAsync(rows, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdatePayrollResponsibilityPerformanceBonusForPeriodResult(
            year,
            month,
            rows.Count,
            updated,
            skippedLocked,
            performanceBonusExcludedRows);
    }

    #endregion
}
