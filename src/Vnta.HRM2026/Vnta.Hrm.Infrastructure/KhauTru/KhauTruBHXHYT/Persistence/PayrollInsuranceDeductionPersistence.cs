using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT;

/// <summary>Raw persistence implementation shared by the feature's narrow read and command adapters.</summary>
public sealed class PayrollInsuranceDeductionPersistence(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
{
    private const int MaxSearchResultLimit = 5000;
    private const string SystemActor = "system";
    private static readonly DateOnly MinimumSyncTargetPeriod = new(2026, 6, 1);

    public async Task<PayrollInsuranceDeductionPageDto> SearchAsync(
        PayrollInsuranceDeductionFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedSkip = Math.Max(0, filter.Skip);
        var normalizedTake = Math.Clamp(filter.Take, 1, MaxSearchResultLimit);
        var normalizedSearchTerm = NormalizeOptional(filter.SearchText);

        var query =
            from detail in dbContext.PayrollDeductionInsuranceRecords.AsNoTracking()
            join summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
                on detail.PayrollDeductionSummaryRecordId equals summary.Id
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new { detail, summary, employee, department, position };

        if (filter.PayrollMonth.HasValue)
        {
            var month = (short)Math.Clamp(filter.PayrollMonth.Value, 1, 12);
            query = query.Where(x => x.summary.PayrollMonth == month);
        }

        if (filter.PayrollYear.HasValue)
        {
            var year = (short)Math.Clamp(filter.PayrollYear.Value, 2000, 2100);
            query = query.Where(x => x.summary.PayrollYear == year);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            var searchPattern = $"%{normalizedSearchTerm}%";
            query = query.Where(x =>
                (x.employee != null && x.employee.EmployeeCode != null && EF.Functions.ILike(x.employee.EmployeeCode, searchPattern))
                || (x.employee != null && x.employee.FirstName != null && EF.Functions.ILike(x.employee.FirstName, searchPattern))
                || (x.employee != null && x.employee.LastName != null && EF.Functions.ILike(x.employee.LastName, searchPattern))
                || (x.department != null && x.department.DepartmentOrWorkshopName != null && EF.Functions.ILike(x.department.DepartmentOrWorkshopName, searchPattern))
                || (x.position != null && x.position.Name != null && EF.Functions.ILike(x.position.Name, searchPattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return new PayrollInsuranceDeductionPageDto([], 0);
        }

        var rows = await query
            .OrderByDescending(x => x.summary.PayrollYear)
            .ThenByDescending(x => x.summary.PayrollMonth)
            .ThenBy(x => x.employee == null ? string.Empty : x.employee.EmployeeCode)
            .ThenByDescending(x => x.detail.CreatedAtUtc)
            .ThenBy(x => x.detail.PayrollDeductionSummaryRecordId)
            .Skip(normalizedSkip)
            .Take(normalizedTake)
            .Select(x => MapToDto(x.detail, x.summary, x.employee, x.department, x.position))
            .ToListAsync(cancellationToken);

        return new PayrollInsuranceDeductionPageDto(rows, totalCount);
    }

    public async Task<RefreshPayrollInsuranceDeductionResult> RefreshAsync(
        RefreshPayrollInsuranceDeductionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.TargetPayrollMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng kỳ lương phải nằm trong khoảng từ 1 đến 12.");
        }

        if (request.TargetPayrollYear is < 2000 or > 2100)
        {
            throw new InvalidOperationException("Năm kỳ lương không hợp lệ.");
        }

        if (request.PayrollDeductionSummaryRecordId == Guid.Empty)
        {
            throw new InvalidOperationException("Dòng khấu trừ BHXH-YT cần làm mới không hợp lệ.");
        }

        var targetPayrollMonth = (short)request.TargetPayrollMonth;
        var targetPayrollYear = (short)request.TargetPayrollYear;

        var rows = await (
                from detail in dbContext.PayrollDeductionInsuranceRecords
                join summary in dbContext.PayrollDeductionSummaryRecords
                    on detail.PayrollDeductionSummaryRecordId equals summary.Id
                where summary.PayrollMonth == targetPayrollMonth
                      && summary.PayrollYear == targetPayrollYear
                      && (!request.PayrollDeductionSummaryRecordId.HasValue
                          || summary.Id == request.PayrollDeductionSummaryRecordId.Value)
                select new { detail, summary })
            .ToListAsync(cancellationToken);

        if (request.PayrollDeductionSummaryRecordId.HasValue && rows.Count == 0)
        {
            throw new InvalidOperationException("Không tìm thấy dòng khấu trừ BHXH-YT thuộc kỳ lương đã chọn.");
        }

        if (rows.Count == 0)
        {
            return new RefreshPayrollInsuranceDeductionResult(
                targetPayrollMonth,
                targetPayrollYear,
                0,
                0,
                0);
        }

        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        var updatedCount = 0;
        var skippedLockedCount = 0;

        foreach (var row in rows)
        {
            if (PayrollInsuranceDeductionLockPolicy.Evaluate(
                    new PayrollInsuranceDeductionLockInput(row.detail.IsLocked, row.summary.IsLocked))
                == PayrollInsuranceDeductionLockDecision.Locked)
            {
                skippedLockedCount++;
                continue;
            }

            var previousTotalInsuranceRate = row.detail.TotalInsuranceRate;
            var previousSocialInsuranceAmount = row.detail.SocialInsuranceAmount;
            var previousHealthInsuranceAmount = row.detail.HealthInsuranceAmount;
            var previousUnemploymentInsuranceAmount = row.detail.UnemploymentInsuranceAmount;
            var previousTotalDeductionAmount = row.detail.TotalDeductionAmount;

            ApplyCalculatedValues(row.detail);
            var detailChanged = previousTotalInsuranceRate != row.detail.TotalInsuranceRate
                || previousSocialInsuranceAmount != row.detail.SocialInsuranceAmount
                || previousHealthInsuranceAmount != row.detail.HealthInsuranceAmount
                || previousUnemploymentInsuranceAmount != row.detail.UnemploymentInsuranceAmount
                || previousTotalDeductionAmount != row.detail.TotalDeductionAmount;
            var summaryChanged = row.summary.SocialInsuranceDeductionAmount != row.detail.TotalDeductionAmount;

            if (!detailChanged && !summaryChanged)
            {
                continue;
            }

            row.detail.UpdatedAtUtc = now;
            row.summary.SocialInsuranceDeductionAmount = row.detail.TotalDeductionAmount;
            row.summary.UpdatedAtUtc = now;
            row.summary.UpdatedBy = SystemActor;
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new RefreshPayrollInsuranceDeductionResult(
            targetPayrollMonth,
            targetPayrollYear,
            rows.Count,
            updatedCount,
            skippedLockedCount);
    }

    public async Task<SyncPayrollInsuranceDeductionFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncPayrollInsuranceDeductionFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = auditScope.Current
            ?? throw new InvalidOperationException("Thiếu audit scope cho thao tác lấy khấu trừ BHXH-YT từ tháng trước.");

        return await auditedMutation.ExecuteAsync(
            command with { ActionIntent = AuditActions.PayrollInsuranceDeduction.SyncedFromPreviousMonth },
            token => SyncFromPreviousMonthCoreAsync(request, token),
            result => new AuditOperationEvent(
                AuditActions.PayrollInsuranceDeduction.SyncedFromPreviousMonth,
                AuditEntityTypes.PayrollInsuranceDeduction,
                EntityDisplayName: $"{result.TargetPayrollMonth:00}/{result.TargetPayrollYear}",
                Metadata: new Dictionary<string, string>
                {
                    ["payrollPeriod"] = $"{result.TargetPayrollMonth:00}/{result.TargetPayrollYear}",
                    ["sourcePayrollPeriod"] = $"{result.SourcePayrollMonth:00}/{result.SourcePayrollYear}",
                    ["createdCount"] = result.CreatedCount.ToString(),
                    ["updatedCount"] = result.UpdatedCount.ToString(),
                    ["skippedLockedCount"] = result.SkippedLockedCount.ToString()
                }),
            cancellationToken);
    }

    private async Task<SyncPayrollInsuranceDeductionFromPreviousMonthResult> SyncFromPreviousMonthCoreAsync(
        SyncPayrollInsuranceDeductionFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.TargetPayrollMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng kỳ lương phải nằm trong khoảng từ 1 đến 12.");
        }

        if (request.TargetPayrollYear is < 2000 or > 2100)
        {
            throw new InvalidOperationException("Năm kỳ lương không hợp lệ.");
        }

        var targetPayrollMonth = (short)request.TargetPayrollMonth;
        var targetPayrollYear = (short)request.TargetPayrollYear;
        ValidateSyncTargetPeriod(targetPayrollMonth, targetPayrollYear);
        var (sourcePayrollMonth, sourcePayrollYear) = GetPreviousPayrollPeriod(targetPayrollMonth, targetPayrollYear);
        var targetEmployeeIds = await GetTargetEmployeeIdsAsync(targetPayrollMonth, targetPayrollYear, cancellationToken);

        if (targetEmployeeIds.Length == 0)
        {
            return new SyncPayrollInsuranceDeductionFromPreviousMonthResult(
                sourcePayrollMonth,
                sourcePayrollYear,
                targetPayrollMonth,
                targetPayrollYear,
                0,
                0,
                0,
                0,
                0,
                0,
                0);
        }

        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        var (_, seededSourceSummaryCount) = await EnsureSummariesForPeriodAsync(
            targetEmployeeIds,
            sourcePayrollMonth,
            sourcePayrollYear,
            now,
            cancellationToken);

        var (targetSummaries, seededTargetSummaryCount) = await EnsureSummariesForPeriodAsync(
            targetEmployeeIds,
            targetPayrollMonth,
            targetPayrollYear,
            now,
            cancellationToken);

        var sourceRows = await (
                from detail in dbContext.PayrollDeductionInsuranceRecords.AsNoTracking()
                join summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
                    on detail.PayrollDeductionSummaryRecordId equals summary.Id
                where summary.PayrollMonth == sourcePayrollMonth
                      && summary.PayrollYear == sourcePayrollYear
                      && targetEmployeeIds.Contains(summary.EmployeeId)
                orderby summary.EmployeeId, detail.CreatedAtUtc descending
                select new { detail, summary })
            .ToListAsync(cancellationToken);

        if (sourceRows.Count == 0)
        {
            return new SyncPayrollInsuranceDeductionFromPreviousMonthResult(
                sourcePayrollMonth,
                sourcePayrollYear,
                targetPayrollMonth,
                targetPayrollYear,
                targetEmployeeIds.Length,
                seededSourceSummaryCount,
                seededTargetSummaryCount,
                0,
                0,
                0,
                0);
        }

        var targetSummaryByEmployeeId = targetSummaries
            .GroupBy(summary => summary.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(summary => summary.CreatedAtUtc)
                    .ThenByDescending(summary => summary.Id)
                    .First());

        var targetSummaryIds = targetSummaries
            .Select(summary => summary.Id)
            .Distinct()
            .ToArray();

        var targetDetails = targetSummaryIds.Length == 0
            ? []
            : await dbContext.PayrollDeductionInsuranceRecords
                .Where(detail => targetSummaryIds.Contains(detail.PayrollDeductionSummaryRecordId))
                .ToListAsync(cancellationToken);

        var targetDetailBySummaryId = targetDetails.ToDictionary(detail => detail.PayrollDeductionSummaryRecordId);
        var createdCount = 0;
        var updatedCount = 0;
        var skippedLockedCount = 0;

        foreach (var sourceRow in sourceRows)
        {
            if (!targetSummaryByEmployeeId.TryGetValue(sourceRow.summary.EmployeeId, out var targetSummary))
            {
                targetSummary = new PayrollDeductionSummaryRecordRow
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = sourceRow.summary.EmployeeId,
                    PayrollMonth = targetPayrollMonth,
                    PayrollYear = targetPayrollYear,
                    IsLocked = false,
                    CreatedBy = SystemActor,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                dbContext.PayrollDeductionSummaryRecords.Add(targetSummary);
                targetSummaryByEmployeeId[sourceRow.summary.EmployeeId] = targetSummary;
            }
            else
            {
                targetSummary.EmployeeId = sourceRow.summary.EmployeeId;
                targetSummary.PayrollMonth = targetPayrollMonth;
                targetSummary.PayrollYear = targetPayrollYear;
                EnsureSummaryDefaults(targetSummary);
                targetSummary.UpdatedAtUtc = now;
                targetSummary.UpdatedBy = SystemActor;
            }

            if (targetSummary.IsLocked)
            {
                skippedLockedCount++;
                continue;
            }

            if (targetDetailBySummaryId.TryGetValue(targetSummary.Id, out var targetDetail))
            {
                if (targetDetail.IsLocked)
                {
                    skippedLockedCount++;
                    continue;
                }

                ApplySyncValues(sourceRow.detail, targetDetail, now);
                targetSummary.SocialInsuranceDeductionAmount = targetDetail.TotalDeductionAmount;
                updatedCount++;
                continue;
            }

            var newDetail = new PayrollDeductionInsuranceRecordRow
            {
                PayrollDeductionSummaryRecordId = targetSummary.Id,
                CreatedAtUtc = now
            };

            ApplySyncValues(sourceRow.detail, newDetail, now);
            dbContext.PayrollDeductionInsuranceRecords.Add(newDetail);
            targetDetailBySummaryId[targetSummary.Id] = newDetail;
            targetSummary.SocialInsuranceDeductionAmount = newDetail.TotalDeductionAmount;
            createdCount++;
        }

        if (createdCount > 0 || updatedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SyncPayrollInsuranceDeductionFromPreviousMonthResult(
            sourcePayrollMonth,
            sourcePayrollYear,
            targetPayrollMonth,
            targetPayrollYear,
            targetEmployeeIds.Length,
            seededSourceSummaryCount,
            seededTargetSummaryCount,
            sourceRows.Count,
            createdCount,
            updatedCount,
            skippedLockedCount);
    }

    /// <summary>
    /// Cập nhật các giá trị BHXH-YT được phép điều chỉnh thủ công. Danh tính, kỳ lương,
    /// trạng thái khóa và tổng tiền luôn do bản ghi hiện tại ở server quyết định.
    /// </summary>
    public async Task<PayrollInsuranceDeductionListItemDto> UpdateManualValuesAsync(
        UpdatePayrollInsuranceDeductionManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateManualValuesRequest(request);

        var detail = await dbContext.PayrollDeductionInsuranceRecords
            .SingleOrDefaultAsync(
                row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId,
                cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng khấu trừ BHXH-YT để điều chỉnh.");

        var summary = await dbContext.PayrollDeductionSummaryRecords
            .SingleOrDefaultAsync(
                row => row.Id == detail.PayrollDeductionSummaryRecordId,
                cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng tổng kết khấu trừ liên quan.");

        if (PayrollInsuranceDeductionLockPolicy.Evaluate(
                new PayrollInsuranceDeductionLockInput(detail.IsLocked, summary.IsLocked))
            == PayrollInsuranceDeductionLockDecision.Locked)
        {
            throw new InvalidOperationException("Dòng khấu trừ BHXH-YT đã khóa nên không thể điều chỉnh.");
        }

        var command = auditScope.Current
            ?? throw new InvalidOperationException("Thiếu audit scope cho thao tác điều chỉnh khấu trừ BHXH-YT.");
        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        var actor = NormalizeActor(command.Actor.ActorId);
        var calculatedValues = CalculateValues(request);

        await auditedMutation.ExecuteAsync(
            command with { ActionIntent = AuditActions.PayrollInsuranceDeduction.ManualValuesUpdated },
            async token =>
            {
                var detailUpdatedCount = await dbContext.PayrollDeductionInsuranceRecords
                    .Where(row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId
                        && !row.IsLocked
                        && (row.UpdatedAtUtc ?? row.CreatedAtUtc) == request.OriginalUpdatedAtUtc)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(row => row.InsuranceSalaryBaseAmount, request.InsuranceSalaryBaseAmount)
                            .SetProperty(row => row.SocialInsuranceRate, request.SocialInsuranceRate)
                            .SetProperty(row => row.HealthInsuranceRate, request.HealthInsuranceRate)
                            .SetProperty(row => row.UnemploymentInsuranceRate, request.UnemploymentInsuranceRate)
                            .SetProperty(row => row.IsParticipating, request.IsParticipating)
                            .SetProperty(row => row.ParticipationChangeType, request.ParticipationChangeType)
                            .SetProperty(row => row.EffectiveDate, request.EffectiveDate)
                            .SetProperty(row => row.TotalInsuranceRate, calculatedValues.TotalInsuranceRate)
                            .SetProperty(row => row.SocialInsuranceAmount, calculatedValues.SocialInsuranceAmount)
                            .SetProperty(row => row.HealthInsuranceAmount, calculatedValues.HealthInsuranceAmount)
                            .SetProperty(row => row.UnemploymentInsuranceAmount, calculatedValues.UnemploymentInsuranceAmount)
                            .SetProperty(row => row.TotalDeductionAmount, calculatedValues.TotalDeductionAmount)
                            .SetProperty(row => row.UpdatedAtUtc, now),
                        token);

                if (detailUpdatedCount != 1)
                {
                    throw new PayrollInsuranceDeductionConcurrencyException(
                        "Dòng khấu trừ BHXH-YT đã được thay đổi hoặc khóa bởi thao tác khác. Vui lòng tải lại dữ liệu.");
                }

                var summaryUpdatedCount = await dbContext.PayrollDeductionSummaryRecords
                    .Where(row => row.Id == request.PayrollDeductionSummaryRecordId && !row.IsLocked)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(row => row.SocialInsuranceDeductionAmount, calculatedValues.TotalDeductionAmount)
                            .SetProperty(row => row.UpdatedAtUtc, now)
                            .SetProperty(row => row.UpdatedBy, actor),
                        token);

                if (summaryUpdatedCount != 1)
                {
                    throw new InvalidOperationException(
                        "Dòng tổng kết khấu trừ đã được khóa hoặc không còn tồn tại. Vui lòng tải lại dữ liệu.");
                }

                return true;
            },
            _ => new AuditOperationEvent(
                AuditActions.PayrollInsuranceDeduction.ManualValuesUpdated,
                AuditEntityTypes.PayrollInsuranceDeduction,
                request.PayrollDeductionSummaryRecordId.ToString("D"),
                Metadata: new Dictionary<string, string>
                {
                    ["concurrencyTokenProvided"] = bool.TrueString,
                    ["payrollPeriod"] = $"{summary.PayrollMonth:00}/{summary.PayrollYear}"
                }),
            cancellationToken);

        dbContext.ChangeTracker.Clear();
        return await GetByIdAsync(request.PayrollDeductionSummaryRecordId, cancellationToken)
               ?? throw new InvalidOperationException("Không thể tải lại dòng khấu trừ BHXH-YT vừa điều chỉnh.");
    }

    /// <summary>
    /// Đổi trạng thái khóa của detail BHXH-YT. Summary khấu trừ là khóa cấp cha
    /// dùng chung, vì vậy command này không thay đổi summary và từ chối khi cha đã khóa.
    /// </summary>
    public async Task<PayrollInsuranceDeductionListItemDto> SetLockStateAsync(
        SetPayrollInsuranceDeductionLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.PayrollDeductionSummaryRecordId == Guid.Empty)
        {
            throw new InvalidOperationException("Thiếu dòng khấu trừ BHXH-YT để khóa hoặc mở khóa.");
        }

        if (request.OriginalUpdatedAtUtc == default)
        {
            throw new InvalidOperationException("Thiếu phiên bản dữ liệu của dòng khấu trừ BHXH-YT.");
        }

        var detail = await dbContext.PayrollDeductionInsuranceRecords
            .SingleOrDefaultAsync(
                row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId,
                cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng khấu trừ BHXH-YT để khóa hoặc mở khóa.");

        var summary = await dbContext.PayrollDeductionSummaryRecords
            .SingleOrDefaultAsync(
                row => row.Id == request.PayrollDeductionSummaryRecordId,
                cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng tổng kết khấu trừ liên quan.");

        if (summary.IsLocked)
        {
            throw new InvalidOperationException(
                "Dòng tổng kết khấu trừ đã khóa. Hãy mở khóa ở màn Tổng kết khấu trừ trước khi thay đổi dòng BHXH-YT.");
        }

        // Repeated requests that already match the effective detail state are idempotent.
        if (detail.IsLocked == request.IsLocked)
        {
            return await GetByIdAsync(request.PayrollDeductionSummaryRecordId, cancellationToken)
                   ?? throw new InvalidOperationException("Không thể tải lại dòng khấu trừ BHXH-YT.");
        }

        var command = auditScope.Current
            ?? throw new InvalidOperationException("Thiếu audit scope cho thao tác khóa hoặc mở khóa khấu trừ BHXH-YT.");
        var now = ToDatabaseTimestamp(DateTime.UtcNow);

        await auditedMutation.ExecuteAsync(
            command with { ActionIntent = AuditActions.PayrollInsuranceDeduction.LockStateChanged },
            async token =>
            {
                var updatedCount = await dbContext.PayrollDeductionInsuranceRecords
                    .Where(row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId
                        && (row.UpdatedAtUtc ?? row.CreatedAtUtc) == request.OriginalUpdatedAtUtc
                        && dbContext.PayrollDeductionSummaryRecords.Any(summary =>
                            summary.Id == row.PayrollDeductionSummaryRecordId
                            && !summary.IsLocked))
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(row => row.IsLocked, request.IsLocked)
                            .SetProperty(row => row.UpdatedAtUtc, now),
                        token);

                if (updatedCount != 1)
                {
                    throw new PayrollInsuranceDeductionConcurrencyException(
                        "Dòng khấu trừ BHXH-YT đã được thay đổi hoặc khóa bởi thao tác khác. Vui lòng tải lại dữ liệu.");
                }

                return true;
            },
            _ => new AuditOperationEvent(
                AuditActions.PayrollInsuranceDeduction.LockStateChanged,
                AuditEntityTypes.PayrollInsuranceDeduction,
                request.PayrollDeductionSummaryRecordId.ToString("D"),
                Metadata: new Dictionary<string, string>
                {
                    ["isLocked"] = request.IsLocked.ToString(),
                    ["concurrencyTokenProvided"] = bool.TrueString,
                    ["payrollPeriod"] = $"{summary.PayrollMonth:00}/{summary.PayrollYear}"
                }),
            cancellationToken);

        dbContext.ChangeTracker.Clear();
        return await GetByIdAsync(request.PayrollDeductionSummaryRecordId, cancellationToken)
               ?? throw new InvalidOperationException("Không thể tải lại dòng khấu trừ BHXH-YT vừa cập nhật trạng thái khóa.");
    }

    /// <summary>
    /// Khóa hoặc mở khóa theo lô trong phạm vi một kỳ lương. Summary khấu trừ là
    /// khóa cấp cha nên toàn bộ command bị từ chối nếu một target đã bị khóa ở cấp này.
    /// </summary>
    public async Task<SetPayrollInsuranceDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollInsuranceDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePayrollPeriod(request.PayrollMonth, request.PayrollYear);

        var hasExplicitTargets = request.PayrollDeductionSummaryRecordIds is not null;
        var normalizedIds = request.PayrollDeductionSummaryRecordIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (hasExplicitTargets && (normalizedIds is null || normalizedIds.Length == 0))
        {
            return new SetPayrollInsuranceDeductionBatchLockStateResult(
                request.PayrollYear,
                request.PayrollMonth,
                0,
                0);
        }

        var targetQuery =
            from detail in dbContext.PayrollDeductionInsuranceRecords
            join summary in dbContext.PayrollDeductionSummaryRecords
                on detail.PayrollDeductionSummaryRecordId equals summary.Id
            where summary.PayrollMonth == request.PayrollMonth
                  && summary.PayrollYear == request.PayrollYear
            select new
            {
                detail.PayrollDeductionSummaryRecordId,
                DetailIsLocked = detail.IsLocked,
                SummaryIsLocked = summary.IsLocked
            };

        if (hasExplicitTargets)
        {
            targetQuery = targetQuery.Where(row => normalizedIds!.Contains(row.PayrollDeductionSummaryRecordId));
        }

        var targets = await targetQuery.ToListAsync(cancellationToken);
        if (hasExplicitTargets && targets.Count != normalizedIds!.Length)
        {
            throw new InvalidOperationException(
                "Có dòng khấu trừ BHXH-YT không còn tồn tại hoặc không thuộc kỳ lương đã áp dụng. Vui lòng tải lại dữ liệu.");
        }

        if (targets.Any(row => row.SummaryIsLocked))
        {
            throw new InvalidOperationException(
                "Có dòng tổng kết khấu trừ đã khóa. Hãy mở khóa ở màn Tổng kết khấu trừ trước khi thay đổi trạng thái BHXH-YT.");
        }

        var targetRowCount = targets.Count;
        var expectedUpdatedCount = targets.Count(row => row.DetailIsLocked != request.IsLocked);
        if (targetRowCount == 0 || expectedUpdatedCount == 0)
        {
            return new SetPayrollInsuranceDeductionBatchLockStateResult(
                request.PayrollYear,
                request.PayrollMonth,
                targetRowCount,
                0);
        }

        var command = auditScope.Current
            ?? throw new InvalidOperationException("Thiếu audit scope cho thao tác khóa hoặc mở khóa hàng loạt khấu trừ BHXH-YT.");
        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        var targetIds = targets.Select(row => row.PayrollDeductionSummaryRecordId).ToArray();

        return await auditedMutation.ExecuteAsync(
            command with { ActionIntent = AuditActions.PayrollInsuranceDeduction.BatchLockStateChanged },
            async token =>
            {
                var updatedCount = await dbContext.PayrollDeductionInsuranceRecords
                    .Where(row => targetIds.Contains(row.PayrollDeductionSummaryRecordId)
                        && row.IsLocked != request.IsLocked
                        && dbContext.PayrollDeductionSummaryRecords.Any(summary =>
                            summary.Id == row.PayrollDeductionSummaryRecordId
                            && !summary.IsLocked))
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(row => row.IsLocked, request.IsLocked)
                            .SetProperty(row => row.UpdatedAtUtc, now),
                        token);

                if (updatedCount != expectedUpdatedCount)
                {
                    throw new PayrollInsuranceDeductionConcurrencyException(
                        "Dữ liệu khấu trừ BHXH-YT hoặc trạng thái khóa tổng kết đã thay đổi. Vui lòng tải lại dữ liệu.");
                }

                return new SetPayrollInsuranceDeductionBatchLockStateResult(
                    request.PayrollYear,
                    request.PayrollMonth,
                    targetRowCount,
                    updatedCount);
            },
            result => new AuditOperationEvent(
                AuditActions.PayrollInsuranceDeduction.BatchLockStateChanged,
                AuditEntityTypes.PayrollInsuranceDeduction,
                EntityDisplayName: $"{result.PayrollMonth:00}/{result.PayrollYear}",
                Metadata: new Dictionary<string, string>
                {
                    ["scope"] = hasExplicitTargets ? "selected-rows" : "whole-period",
                    ["isLocked"] = request.IsLocked.ToString(),
                    ["targetRowCount"] = result.TargetRowCount.ToString(),
                    ["updatedCount"] = result.UpdatedCount.ToString(),
                    ["payrollPeriod"] = $"{result.PayrollMonth:00}/{result.PayrollYear}"
                }),
            cancellationToken);
    }

    public async Task<string?> ValidateAsync(
        UpsertPayrollInsuranceDeductionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.EmployeeId == Guid.Empty)
        {
            return "Nhân viên không được để trống.";
        }

        if (request.PayrollMonth is < 1 or > 12)
        {
            return "Tháng kỳ lương phải nằm trong khoảng từ 1 đến 12.";
        }

        if (request.PayrollYear is < 2000 or > 2100)
        {
            return "Năm kỳ lương không hợp lệ.";
        }

        var requestValidationMessage = PayrollInsuranceDeductionRequestValidator.Validate(
            new PayrollInsuranceDeductionValidationInput(
                request.InsuranceSalaryBaseAmount,
                request.SocialInsuranceRate,
                request.HealthInsuranceRate,
                request.UnemploymentInsuranceRate,
                request.ParticipationChangeType),
            "Mức tiền chuẩn không được âm.");
        if (!string.IsNullOrWhiteSpace(requestValidationMessage))
        {
            return requestValidationMessage;
        }

        var employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(employee => employee.Id == request.EmployeeId && !employee.IsDeleted, cancellationToken);
        if (!employeeExists)
        {
            return "Nhân viên đã chọn không tồn tại hoặc đã nghỉ việc.";
        }

        var duplicateExists = await (
                from detail in dbContext.PayrollDeductionInsuranceRecords.AsNoTracking()
                join summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
                    on detail.PayrollDeductionSummaryRecordId equals summary.Id
                where summary.EmployeeId == request.EmployeeId
                      && summary.PayrollMonth == request.PayrollMonth
                      && summary.PayrollYear == request.PayrollYear
                      && detail.PayrollDeductionSummaryRecordId != request.Id
                select detail.PayrollDeductionSummaryRecordId)
            .AnyAsync(cancellationToken);

        return duplicateExists
            ? "Đã tồn tại dòng khấu trừ BHXH-YT cho nhân viên này trong kỳ lương đã chọn."
            : null;
    }

    public async Task<PayrollInsuranceDeductionListItemDto> SaveAsync(
        UpsertPayrollInsuranceDeductionRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var command = auditScope.Current
            ?? throw new InvalidOperationException("Thiếu audit scope cho thao tác tạo khấu trừ BHXH-YT.");

        return await auditedMutation.ExecuteAsync(
            command with { ActionIntent = AuditActions.PayrollInsuranceDeduction.Created },
            token => SaveCoreAsync(request, isNew, token),
            result => new AuditOperationEvent(
                AuditActions.PayrollInsuranceDeduction.Created,
                AuditEntityTypes.PayrollInsuranceDeduction,
                result.PayrollDeductionSummaryRecordId.ToString("D"),
                Metadata: new Dictionary<string, string>
                {
                    ["payrollPeriod"] = $"{result.PayrollMonth:00}/{result.PayrollYear}",
                    ["employeeId"] = result.EmployeeId.ToString("D")
                }),
            cancellationToken);
    }

    private async Task<PayrollInsuranceDeductionListItemDto> SaveCoreAsync(
        UpsertPayrollInsuranceDeductionRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        if (!isNew)
        {
            throw new InvalidOperationException(
                "Điều chỉnh khấu trừ BHXH-YT phải dùng command điều chỉnh thủ công.");
        }

        var validationMessage = await ValidateAsync(request, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            throw new InvalidOperationException(validationMessage);
        }

        var normalizedId = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        var now = DateTime.UtcNow;
        var nowTimestamp = ToDatabaseTimestamp(now);
        var normalizedCreatedAt = request.CreatedAtUtc == default ? now : request.CreatedAtUtc;
        var normalizedCreatedAtTimestamp = ToDatabaseTimestamp(normalizedCreatedAt);
        PayrollDeductionInsuranceRecordRow detailRow;
        PayrollDeductionSummaryRecordRow summaryRow;

        if (isNew)
        {
            summaryRow = await FindOrCreateSummaryAsync(
                request.EmployeeId,
                (short)request.PayrollMonth,
                (short)request.PayrollYear,
                normalizedCreatedAtTimestamp,
                nowTimestamp,
                cancellationToken);

            detailRow = await dbContext.PayrollDeductionInsuranceRecords.SingleOrDefaultAsync(
                            item => item.PayrollDeductionSummaryRecordId == summaryRow.Id,
                            cancellationToken)
                        ?? new PayrollDeductionInsuranceRecordRow
                        {
                            PayrollDeductionSummaryRecordId = summaryRow.Id,
                            CreatedAtUtc = normalizedCreatedAtTimestamp
                        };

            if (dbContext.Entry(detailRow).State == EntityState.Detached)
            {
                dbContext.PayrollDeductionInsuranceRecords.Add(detailRow);
            }
        }
        else
        {
            detailRow = await dbContext.PayrollDeductionInsuranceRecords.SingleOrDefaultAsync(
                            item => item.PayrollDeductionSummaryRecordId == normalizedId,
                            cancellationToken)
                        ?? throw new InvalidOperationException("Không tìm thấy dòng khấu trừ BHXH-YT để cập nhật.");

            summaryRow = await dbContext.PayrollDeductionSummaryRecords.SingleAsync(
                item => item.Id == detailRow.PayrollDeductionSummaryRecordId,
                cancellationToken);

            if (summaryRow.CreatedAtUtc == default)
            {
                summaryRow.CreatedAtUtc = normalizedCreatedAtTimestamp;
            }

            if (detailRow.CreatedAtUtc == default)
            {
                detailRow.CreatedAtUtc = normalizedCreatedAtTimestamp;
            }

            var targetSummary = await dbContext.PayrollDeductionSummaryRecords.SingleOrDefaultAsync(
                item => item.EmployeeId == request.EmployeeId
                        && item.PayrollMonth == request.PayrollMonth
                        && item.PayrollYear == request.PayrollYear,
                cancellationToken);

            if (targetSummary is not null && targetSummary.Id != summaryRow.Id)
            {
                var targetHasDetail = await dbContext.PayrollDeductionInsuranceRecords
                    .AnyAsync(item => item.PayrollDeductionSummaryRecordId == targetSummary.Id, cancellationToken);

                if (targetHasDetail)
                {
                    throw new InvalidOperationException("Đã tồn tại dòng khấu trừ BHXH-YT cho nhân viên này trong kỳ lương đã chọn.");
                }

                detailRow.PayrollDeductionSummaryRecordId = targetSummary.Id;
                EnsureSummaryDefaults(targetSummary);
                targetSummary.UpdatedAtUtc = nowTimestamp;
                targetSummary.UpdatedBy = SystemActor;
                summaryRow = targetSummary;
            }
            else
            {
                summaryRow.EmployeeId = request.EmployeeId;
                summaryRow.PayrollMonth = (short)request.PayrollMonth;
                summaryRow.PayrollYear = (short)request.PayrollYear;
                EnsureSummaryDefaults(summaryRow);
                summaryRow.UpdatedAtUtc = nowTimestamp;
                summaryRow.UpdatedBy = SystemActor;
            }
        }

        summaryRow.EmployeeId = request.EmployeeId;
        summaryRow.PayrollMonth = (short)request.PayrollMonth;
        summaryRow.PayrollYear = (short)request.PayrollYear;
        EnsureSummaryDefaults(summaryRow);
        if (summaryRow.IsLocked)
        {
            throw new InvalidOperationException("Dòng tổng kết khấu trừ đã khóa nên không thể cập nhật BHXH.");
        }
        summaryRow.UpdatedAtUtc = nowTimestamp;
        summaryRow.UpdatedBy = SystemActor;

        detailRow.PayrollDeductionSummaryRecordId = summaryRow.Id;
        detailRow.InsuranceSalaryBaseAmount = request.InsuranceSalaryBaseAmount;
        detailRow.SocialInsuranceRate = request.SocialInsuranceRate;
        detailRow.HealthInsuranceRate = request.HealthInsuranceRate;
        detailRow.UnemploymentInsuranceRate = request.UnemploymentInsuranceRate;
        detailRow.IsParticipating = request.IsParticipating;
        detailRow.ParticipationChangeType = request.ParticipationChangeType;
        detailRow.EffectiveDate = request.EffectiveDate;
        ApplyCalculatedValues(detailRow);
        detailRow.IsLocked = request.IsLocked;
        detailRow.UpdatedAtUtc = nowTimestamp;
        summaryRow.SocialInsuranceDeductionAmount = detailRow.TotalDeductionAmount;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(detailRow.PayrollDeductionSummaryRecordId, cancellationToken)
               ?? throw new InvalidOperationException("Không thể tải lại dòng khấu trừ BHXH-YT vừa lưu.");
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var command = auditScope.Current
            ?? throw new InvalidOperationException("Thiếu audit scope cho thao tác xóa khấu trừ BHXH-YT.");

        await auditedMutation.ExecuteAsync(
            command with { ActionIntent = AuditActions.PayrollInsuranceDeduction.Deleted },
            async token =>
            {
                var deletedCount = await DeleteCoreAsync(ids, token);
                return deletedCount;
            },
            deletedCount => new AuditOperationEvent(
                AuditActions.PayrollInsuranceDeduction.Deleted,
                AuditEntityTypes.PayrollInsuranceDeduction,
                EntityDisplayName: $"{deletedCount} dòng",
                Metadata: new Dictionary<string, string>
                {
                    ["requestedCount"] = ids.Count.ToString(),
                    ["deletedCount"] = deletedCount.ToString()
                }),
            cancellationToken);
    }

    private async Task<int> DeleteCoreAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ids.Count == 0)
        {
            return 0;
        }

        var normalizedIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return 0;
        }

        var details = await dbContext.PayrollDeductionInsuranceRecords
            .Where(item => normalizedIds.Contains(item.PayrollDeductionSummaryRecordId))
            .ToListAsync(cancellationToken);
        if (details.Count == 0)
        {
            return 0;
        }

        var summaryIds = details
            .Select(item => item.PayrollDeductionSummaryRecordId)
            .Distinct()
            .ToArray();

        var summaries = await dbContext.PayrollDeductionSummaryRecords
            .Where(summary => summaryIds.Contains(summary.Id))
            .ToListAsync(cancellationToken);

        if (summaries.Any(summary => summary.IsLocked))
        {
            throw new InvalidOperationException("Dòng tổng kết khấu trừ đã khóa nên không thể xóa BHXH.");
        }

        foreach (var summary in summaries)
        {
            summary.SocialInsuranceDeductionAmount = 0m;
            summary.UpdatedAtUtc = ToDatabaseTimestamp(DateTime.UtcNow);
            summary.UpdatedBy = SystemActor;
        }

        dbContext.PayrollDeductionInsuranceRecords.RemoveRange(details);

        await dbContext.SaveChangesAsync(cancellationToken);
        return details.Count;
    }

    private async Task<PayrollInsuranceDeductionListItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query =
            from detail in dbContext.PayrollDeductionInsuranceRecords.AsNoTracking()
            where detail.PayrollDeductionSummaryRecordId == id
            join summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
                on detail.PayrollDeductionSummaryRecordId equals summary.Id
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select MapToDto(detail, summary, employee, department, position);

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<PayrollDeductionSummaryRecordRow> FindOrCreateSummaryAsync(
        Guid employeeId,
        short payrollMonth,
        short payrollYear,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var existingSummary = await dbContext.PayrollDeductionSummaryRecords.SingleOrDefaultAsync(
            item => item.EmployeeId == employeeId
                    && item.PayrollMonth == payrollMonth
                    && item.PayrollYear == payrollYear,
            cancellationToken);

        if (existingSummary is not null)
        {
            existingSummary.UpdatedAtUtc = updatedAtUtc;
            return existingSummary;
        }

        var summary = new PayrollDeductionSummaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayrollMonth = payrollMonth,
            PayrollYear = payrollYear,
            IsLocked = false,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = SystemActor,
            UpdatedAtUtc = updatedAtUtc,
            UpdatedBy = SystemActor
        };

        dbContext.PayrollDeductionSummaryRecords.Add(summary);
        return summary;
    }

    private static void EnsureSummaryDefaults(PayrollDeductionSummaryRecordRow summary)
    {
        summary.CreatedBy = NormalizeActor(summary.CreatedBy);
        summary.UpdatedBy = NormalizeOptional(summary.UpdatedBy);
        summary.Note = NormalizeOptional(summary.Note);
    }

    private async Task<Guid[]> GetTargetEmployeeIdsAsync(
        short targetPayrollMonth,
        short targetPayrollYear,
        CancellationToken cancellationToken)
    {
        var periodStart = new DateOnly(targetPayrollYear, targetPayrollMonth, 1);
        var periodEnd = periodStart.AddMonths(1);

        return await dbContext.AttendanceWorkdaySummaries
            .AsNoTracking()
            .Where(summary => summary.WorkDate >= periodStart && summary.WorkDate < periodEnd)
            .Select(summary => summary.EmployeeId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }

    private async Task<(List<PayrollDeductionSummaryRecordRow> Summaries, int CreatedCount)> EnsureSummariesForPeriodAsync(
        IReadOnlyCollection<Guid> employeeIds,
        short payrollMonth,
        short payrollYear,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var summaries = await dbContext.PayrollDeductionSummaryRecords
            .Where(summary =>
                summary.PayrollMonth == payrollMonth
                && summary.PayrollYear == payrollYear
                && employeeIds.Contains(summary.EmployeeId))
            .ToListAsync(cancellationToken);

        var summaryByEmployeeId = summaries
            .GroupBy(summary => summary.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(summary => summary.CreatedAtUtc)
                    .ThenByDescending(summary => summary.Id)
                    .First());

        var hasChanges = false;
        var createdCount = 0;
        foreach (var employeeId in employeeIds)
        {
            if (summaryByEmployeeId.TryGetValue(employeeId, out var existingSummary))
            {
                EnsureSummaryDefaults(existingSummary);
                continue;
            }

            var newSummary = new PayrollDeductionSummaryRecordRow
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                PayrollMonth = payrollMonth,
                PayrollYear = payrollYear,
                IsLocked = false,
                CreatedAtUtc = now,
                CreatedBy = SystemActor,
                UpdatedAtUtc = now,
                UpdatedBy = SystemActor
            };

            dbContext.PayrollDeductionSummaryRecords.Add(newSummary);
            summaries.Add(newSummary);
            summaryByEmployeeId[employeeId] = newSummary;
            hasChanges = true;
            createdCount++;
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return (summaries, createdCount);
    }

    private static string NormalizeActor(string? actor)
    {
        var normalizedActor = NormalizeOptional(actor);
        if (string.IsNullOrWhiteSpace(normalizedActor))
        {
            return SystemActor;
        }

        return normalizedActor.Length <= 128
            ? normalizedActor
            : normalizedActor[..128];
    }

    private static void ValidateManualValuesRequest(UpdatePayrollInsuranceDeductionManualValuesRequest request)
    {
        if (request.PayrollDeductionSummaryRecordId == Guid.Empty)
        {
            throw new InvalidOperationException("Thiếu dòng khấu trừ BHXH-YT để điều chỉnh.");
        }

        PayrollInsuranceDeductionConcurrencyPolicy.EnsureExpectedVersionProvided(request.OriginalUpdatedAtUtc);

        PayrollInsuranceDeductionRequestValidator.EnsureValid(
            new PayrollInsuranceDeductionValidationInput(
                request.InsuranceSalaryBaseAmount,
                request.SocialInsuranceRate,
                request.HealthInsuranceRate,
                request.UnemploymentInsuranceRate,
                request.ParticipationChangeType),
            "Tổng tiền lương đóng BHXH không được âm.");
    }

    private static PayrollInsuranceDeductionCalculatedValues CalculateValues(UpdatePayrollInsuranceDeductionManualValuesRequest request) =>
        PayrollInsuranceDeductionCalculator.Calculate(
            new PayrollInsuranceDeductionCalculationInput(
                request.InsuranceSalaryBaseAmount,
                request.SocialInsuranceRate,
                request.HealthInsuranceRate,
                request.UnemploymentInsuranceRate,
                request.IsParticipating
                    ? InsuranceParticipationStatus.Participating
                    : InsuranceParticipationStatus.NotParticipating));

    private static PayrollInsuranceDeductionListItemDto MapToDto(
        PayrollDeductionInsuranceRecordRow detail,
        PayrollDeductionSummaryRecordRow summary,
        AttendanceGatewayEmployeeRow? employee,
        AttendanceDepartmentRow? department,
        AttendanceGatewayPositionRow? position) =>
        new(
            detail.PayrollDeductionSummaryRecordId,
            summary.Id,
            summary.EmployeeId,
            employee?.EmployeeCode,
            employee is null ? null : BuildEmployeeName(employee),
            department is null ? null : BuildDepartmentName(department),
            position?.Name,
            summary.PayrollMonth,
            summary.PayrollYear,
            detail.InsuranceSalaryBaseAmount,
            detail.SocialInsuranceRate,
            detail.HealthInsuranceRate,
            detail.UnemploymentInsuranceRate,
            detail.TotalInsuranceRate,
            detail.SocialInsuranceAmount,
            detail.HealthInsuranceAmount,
            detail.UnemploymentInsuranceAmount,
            detail.TotalDeductionAmount,
            detail.IsParticipating,
            detail.ParticipationChangeType,
            detail.EffectiveDate,
            detail.IsLocked || summary.IsLocked,
            detail.CreatedAtUtc,
            detail.UpdatedAtUtc);

    private static string BuildEmployeeName(AttendanceGatewayEmployeeRow employee)
    {
        var parts = new[] { employee.LastName, employee.FirstName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part.Trim());

        return string.Join(" ", parts);
    }

    private static string BuildDepartmentName(AttendanceDepartmentRow department)
    {
        return NormalizeOptional(department.GroupName)
               ?? NormalizeOptional(department.TeamName)
               ?? NormalizeOptional(department.DepartmentOrWorkshopName)
               ?? NormalizeOptional(department.CenterName)
               ?? string.Empty;
    }

    private static void ApplySyncValues(
        PayrollDeductionInsuranceRecordRow sourceRow,
        PayrollDeductionInsuranceRecordRow targetRow,
        DateTime updatedAtUtc)
    {
        targetRow.InsuranceSalaryBaseAmount = sourceRow.InsuranceSalaryBaseAmount;
        targetRow.SocialInsuranceRate = sourceRow.SocialInsuranceRate;
        targetRow.HealthInsuranceRate = sourceRow.HealthInsuranceRate;
        targetRow.UnemploymentInsuranceRate = sourceRow.UnemploymentInsuranceRate;
        targetRow.IsParticipating = sourceRow.IsParticipating;
        targetRow.ParticipationChangeType = sourceRow.ParticipationChangeType;
        targetRow.EffectiveDate = sourceRow.EffectiveDate;
        ApplyCalculatedValues(targetRow);
        targetRow.IsLocked = false;
        targetRow.UpdatedAtUtc = updatedAtUtc;
    }

    private static void ApplyCalculatedValues(PayrollDeductionInsuranceRecordRow row)
    {
        var values = PayrollInsuranceDeductionCalculator.Calculate(
            new PayrollInsuranceDeductionCalculationInput(
                row.InsuranceSalaryBaseAmount,
                row.SocialInsuranceRate,
                row.HealthInsuranceRate,
                row.UnemploymentInsuranceRate,
                row.IsParticipating
                    ? InsuranceParticipationStatus.Participating
                    : InsuranceParticipationStatus.NotParticipating));
        row.TotalInsuranceRate = values.TotalInsuranceRate;
        row.SocialInsuranceAmount = values.SocialInsuranceAmount;
        row.HealthInsuranceAmount = values.HealthInsuranceAmount;
        row.UnemploymentInsuranceAmount = values.UnemploymentInsuranceAmount;
        row.TotalDeductionAmount = values.TotalDeductionAmount;
    }

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private static void ValidatePayrollPeriod(int payrollMonth, int payrollYear)
    {
        if (payrollMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng kỳ lương phải nằm trong khoảng từ 1 đến 12.");
        }

        if (payrollYear is < 2000 or > 2100)
        {
            throw new InvalidOperationException("Năm kỳ lương không hợp lệ.");
        }
    }

    private static void ValidateSyncTargetPeriod(short targetPayrollMonth, short targetPayrollYear)
    {
        var targetPeriod = new DateOnly(targetPayrollYear, targetPayrollMonth, 1);
        if (targetPeriod < MinimumSyncTargetPeriod)
        {
            throw new InvalidOperationException("Chỉ hỗ trợ lấy từ tháng trước từ kỳ 06/2026 trở đi.");
        }
    }

    private static (short Month, short Year) GetPreviousPayrollPeriod(short month, short year) =>
        month == 1
            ? ((short)12, (short)(year - 1))
            : ((short)(month - 1), year);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private Task EnsurePayrollInsuranceDeductionTablesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /*
    private async Task EnsurePayrollInsuranceDeductionTablesAsyncLegacy(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS public.payroll_decuction_summary_records (
                "Id" uuid NOT NULL,
                "EmployeeId" uuid NOT NULL,
                "PayrollMonth" smallint NOT NULL,
                "PayrollYear" smallint NOT NULL,
                "BhxhYtAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "CongDoanAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "ThueTncnAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "TamUngAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "KhacAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "IsLocked" boolean NOT NULL DEFAULT FALSE,
                "Note" text NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "CreatedBy" character varying(128) NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                "UpdatedBy" character varying(128) NULL,
                CONSTRAINT "PK_payroll_decuction_summary_records" PRIMARY KEY ("Id"),
                CONSTRAINT "CK_payroll_decuction_summary_records_PayrollMonth"
                    CHECK ("PayrollMonth" >= 1 AND "PayrollMonth" <= 12),
                CONSTRAINT "CK_payroll_decuction_summary_records_PayrollYear"
                    CHECK ("PayrollYear" >= 1 AND "PayrollYear" <= 9999),
                CONSTRAINT "CK_payroll_decuction_summary_records_BhxhYtAmount"
                    CHECK ("BhxhYtAmount" >= 0),
                CONSTRAINT "CK_payroll_decuction_summary_records_CongDoanAmount"
                    CHECK ("CongDoanAmount" >= 0),
                CONSTRAINT "CK_payroll_decuction_summary_records_ThueTncnAmount"
                    CHECK ("ThueTncnAmount" >= 0),
                CONSTRAINT "CK_payroll_decuction_summary_records_TamUngAmount"
                    CHECK ("TamUngAmount" >= 0),
                CONSTRAINT "CK_payroll_decuction_summary_records_KhacAmount"
                    CHECK ("KhacAmount" >= 0)
            );

            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "BhxhYtAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "CongDoanAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "ThueTncnAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "TamUngAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "KhacAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "IsLocked" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "Note" text NULL;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "CreatedBy" character varying(128) NULL;
            ALTER TABLE public.payroll_decuction_summary_records
                ADD COLUMN IF NOT EXISTS "UpdatedBy" character varying(128) NULL;

            UPDATE public.payroll_decuction_summary_records
            SET "CreatedBy" = 'system'
            WHERE "CreatedBy" IS NULL OR btrim("CreatedBy") = '';

            ALTER TABLE public.payroll_decuction_summary_records
                ALTER COLUMN "CreatedBy" SET NOT NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_decuction_summary_records_EmployeeId_PayrollYear_PayrollMonth"
            ON public.payroll_decuction_summary_records ("EmployeeId", "PayrollYear", "PayrollMonth");

            CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_summary_records_PayrollYear_PayrollMonth"
            ON public.payroll_decuction_summary_records ("PayrollYear", "PayrollMonth");

            CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_summary_records_IsLocked"
            ON public.payroll_decuction_summary_records ("IsLocked");

            DO $$
            BEGIN
                IF to_regclass('public.employees') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_payroll_decuction_summary_records_employees_EmployeeId'
                    )
                THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    ADD CONSTRAINT "FK_payroll_decuction_summary_records_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id")
                    ON DELETE RESTRICT;
                END IF;
            END $$;

            CREATE TABLE IF NOT EXISTS public.payroll_decuction_summary_insurance_details (
                "Id" uuid NOT NULL,
                "PayrollDeductionSummaryRecordId" uuid NOT NULL,
                "StandardAllowanceAmount" numeric(18,2) NOT NULL,
                "StandardWorkdayCount" numeric(10,2) NOT NULL,
                "ActualWorkdayCount" numeric(10,2) NOT NULL,
                "AttendanceRate" numeric(7,4) NOT NULL,
                "ActualAllowanceAmount" numeric(18,2) NOT NULL,
                "IsLocked" boolean NOT NULL DEFAULT FALSE,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_payroll_decuction_summary_insurance_details" PRIMARY KEY ("Id"),
                CONSTRAINT "CK_payroll_decuction_summary_insurance_details_StandardAllowanceAmount"
                    CHECK ("StandardAllowanceAmount" >= 0),
                CONSTRAINT "CK_payroll_decuction_summary_insurance_details_StandardWorkdayCount"
                    CHECK ("StandardWorkdayCount" > 0),
                CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualWorkdayCount"
                    CHECK ("ActualWorkdayCount" >= 0),
                CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualVsStandardWorkdayCount"
                    CHECK ("ActualWorkdayCount" <= "StandardWorkdayCount"),
                CONSTRAINT "CK_payroll_decuction_summary_insurance_details_AttendanceRate"
                    CHECK ("AttendanceRate" >= 0 AND "AttendanceRate" <= 1),
                CONSTRAINT "CK_payroll_decuction_summary_insurance_details_ActualAllowanceAmount"
                    CHECK ("ActualAllowanceAmount" >= 0)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_payroll_decuction_summary_insurance_details_PayrollDeductionSummaryRecordId"
            ON public.payroll_decuction_summary_insurance_details ("PayrollDeductionSummaryRecordId");

            CREATE INDEX IF NOT EXISTS "IX_payroll_decuction_summary_insurance_details_IsLocked"
            ON public.payroll_decuction_summary_insurance_details ("IsLocked");

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_records_PayrollYear'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_records
                    DROP CONSTRAINT "CK_payroll_decuction_summary_records_PayrollYear";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_records
                ADD CONSTRAINT "CK_payroll_decuction_summary_records_PayrollYear"
                CHECK ("PayrollYear" >= 1 AND "PayrollYear" <= 9999);

                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_payroll_decuction_summary_insurance_details_StandardWorkdayCount'
                ) THEN
                    ALTER TABLE public.payroll_decuction_summary_insurance_details
                    DROP CONSTRAINT "CK_payroll_decuction_summary_insurance_details_StandardWorkdayCount";
                END IF;

                ALTER TABLE public.payroll_decuction_summary_insurance_details
                ADD CONSTRAINT "CK_payroll_decuction_summary_insurance_details_StandardWorkdayCount"
                CHECK ("StandardWorkdayCount" > 0);

                IF to_regclass('public.payroll_decuction_summary_records') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_payroll_decuction_summary_insurance_details_PayrollDeductionSummaryRecordId'
                    )
                THEN
                    ALTER TABLE public.payroll_decuction_summary_insurance_details
                    ADD CONSTRAINT "FK_payroll_decuction_summary_insurance_details_PayrollDeductionSummaryRecordId"
                    FOREIGN KEY ("PayrollDeductionSummaryRecordId")
                    REFERENCES public.payroll_decuction_summary_records ("Id")
                    ON DELETE CASCADE;
                END IF;
            END $$;
            """,
            cancellationToken);
    }
    */
}
