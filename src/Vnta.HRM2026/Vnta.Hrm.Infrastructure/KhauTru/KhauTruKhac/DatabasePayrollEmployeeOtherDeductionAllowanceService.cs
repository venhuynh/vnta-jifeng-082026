using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruKhac;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruKhac;

public sealed class DatabasePayrollEmployeeOtherDeductionAllowanceService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IPayrollEmployeeOtherDeductionAllowanceService
{
    private const int MaximumPageSize = 5000;

    public async Task PreparePeriodAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);
        var payrollYear = (short)year;
        var payrollMonth = (short)month;
        var usePostgresAdvisoryLock = string.Equals(
            dbContext.Database.ProviderName,
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            StringComparison.Ordinal);
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if(usePostgresAdvisoryLock)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({year}, {month});",
                cancellationToken);
        }

        var summaries = await dbContext.PayrollDeductionSummaryRecords
            .Where(row => row.PayrollYear == payrollYear && row.PayrollMonth == payrollMonth)
            .Select(row => new { row.Id, row.OtherDeductionAmount })
            .ToArrayAsync(cancellationToken);

        if(summaries.Length > 0)
        {
            var summaryIds = summaries.Select(row => row.Id).ToArray();
            var existingIds = await dbContext.PayrollDeductionOtherRecords
                .Where(row => summaryIds.Contains(row.PayrollDeductionSummaryRecordId))
                .Select(row => row.PayrollDeductionSummaryRecordId)
                .ToHashSetAsync(cancellationToken);
            var now = GetDatabaseNow();
            var newRows = summaries
                .Where(summary => !existingIds.Contains(summary.Id))
                .Select(summary => new PayrollDeductionOtherRecordRow
                {
                    PayrollDeductionSummaryRecordId = summary.Id,
                    DeductionAmount = summary.OtherDeductionAmount,
                    CreatedAtUtc = now
                })
                .ToArray();

            if(newRows.Length > 0)
            {
                dbContext.PayrollDeductionOtherRecords.AddRange(newRows);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if(transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<PayrollEmployeeOtherDeductionAllowanceListItemDto>> SearchAsync(
        PayrollEmployeeOtherDeductionAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = await SearchPageAsync(filter with { Take = MaximumPageSize, Skip = 0 }, cancellationToken);
        return page.Rows;
    }

    public async Task<PayrollEmployeeOtherDeductionAllowancePageDto> SearchPageAsync(
        PayrollEmployeeOtherDeductionAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(filter.PayrollYear, filter.PayrollMonth);
        var searchText = NormalizeOptional(filter.SearchText);
        var query =
            from detail in dbContext.PayrollDeductionOtherRecords.AsNoTracking()
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

        query = query.Where(item => item.summary.PayrollMonth == filter.PayrollMonth
                                    && item.summary.PayrollYear == filter.PayrollYear);

        if(filter.EmployeeId.HasValue)
        {
            query = query.Where(item => item.summary.EmployeeId == filter.EmployeeId.Value);
        }

        if(searchText is not null)
        {
            var searchPattern = $"%{searchText}%";
            query = query.Where(item =>
                (item.employee != null && item.employee.EmployeeCode != null && EF.Functions.ILike(item.employee.EmployeeCode, searchPattern))
                || (item.employee != null && item.employee.FirstName != null && EF.Functions.ILike(item.employee.FirstName, searchPattern))
                || (item.employee != null && item.employee.LastName != null && EF.Functions.ILike(item.employee.LastName, searchPattern))
                || (item.department != null && item.department.DepartmentOrWorkshopName != null && EF.Functions.ILike(item.department.DepartmentOrWorkshopName, searchPattern))
                || (item.position != null && item.position.Name != null && EF.Functions.ILike(item.position.Name, searchPattern))
                || (item.detail.Description != null && EF.Functions.ILike(item.detail.Description, searchPattern))
                || (item.detail.Note != null && EF.Functions.ILike(item.detail.Note, searchPattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(item => item.summary.PayrollYear)
            .ThenByDescending(item => item.summary.PayrollMonth)
            .ThenBy(item => item.employee == null ? string.Empty : item.employee.EmployeeCode)
            .ThenBy(item => item.summary.Id)
            .Skip(Math.Max(filter.Skip, 0))
            .Take(Math.Clamp(filter.Take, 1, MaximumPageSize))
            .Select(item => new PayrollEmployeeOtherDeductionAllowanceListItemDto(
                item.detail.PayrollDeductionSummaryRecordId,
                item.detail.PayrollDeductionSummaryRecordId,
                item.summary.EmployeeId,
                item.employee == null ? null : item.employee.EmployeeCode,
                item.employee == null ? null : BuildEmployeeName(item.employee.LastName, item.employee.FirstName),
                item.department == null ? null : FirstNotEmpty(item.department.GroupName, item.department.TeamName, item.department.DepartmentOrWorkshopName, item.department.CenterName),
                item.position == null ? null : item.position.Name,
                item.summary.PayrollMonth,
                item.summary.PayrollYear,
                item.detail.Description,
                item.detail.DeductionAmount,
                item.detail.Note,
                item.summary.IsLocked || item.detail.IsLocked,
                item.detail.CreatedAtUtc,
                item.detail.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PayrollEmployeeOtherDeductionAllowancePageDto(rows, totalCount);
    }

    public async Task<RefreshPayrollEmployeeOtherDeductionAllowanceResult> RefreshAsync(
        RefreshPayrollEmployeeOtherDeductionAllowanceRequest request,
        CancellationToken cancellationToken = default)
    {
        await PreparePeriodAsync(request.PayrollYear, request.PayrollMonth, cancellationToken);
        var filter = new PayrollEmployeeOtherDeductionAllowanceFilter(request.PayrollMonth, request.PayrollYear);
        var rows = await SearchAsync(filter, cancellationToken);
        if(request.PayrollDeductionSummaryRecordId.HasValue)
        {
            rows = rows.Where(row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId.Value).ToArray();
        }

        // Khấu trừ khác là dữ liệu thủ công; refresh chỉ bảo đảm snapshot tồn tại và không ghi đè giá trị người dùng.
        return new RefreshPayrollEmployeeOtherDeductionAllowanceResult(
            rows.Count,
            0,
            rows.Count(row => row.IsLocked));
    }

    public async Task<PayrollEmployeeOtherDeductionAllowanceListItemDto> UpdateManualValuesAsync(
        UpdatePayrollEmployeeOtherDeductionAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        if(request.PayrollDeductionSummaryRecordId == Guid.Empty)
        {
            throw new InvalidOperationException("Thiếu dòng tổng hợp khấu trừ để cập nhật thủ công.");
        }

        if(request.DeductionAmount < 0)
        {
            throw new InvalidOperationException("Số tiền khấu trừ không được nhỏ hơn 0.");
        }

        if(decimal.Round(request.DeductionAmount, 2, MidpointRounding.AwayFromZero) != request.DeductionAmount)
        {
            throw new InvalidOperationException("Số tiền khấu trừ chỉ được có tối đa 2 chữ số thập phân.");
        }

        if(!request.OriginalUpdatedAtUtc.HasValue)
        {
            throw new PayrollEmployeeOtherDeductionConflictException(
                "Thiếu mốc đối soát cập nhật. Vui lòng tải lại dữ liệu trước khi lưu.");
        }

        var detail = await dbContext.PayrollDeductionOtherRecords
            .SingleOrDefaultAsync(row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng khấu trừ khác cần cập nhật.");
        var summary = await dbContext.PayrollDeductionSummaryRecords
            .SingleOrDefaultAsync(row => row.Id == request.PayrollDeductionSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng tổng hợp khấu trừ liên quan.");
        ValidatePeriod(summary.PayrollYear, summary.PayrollMonth);
        if(detail.IsLocked || summary.IsLocked)
        {
            throw new PayrollEmployeeOtherDeductionConflictException(
                "Dòng khấu trừ khác đã khóa nên không thể chỉnh sửa.");
        }

        var currentVersion = detail.UpdatedAtUtc ?? detail.CreatedAtUtc;
        if(currentVersion != request.OriginalUpdatedAtUtc.Value)
        {
            throw new PayrollEmployeeOtherDeductionConflictException(
                "Dòng khấu trừ khác đã được cập nhật ở phiên khác. Vui lòng tải lại dữ liệu trước khi lưu tiếp.");
        }

        var command = auditScope.Current
            ?? throw new InvalidOperationException("Thiếu audit scope cho thao tác điều chỉnh khấu trừ khác.");
        var normalizedAmount = decimal.Round(request.DeductionAmount, 2, MidpointRounding.AwayFromZero);
        var normalizedNote = NormalizeOptional(request.Note);
        var now = GetDatabaseNow();

        await auditedMutation.ExecuteAsync(
            command with { ActionIntent = AuditActions.OtherDeduction.ManualValueUpdated },
            async token =>
            {
                var detailUpdatedCount = await dbContext.PayrollDeductionOtherRecords
                    .Where(row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId
                        && !row.IsLocked
                        && (row.UpdatedAtUtc ?? row.CreatedAtUtc) == request.OriginalUpdatedAtUtc.Value)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(row => row.DeductionAmount, normalizedAmount)
                            .SetProperty(row => row.Note, normalizedNote)
                            .SetProperty(row => row.UpdatedAtUtc, now),
                        token);
                if(detailUpdatedCount != 1)
                {
                    throw new PayrollEmployeeOtherDeductionConflictException(
                        "Dòng khấu trừ khác đã được thay đổi hoặc khóa bởi thao tác khác. Vui lòng tải lại dữ liệu.");
                }

                var summaryUpdatedCount = await dbContext.PayrollDeductionSummaryRecords
                    .Where(row => row.Id == request.PayrollDeductionSummaryRecordId && !row.IsLocked)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(row => row.OtherDeductionAmount, normalizedAmount)
                            .SetProperty(row => row.UpdatedAtUtc, now),
                        token);
                if(summaryUpdatedCount != 1)
                {
                    throw new PayrollEmployeeOtherDeductionConflictException(
                        "Dòng tổng hợp khấu trừ đã được khóa hoặc thay đổi. Vui lòng tải lại dữ liệu.");
                }

                return true;
            },
            _ => new AuditOperationEvent(
                AuditActions.OtherDeduction.ManualValueUpdated,
                AuditEntityTypes.OtherDeduction,
                request.PayrollDeductionSummaryRecordId.ToString("D"),
                Metadata: new Dictionary<string, string>
                {
                    ["concurrencyTokenProvided"] = bool.TrueString
                }),
            cancellationToken);

        dbContext.ChangeTracker.Clear();
        return await GetRequiredAsync(request.PayrollDeductionSummaryRecordId, cancellationToken);
    }

    public async Task<PayrollEmployeeOtherDeductionAllowanceListItemDto> SetLockStateAsync(
        SetPayrollEmployeeOtherDeductionAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var detail = await dbContext.PayrollDeductionOtherRecords
            .SingleOrDefaultAsync(row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng khấu trừ khác cần cập nhật trạng thái khóa.");
        var summaryLocked = await dbContext.PayrollDeductionSummaryRecords
            .Where(row => row.Id == request.PayrollDeductionSummaryRecordId)
            .Select(row => row.IsLocked)
            .SingleAsync(cancellationToken);
        if(summaryLocked)
        {
            throw new InvalidOperationException("Kỳ khấu trừ đã khóa nên không thể cập nhật trạng thái dòng.");
        }

        detail.IsLocked = request.IsLocked;
        detail.UpdatedAtUtc = GetDatabaseNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredAsync(request.PayrollDeductionSummaryRecordId, cancellationToken);
    }

    public async Task<SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.PayrollYear, request.PayrollMonth);
        await PreparePeriodAsync(request.PayrollYear, request.PayrollMonth, cancellationToken);
        var payrollYear = (short)request.PayrollYear;
        var payrollMonth = (short)request.PayrollMonth;
        var ids = request.PayrollDeductionSummaryRecordIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var query =
            from detail in dbContext.PayrollDeductionOtherRecords
            join summary in dbContext.PayrollDeductionSummaryRecords on detail.PayrollDeductionSummaryRecordId equals summary.Id
            where summary.PayrollYear == payrollYear && summary.PayrollMonth == payrollMonth
            select new { detail, summary };
        if(ids is { Length: > 0 })
        {
            query = query.Where(item => ids.Contains(item.detail.PayrollDeductionSummaryRecordId));
        }

        var rows = await query.ToListAsync(cancellationToken);
        var updatedCount = 0;
        foreach(var row in rows.Where(row => !row.summary.IsLocked && row.detail.IsLocked != request.IsLocked))
        {
            row.detail.IsLocked = request.IsLocked;
            row.detail.UpdatedAtUtc = GetDatabaseNow();
            updatedCount++;
        }

        if(updatedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateResult(rows.Count, updatedCount);
    }

    private async Task<PayrollEmployeeOtherDeductionAllowanceListItemDto> GetRequiredAsync(Guid summaryRecordId, CancellationToken cancellationToken)
    {
        var period = await dbContext.PayrollDeductionSummaryRecords
            .AsNoTracking()
            .Where(row => row.Id == summaryRecordId)
            .Select(row => new { row.PayrollMonth, row.PayrollYear })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng tổng hợp khấu trừ liên quan.");
        var result = await SearchAsync(
            new PayrollEmployeeOtherDeductionAllowanceFilter(period.PayrollMonth, period.PayrollYear),
            cancellationToken);
        return result.Single(row => row.PayrollDeductionSummaryRecordId == summaryRecordId);
    }

    private static void ValidatePeriod(int year, int month)
    {
        if(year is < 1900 or > 2100 || month is < 1 or > 12)
        {
            throw new InvalidOperationException("Kỳ lương không hợp lệ.");
        }
    }

    private static DateTime GetDatabaseNow() =>
        DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified);

    private static string? BuildEmployeeName(string? lastName, string? firstName) =>
        NormalizeOptional(string.Join(" ", new[] { lastName, firstName }.Where(value => !string.IsNullOrWhiteSpace(value))));

    private static string? FirstNotEmpty(params string?[] values) => values.Select(NormalizeOptional).FirstOrDefault(value => value is not null);

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
