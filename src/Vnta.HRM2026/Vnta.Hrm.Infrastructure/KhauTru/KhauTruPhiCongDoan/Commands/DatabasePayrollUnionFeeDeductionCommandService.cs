using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruPhiCongDoan;

public sealed class DatabasePayrollUnionFeeDeductionCommandService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IPayrollUnionFeeDeductionPeriodPreparationService,
      IPayrollUnionFeeDeductionRefreshService,
      IPayrollUnionFeeDeductionManualAdjustmentService,
      IPayrollUnionFeeDeductionLockService
{
    private const string SystemActor = "system";
    private const int MinimumSupportedMonth = 6;
    private const int MinimumSupportedYear = 2026;
    private const int MaximumSupportedYear = 2100;

    public Task<PreparePayrollUnionFeeDeductionPeriodResult> PreparePeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);
        return auditedMutation.ExecuteAsync(
            CreateAuditCommand(AuditActions.UnionFeeDeduction.PeriodPrepared),
            token => PreparePeriodCoreAsync(year, month, token),
            result => new AuditOperationEvent(
                AuditActions.UnionFeeDeduction.PeriodPrepared,
                "PayrollUnionFeeDeductionPeriod",
                $"{result.PayrollYear:D4}-{result.PayrollMonth:D2}",
                Outcome: result.CreatedCount == 0 ? AuditOperationOutcome.NoChanges : AuditOperationOutcome.Succeeded,
                Metadata: new Dictionary<string, string>
                {
                    ["summaryCount"] = result.SummaryCount.ToString(),
                    ["createdCount"] = result.CreatedCount.ToString(),
                    ["existingCount"] = result.ExistingCount.ToString(),
                    ["lockedSummaryCount"] = result.LockedSummaryCount.ToString()
                }),
            cancellationToken);
    }

    public Task<RefreshPayrollUnionFeeDeductionResult> RefreshAsync(
        RefreshPayrollUnionFeeDeductionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.PayrollYear, request.PayrollMonth);
        return auditedMutation.ExecuteAsync(
            CreateAuditCommand(AuditActions.UnionFeeDeduction.Refreshed),
            token => RefreshCoreAsync(request, token),
            result => new AuditOperationEvent(
                AuditActions.UnionFeeDeduction.Refreshed,
                "PayrollUnionFeeDeductionPeriod",
                $"{result.PayrollYear:D4}-{result.PayrollMonth:D2}",
                Outcome: result.UpdatedCount == 0 ? AuditOperationOutcome.NoChanges : AuditOperationOutcome.Succeeded,
                Metadata: new Dictionary<string, string>
                {
                    ["targetRowCount"] = result.TargetRowCount.ToString(),
                    ["updatedCount"] = result.UpdatedCount.ToString(),
                    ["skippedLockedCount"] = result.SkippedLockedCount.ToString()
                }),
            cancellationToken);
    }

    public Task<PayrollUnionFeeDeductionListItemDto> UpdateManualValueAsync(
        UpdatePayrollUnionFeeDeductionManualValueRequest request,
        CancellationToken cancellationToken = default) =>
        auditedMutation.ExecuteAsync(
            CreateAuditCommand(AuditActions.UnionFeeDeduction.ManualValueUpdated),
            token => UpdateManualValueCoreAsync(request, token),
            CreateManualValueUpdatedAuditEvent,
            cancellationToken);

    public Task<PayrollUnionFeeDeductionListItemDto> SetLockStateAsync(
        SetPayrollUnionFeeDeductionLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        auditedMutation.ExecuteAsync(
            CreateAuditCommand(AuditActions.UnionFeeDeduction.SetLockState),
            token => SetLockStateCoreAsync(request, token),
            CreateAuditEvent,
            cancellationToken);

    public Task<SetPayrollUnionFeeDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollUnionFeeDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.PayrollYear, request.PayrollMonth);
        return auditedMutation.ExecuteAsync(
            CreateAuditCommand(AuditActions.UnionFeeDeduction.SetLockStateBatch),
            token => SetLockStateBatchCoreAsync(request, token),
            result => new AuditOperationEvent(
                AuditActions.UnionFeeDeduction.SetLockStateBatch,
                "PayrollUnionFeeDeductionPeriod",
                $"{request.PayrollYear:D4}-{request.PayrollMonth:D2}",
                Outcome: result.UpdatedCount == 0 ? AuditOperationOutcome.NoChanges : AuditOperationOutcome.Succeeded,
                Metadata: new Dictionary<string, string>
                {
                    ["targetRowCount"] = result.TargetRowCount.ToString(),
                    ["updatedCount"] = result.UpdatedCount.ToString(),
                    ["isLocked"] = request.IsLocked.ToString()
                }),
            cancellationToken);
    }

    private async Task<PreparePayrollUnionFeeDeductionPeriodResult> PreparePeriodCoreAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var payrollYear = (short)year;
        var payrollMonth = (short)month;
        var now = GetDatabaseNow();

        if(dbContext.Database.IsNpgsql())
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({year}, {month});",
                cancellationToken);
        }

        var summaries = await dbContext.PayrollDeductionSummaryRecords
            .AsNoTracking()
            .Where(summary => summary.PayrollYear == payrollYear && summary.PayrollMonth == payrollMonth)
            .OrderBy(summary => summary.Id)
            .Select(summary => new
            {
                summary.Id,
                summary.UnionFeeDeductionAmount,
                summary.IsLocked
            })
            .ToListAsync(cancellationToken);

        var summaryIds = summaries.Select(summary => summary.Id).ToArray();
        var existingIds = summaryIds.Length == 0
            ? new HashSet<Guid>()
            : await dbContext.PayrollDeductionUnionFeeRecords
                .AsNoTracking()
                .Where(detail => summaryIds.Contains(detail.PayrollDeductionSummaryRecordId))
                .Select(detail => detail.PayrollDeductionSummaryRecordId)
                .ToHashSetAsync(cancellationToken);

        foreach(var summary in summaries.Where(summary => !existingIds.Contains(summary.Id)))
        {
            dbContext.PayrollDeductionUnionFeeRecords.Add(new PayrollDeductionUnionFeeRecordRow
            {
                PayrollDeductionSummaryRecordId = summary.Id,
                DeductionAmount = summary.UnionFeeDeductionAmount,
                IsLocked = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = null
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PreparePayrollUnionFeeDeductionPeriodResult(
            year,
            month,
            summaries.Count,
            summaries.Count - existingIds.Count,
            existingIds.Count,
            summaries.Count(summary => summary.IsLocked));
    }

    private async Task<RefreshPayrollUnionFeeDeductionResult> RefreshCoreAsync(
        RefreshPayrollUnionFeeDeductionRequest request,
        CancellationToken cancellationToken)
    {
        await PreparePeriodCoreAsync(request.PayrollYear, request.PayrollMonth, cancellationToken);

        var payrollYear = (short)request.PayrollYear;
        var payrollMonth = (short)request.PayrollMonth;
        var query =
            from summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
            join detail in dbContext.PayrollDeductionUnionFeeRecords.AsNoTracking()
                on summary.Id equals detail.PayrollDeductionSummaryRecordId into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            where summary.PayrollYear == payrollYear && summary.PayrollMonth == payrollMonth
            select new
            {
                summary.Id,
                summary.UnionFeeDeductionAmount,
                summary.IsLocked,
                Detail = detail
            };

        if(request.PayrollDeductionSummaryRecordId.HasValue)
        {
            query = query.Where(row => row.Id == request.PayrollDeductionSummaryRecordId.Value);
        }

        var rows = await query.ToListAsync(cancellationToken);
        if(request.PayrollDeductionSummaryRecordId.HasValue && rows.Count == 0)
        {
            throw new InvalidOperationException("Khong tim thay dong cong doan can lam lai.");
        }

        var updatedCount = 0;
        var skippedLockedCount = 0;
        var now = GetDatabaseNow();

        foreach(var row in rows)
        {
            if(row.IsLocked || row.Detail?.IsLocked == true)
            {
                skippedLockedCount++;
                continue;
            }

            if(row.Detail is null)
            {
                dbContext.PayrollDeductionUnionFeeRecords.Add(new PayrollDeductionUnionFeeRecordRow
                {
                    PayrollDeductionSummaryRecordId = row.Id,
                    DeductionAmount = row.UnionFeeDeductionAmount,
                    IsLocked = false,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = null
                });
                updatedCount++;
                continue;
            }

            if(row.Detail.DeductionAmount != row.UnionFeeDeductionAmount)
            {
                row.Detail.DeductionAmount = row.UnionFeeDeductionAmount;
                row.Detail.UpdatedAtUtc = now;
                updatedCount++;
            }
        }

        if(updatedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new RefreshPayrollUnionFeeDeductionResult(
            request.PayrollYear,
            request.PayrollMonth,
            rows.Count,
            updatedCount,
            skippedLockedCount);
    }

    private async Task<PayrollUnionFeeDeductionListItemDto> SetLockStateCoreAsync(
        SetPayrollUnionFeeDeductionLockStateRequest request,
        CancellationToken cancellationToken)
    {
        if(request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Thieu dong phi cong doan de khoa hoac mo khoa.");
        }

        var current = await (
                from summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
                join detail in dbContext.PayrollDeductionUnionFeeRecords.AsNoTracking()
                    on summary.Id equals detail.PayrollDeductionSummaryRecordId into detailGroup
                from detail in detailGroup.DefaultIfEmpty()
                where summary.Id == request.Id
                select new
                {
                    summary.IsLocked,
                    SummaryAmount = summary.UnionFeeDeductionAmount,
                    SummaryMonth = summary.PayrollMonth,
                    SummaryYear = summary.PayrollYear,
                    SummaryEmployeeId = summary.EmployeeId,
                    Detail = detail
                })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Khong tim thay dong phi cong doan de khoa hoac mo khoa.");

        if(current.IsLocked)
        {
            throw new InvalidOperationException("Ky luong khau tru da khoa, khong the thay doi trang thai phi cong doan.");
        }

        if(current.Detail is null)
        {
            var now = GetDatabaseNow();
            dbContext.PayrollDeductionUnionFeeRecords.Add(new PayrollDeductionUnionFeeRecordRow
            {
                PayrollDeductionSummaryRecordId = request.Id,
                DeductionAmount = current.SummaryAmount,
                IsLocked = request.IsLocked,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if(current.Detail.IsLocked != request.IsLocked
            && current.Detail.UpdatedAtUtc != request.OriginalUpdatedAtUtc)
        {
            throw new InvalidOperationException("Dong phi cong doan da duoc cap nhat boi phien khac. Hay tai lai du lieu truoc khi thao tac.");
        }
        else if(current.Detail.IsLocked != request.IsLocked)
        {
            var now = GetDatabaseNow();
            var updated = await dbContext.PayrollDeductionUnionFeeRecords
                .Where(detail => detail.PayrollDeductionSummaryRecordId == request.Id
                    && detail.IsLocked != request.IsLocked
                    && detail.UpdatedAtUtc == request.OriginalUpdatedAtUtc)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(detail => detail.IsLocked, request.IsLocked)
                    .SetProperty(detail => detail.UpdatedAtUtc, now), cancellationToken);

            if(updated != 1)
            {
                throw new InvalidOperationException("Dong phi cong doan da thay doi. Hay tai lai du lieu truoc khi thao tac.");
            }
        }

        return await GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Khong the tai lai dong phi cong doan sau khi thay doi trang thai.");
    }

    private async Task<PayrollUnionFeeDeductionListItemDto> UpdateManualValueCoreAsync(
        UpdatePayrollUnionFeeDeductionManualValueRequest request,
        CancellationToken cancellationToken)
    {
        PayrollUnionFeeDeductionManualValuePolicy.EnsureValid(request.DeductionAmount);

        if(request.PayrollDeductionSummaryRecordId == Guid.Empty)
        {
            throw new InvalidOperationException("Thiếu dòng tổng hợp khấu trừ để điều chỉnh phí công đoàn.");
        }

        if(request.DeductionAmount < 0m || request.DeductionAmount > 9_999_999_999_999_999.99m)
        {
            throw new InvalidOperationException("Số tiền phí công đoàn phải nằm trong phạm vi cho phép.");
        }

        if(decimal.Round(request.DeductionAmount, 2, MidpointRounding.AwayFromZero) != request.DeductionAmount)
        {
            throw new InvalidOperationException("Số tiền phí công đoàn chỉ được có tối đa 2 chữ số thập phân.");
        }

        var current = await (
                from detail in dbContext.PayrollDeductionUnionFeeRecords.AsNoTracking()
                join summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
                    on detail.PayrollDeductionSummaryRecordId equals summary.Id
                where detail.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId
                select new
                {
                    DetailIsLocked = detail.IsLocked,
                    SummaryIsLocked = summary.IsLocked,
                    VersionAtUtc = detail.UpdatedAtUtc ?? detail.CreatedAtUtc
                })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng phí công đoàn cần điều chỉnh.");

        if(current.DetailIsLocked || current.SummaryIsLocked)
        {
            throw new PayrollUnionFeeDeductionConflictException(
                "Dòng phí công đoàn hoặc kỳ tổng hợp đã khóa nên không thể điều chỉnh.");
        }

        if(current.VersionAtUtc != request.OriginalVersionAtUtc)
        {
            throw new PayrollUnionFeeDeductionConflictException(
                "Dòng phí công đoàn đã được cập nhật ở phiên khác. Vui lòng tải lại dữ liệu trước khi lưu tiếp.");
        }

        var amount = PayrollUnionFeeDeductionManualValuePolicy.Normalize(request.DeductionAmount);
        var now = GetDatabaseNow();
        var detailUpdatedCount = await dbContext.PayrollDeductionUnionFeeRecords
            .Where(detail => detail.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId
                && !detail.IsLocked
                && (detail.UpdatedAtUtc ?? detail.CreatedAtUtc) == request.OriginalVersionAtUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(detail => detail.DeductionAmount, amount)
                .SetProperty(detail => detail.UpdatedAtUtc, now), cancellationToken);
        if(detailUpdatedCount != 1)
        {
            throw new PayrollUnionFeeDeductionConflictException(
                "Dòng phí công đoàn đã thay đổi hoặc bị khóa bởi thao tác khác. Vui lòng tải lại dữ liệu.");
        }

        var summaryUpdatedCount = await dbContext.PayrollDeductionSummaryRecords
            .Where(summary => summary.Id == request.PayrollDeductionSummaryRecordId && !summary.IsLocked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(summary => summary.UnionFeeDeductionAmount, amount)
                .SetProperty(summary => summary.UpdatedAtUtc, now), cancellationToken);
        if(summaryUpdatedCount != 1)
        {
            throw new PayrollUnionFeeDeductionConflictException(
                "Dòng tổng hợp khấu trừ đã bị khóa hoặc thay đổi. Vui lòng tải lại dữ liệu.");
        }

        dbContext.ChangeTracker.Clear();
        return await GetByIdAsync(request.PayrollDeductionSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại dòng phí công đoàn sau khi điều chỉnh.");
    }

    private async Task<SetPayrollUnionFeeDeductionBatchLockStateResult> SetLockStateBatchCoreAsync(
        SetPayrollUnionFeeDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken)
    {
        await PreparePeriodCoreAsync(request.PayrollYear, request.PayrollMonth, cancellationToken);

        var payrollYear = (short)request.PayrollYear;
        var payrollMonth = (short)request.PayrollMonth;
        var ids = request.PayrollDeductionSummaryRecordIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var query =
            from detail in dbContext.PayrollDeductionUnionFeeRecords
            join summary in dbContext.PayrollDeductionSummaryRecords
                on detail.PayrollDeductionSummaryRecordId equals summary.Id
            where summary.PayrollYear == payrollYear && summary.PayrollMonth == payrollMonth
            select new { detail, summary };

        if(ids is { Length: > 0 })
        {
            query = query.Where(row => ids.Contains(row.summary.Id));
        }

        var rows = await query.ToListAsync(cancellationToken);
        var updatedRows = rows
            .Where(row => !row.summary.IsLocked && row.detail.IsLocked != request.IsLocked)
            .ToArray();
        if(updatedRows.Length > 0)
        {
            var now = GetDatabaseNow();
            foreach(var row in updatedRows)
            {
                row.detail.IsLocked = request.IsLocked;
                row.detail.UpdatedAtUtc = now;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SetPayrollUnionFeeDeductionBatchLockStateResult(rows.Count, updatedRows.Length);
    }

    private async Task<PayrollUnionFeeDeductionListItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await (
            from summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
            join detail in dbContext.PayrollDeductionUnionFeeRecords.AsNoTracking()
                on summary.Id equals detail.PayrollDeductionSummaryRecordId into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            where summary.Id == id
            select new
            {
                summary.Id,
                summary.EmployeeId,
                EmployeeCode = employee == null ? null : employee.EmployeeCode,
                EmployeeLastName = employee == null ? null : employee.LastName,
                EmployeeFirstName = employee == null ? null : employee.FirstName,
                DepartmentName = department == null ? null : department.DepartmentOrWorkshopName,
                DepartmentTeamName = department == null ? null : department.TeamName,
                DepartmentGroupName = department == null ? null : department.GroupName,
                DepartmentCenterName = department == null ? null : department.CenterName,
                PositionName = position == null ? null : position.Name,
                summary.PayrollMonth,
                summary.PayrollYear,
                DeductionAmount = summary.UnionFeeDeductionAmount,
                summary.IsLocked,
                DetailIsLocked = detail != null && detail.IsLocked,
                CreatedAtUtc = detail == null ? summary.CreatedAtUtc : detail.CreatedAtUtc,
                UpdatedAtUtc = detail == null ? null : detail.UpdatedAtUtc
            }).SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new PayrollUnionFeeDeductionListItemDto(
                row.Id,
                row.EmployeeId,
                row.EmployeeCode,
                BuildEmployeeName(row.EmployeeLastName, row.EmployeeFirstName),
                FirstNotEmpty(row.DepartmentGroupName, row.DepartmentTeamName, row.DepartmentName, row.DepartmentCenterName),
                row.PositionName,
                row.PayrollMonth,
                row.PayrollYear,
                row.DeductionAmount,
                row.IsLocked,
                row.DetailIsLocked,
                row.CreatedAtUtc,
                row.UpdatedAtUtc);
    }

    private AuditCommand CreateAuditCommand(string action)
    {
        var current = auditScope.Current;
        return new AuditCommand(
            current?.OperationId ?? Guid.NewGuid(),
            action,
            current?.Actor ?? new AuditActor(SystemActor, SystemActor, AuditActorKind.System, AuditSource.Worker),
            current?.CorrelationId ?? Guid.NewGuid().ToString("N"),
            AuditCaptureMode.OperationOnly,
            Metadata: current?.Metadata);
    }

    private static void ValidatePeriod(int year, int month)
    {
        if(year < MinimumSupportedYear || year > MaximumSupportedYear)
        {
            throw new InvalidOperationException(
                $"Nam ky luong phai nam trong khoang tu {MinimumSupportedYear} den {MaximumSupportedYear}.");
        }

        if(month is < 1 or > 12)
        {
            throw new InvalidOperationException("Thang ky luong phai nam trong khoang tu 1 den 12.");
        }

        if(year == MinimumSupportedYear && month < MinimumSupportedMonth)
        {
            throw new InvalidOperationException($"Ky luong phai tu {MinimumSupportedMonth:00}/{MinimumSupportedYear} tro di.");
        }
    }

    private static AuditOperationEvent CreateAuditEvent(PayrollUnionFeeDeductionListItemDto row) =>
        new(
            AuditActions.UnionFeeDeduction.SetLockState,
            "PayrollUnionFeeDeduction",
            row.PayrollDeductionSummaryRecordId.ToString("N"),
            Outcome: AuditOperationOutcome.Succeeded,
            Metadata: new Dictionary<string, string>
            {
                ["payrollYear"] = row.PayrollYear.ToString(),
                ["payrollMonth"] = row.PayrollMonth.ToString(),
                ["isLocked"] = row.IsLocked.ToString()
            });

    private static AuditOperationEvent CreateManualValueUpdatedAuditEvent(PayrollUnionFeeDeductionListItemDto row) =>
        new(
            AuditActions.UnionFeeDeduction.ManualValueUpdated,
            "PayrollUnionFeeDeduction",
            row.PayrollDeductionSummaryRecordId.ToString("N"),
            Metadata: new Dictionary<string, string>
            {
                ["payrollYear"] = row.PayrollYear.ToString(),
                ["payrollMonth"] = row.PayrollMonth.ToString(),
                ["concurrencyTokenProvided"] = bool.TrueString
            });

    private static string? BuildEmployeeName(string? lastName, string? firstName) =>
        string.Join(" ", new[] { lastName, firstName }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim())) is { Length: > 0 } value
            ? value
            : null;

    private static string? FirstNotEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static DateTime GetDatabaseNow() =>
        DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified);
}
