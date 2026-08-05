using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;

public abstract partial class PayrollResponsibilityAllowancePersistenceOperations
{
    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentDto> SaveEmployeeAssignmentAsync(
        SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var row = await SaveEmployeeAssignmentCoreAsync(request, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildEmployeeAssignmentDtoAsync(row, cancellationToken);
    }

    public async Task<UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult> UpdateAndRefreshEmployeeAssignmentAsync(
        UpdatePayrollResponsibilityAllowanceEmployeeAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var summaryId = await dbContext.PayrollAllowanceSummaryRecords
            .Where(summary => summary.EmployeeId == request.EmployeeId
                && summary.PayrollYear == request.Year
                && summary.PayrollMonth == request.Month)
            .Select(summary => (Guid?)summary.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var existingAssignment = summaryId.HasValue
            ? await dbContext.PayrollResponsibilityAllowanceEmployeeAssignments
                .SingleOrDefaultAsync(item => item.PayrollAllowanceSummaryRecordId == summaryId.Value, cancellationToken)
            : null;
        EnsureEmployeeAssignmentConcurrency(existingAssignment, request.OriginalUpdatedAtUtc);
        if (existingAssignment is not null && existingAssignment.Id != request.Id)
        {
            throw new InvalidOperationException("Dòng gán cấp bậc không thuộc nhân viên hoặc kỳ đang cập nhật. Hãy tải lại dữ liệu.");
        }

        var assignment = await SaveEmployeeAssignmentCoreAsync(
            new SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest(
                request.Id,
                request.Year,
                request.Month,
                request.EmployeeId,
                request.GradeId,
                request.Note),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        var refresh = await RefreshCoreAsync(request.Year, request.Month, request.EmployeeId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult(
            await BuildEmployeeAssignmentDtoAsync(assignment, cancellationToken),
            refresh);
    }

    /// <summary>
    /// Bảo đảm mỗi dòng Summary của kỳ có đúng một assignment. Use case này chỉ tạo
    /// dòng còn thiếu để phục vụ màn hình Xem; không áp hoặc cập nhật bậc theo chức vụ.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> EnsureEmployeeAssignmentsForSummariesAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        var summaryIds = await dbContext.PayrollAllowanceSummaryRecords
            .Where(summary => summary.PayrollYear == year && summary.PayrollMonth == month)
            .Select(summary => summary.Id)
            .ToListAsync(cancellationToken);
        var existingSummaryIds = await dbContext.PayrollResponsibilityAllowanceEmployeeAssignments
            .Where(assignment => summaryIds.Contains(assignment.PayrollAllowanceSummaryRecordId))
            .Select(assignment => assignment.PayrollAllowanceSummaryRecordId)
            .ToHashSetAsync(cancellationToken);

        var now = GetDatabaseNow();
        var created = 0;
        foreach (var summaryId in summaryIds)
        {
            if (existingSummaryIds.Contains(summaryId))
            {
                continue;
            }

            dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.Add(
                new PayrollResponsibilityAllowanceEmployeeAssignmentRow
                {
                    Id = Guid.NewGuid(),
                    PayrollAllowanceSummaryRecordId = summaryId,
                    GradeId = null,
                    IsAssignGradeFromPosition = true,
                    CreatedAtUtc = now
                });
            created++;
        }

        if (created > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult(year, month, summaryIds.Count, created);
    }

    /// <summary>
    /// Đồng bộ assignment theo đúng tập nhân viên của Phụ cấp tổng hợp và kế thừa giá trị
    /// assignment của nhân viên đó từ kỳ liền trước nếu tồn tại.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> LoadEmployeeAssignmentsFromPreviousMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var currentPeriod = new ResponsibilityAllowancePeriod(year, month);
        var previousPeriod = currentPeriod.GetPreviousPeriod();
        var summaries = await dbContext.PayrollAllowanceSummaryRecords
            .Where(summary => summary.PayrollYear == year && summary.PayrollMonth == month)
            .Select(summary => new { summary.Id, summary.EmployeeId })
            .ToListAsync(cancellationToken);
        var summaryIds = summaries.Select(summary => summary.Id).ToArray();
        var assignmentsBySummaryId = await dbContext.PayrollResponsibilityAllowanceEmployeeAssignments
            .Where(assignment => summaryIds.Contains(assignment.PayrollAllowanceSummaryRecordId))
            .ToDictionaryAsync(assignment => assignment.PayrollAllowanceSummaryRecordId, cancellationToken);
        var previousAssignmentsByEmployeeId = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                where summary.PayrollYear == previousPeriod.Year && summary.PayrollMonth == previousPeriod.Month
                join assignment in dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.AsNoTracking()
                    on summary.Id equals assignment.PayrollAllowanceSummaryRecordId
                select new
                {
                    summary.EmployeeId,
                    assignment.GradeId,
                    assignment.IsAssignGradeFromPosition,
                    assignment.Note
                })
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);

        var now = GetDatabaseNow();
        var updated = 0;
        foreach (var summary in summaries)
        {
            if (!assignmentsBySummaryId.TryGetValue(summary.Id, out var assignment))
            {
                assignment = new PayrollResponsibilityAllowanceEmployeeAssignmentRow
                {
                    Id = Guid.NewGuid(),
                    PayrollAllowanceSummaryRecordId = summary.Id,
                    CreatedAtUtc = now
                };
                dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.Add(assignment);
                assignmentsBySummaryId.Add(summary.Id, assignment);
            }

            if (!previousAssignmentsByEmployeeId.TryGetValue(summary.EmployeeId, out var previousAssignment))
            {
                continue;
            }

            assignment.GradeId = previousAssignment.GradeId;
            assignment.IsAssignGradeFromPosition = previousAssignment.IsAssignGradeFromPosition;
            assignment.Note = previousAssignment.Note;
            assignment.UpdatedAtUtc = now;
            updated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult(year, month, summaries.Count, updated);
    }

    /// <summary>
    /// Tính lại assignment của kỳ: ưu tiên sao chép gán tay từ kỳ trước; các dòng
    /// còn gán theo chức vụ được tra lại từ rule của đúng kỳ đang chọn.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> RecalculateEmployeeAssignmentsAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var currentPeriod = new ResponsibilityAllowancePeriod(year, month);
        var previousPeriod = currentPeriod.GetPreviousPeriod();
        var summaries = await dbContext.PayrollAllowanceSummaryRecords
            .Where(summary => summary.PayrollYear == year && summary.PayrollMonth == month)
            .Select(summary => new { summary.Id, summary.EmployeeId })
            .ToListAsync(cancellationToken);
        var summaryIds = summaries.Select(summary => summary.Id).ToArray();
        var assignments = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords
                where summaryIds.Contains(summary.Id)
                join assignment in dbContext.PayrollResponsibilityAllowanceEmployeeAssignments
                    on summary.Id equals assignment.PayrollAllowanceSummaryRecordId
                join employee in dbContext.Employees.AsNoTracking()
                    on summary.EmployeeId equals employee.Id
                select new { summary.EmployeeId, assignment, employee.PositionId })
            .ToListAsync(cancellationToken);
        var previousManualGradesByEmployeeId = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                where summary.PayrollYear == previousPeriod.Year && summary.PayrollMonth == previousPeriod.Month
                join assignment in dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.AsNoTracking()
                    on summary.Id equals assignment.PayrollAllowanceSummaryRecordId
                where !assignment.IsAssignGradeFromPosition
                select new { summary.EmployeeId, assignment.GradeId })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.GradeId, cancellationToken);
        var gradesById = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .Where(grade => grade.Year == year && grade.Month == month && grade.IsActive)
            .ToDictionaryAsync(grade => grade.Id, cancellationToken);
        var mappingsByPositionId = await dbContext.PayrollResponsibilityAllowanceGradePositions
            .AsNoTracking()
            .Where(mapping => mapping.Year == year && mapping.Month == month && mapping.IsActive)
            .ToDictionaryAsync(mapping => mapping.PositionId, cancellationToken);

        var now = GetDatabaseNow();
        var updated = 0;
        foreach (var item in assignments)
        {
            if (previousManualGradesByEmployeeId.TryGetValue(item.EmployeeId, out var previousGradeId))
            {
                item.assignment.GradeId = previousGradeId;
                item.assignment.IsAssignGradeFromPosition = false;
                item.assignment.UpdatedAtUtc = now;
                updated++;
                continue;
            }

            if (!item.assignment.IsAssignGradeFromPosition)
            {
                continue;
            }

            item.assignment.GradeId = mappingsByPositionId.TryGetValue(item.PositionId, out var mapping)
                && gradesById.ContainsKey(mapping.GradeId)
                    ? mapping.GradeId
                    : null;
            item.assignment.UpdatedAtUtc = now;
            updated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult(year, month, summaries.Count, updated);
    }

    /// <summary>
    /// Đồng bộ một assignment cho mỗi Summary trong kỳ. Bậc null biểu diễn chưa gán;
    /// assignment nhận quy tắc chức vụ được đánh dấu rõ bằng cờ chuyên biệt.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> ApplyPositionDefaultsToEmployeeAssignmentsAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        var summaries = await dbContext.PayrollAllowanceSummaryRecords
            .Where(summary => summary.PayrollYear == year && summary.PayrollMonth == month)
            .Select(summary => new { summary.Id, summary.EmployeeId })
            .ToListAsync(cancellationToken);
        var employeeIds = summaries.Select(summary => summary.EmployeeId).ToArray();
        var employeesById = await LoadEmployeeSnapshotsByIdAsync(employeeIds, cancellationToken);
        var gradesById = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .Where(grade => grade.Year == year && grade.Month == month)
            .ToDictionaryAsync(grade => grade.Id, cancellationToken);
        var mappings = await dbContext.PayrollResponsibilityAllowanceGradePositions
            .AsNoTracking()
            .Where(mapping => mapping.Year == year && mapping.Month == month && mapping.IsActive)
            .ToDictionaryAsync(mapping => mapping.PositionId, cancellationToken);
        var summaryIds = summaries.Select(summary => summary.Id).ToArray();
        var assignmentsBySummaryId = await dbContext.PayrollResponsibilityAllowanceEmployeeAssignments
            .Where(assignment => summaryIds.Contains(assignment.PayrollAllowanceSummaryRecordId))
            .ToDictionaryAsync(assignment => assignment.PayrollAllowanceSummaryRecordId, cancellationToken);
        var lockedEmployeeIds = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .AsNoTracking()
            .Where(row => row.Year == year && row.Month == month && row.IsLocked && employeeIds.Contains(row.EmployeeId))
            .Select(row => row.EmployeeId)
            .ToHashSetAsync(cancellationToken);

        var now = GetDatabaseNow();
        var updated = 0;
        foreach (var summary in summaries)
        {
            if (!employeesById.TryGetValue(summary.EmployeeId, out var employee))
            {
                throw new InvalidOperationException("Không tìm thấy nhân viên của dữ liệu Phụ cấp tổng hợp trong kỳ đã chọn.");
            }

            if (lockedEmployeeIds.Contains(summary.EmployeeId))
            {
                continue;
            }

            assignmentsBySummaryId.TryGetValue(summary.Id, out var assignment);
            // Gán riêng thuộc quyết định người dùng; chỉ các dòng nhận quy tắc chức vụ mới bị đồng bộ lại.
            if (assignment is not null && !assignment.IsAssignGradeFromPosition)
            {
                continue;
            }

            var mappedGradeId = employee.PositionId.HasValue
                && mappings.TryGetValue(employee.PositionId.Value, out var mapping)
                && gradesById.TryGetValue(mapping.GradeId, out var mappedGrade)
                && mappedGrade.IsActive
                    ? mappedGrade.Id
                    : (Guid?)null;

            if (assignment is null)
            {
                assignment = new PayrollResponsibilityAllowanceEmployeeAssignmentRow
                {
                    Id = Guid.NewGuid(),
                    PayrollAllowanceSummaryRecordId = summary.Id,
                    CreatedAtUtc = now
                };
                dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.Add(assignment);
                assignmentsBySummaryId.Add(summary.Id, assignment);
            }

            assignment.GradeId = mappedGradeId;
            assignment.IsAssignGradeFromPosition = true;
            assignment.UpdatedAtUtc = now;
            updated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult(year, month, summaries.Count, updated);
    }

    private static void EnsureEmployeeAssignmentConcurrency(
        PayrollResponsibilityAllowanceEmployeeAssignmentRow? assignment,
        DateTime? originalUpdatedAtUtc)
    {
        if (assignment is null)
        {
            if (originalUpdatedAtUtc.HasValue)
            {
                throw new ResponsibilityAllowanceConflictException(
                    "Dữ liệu gán cấp bậc nhân viên đã thay đổi. Vui lòng tải lại trước khi lưu.");
            }

            return;
        }

        var currentTimestamp = assignment.UpdatedAtUtc ?? assignment.CreatedAtUtc;
        if (!originalUpdatedAtUtc.HasValue || currentTimestamp != originalUpdatedAtUtc.Value)
        {
            throw new ResponsibilityAllowanceConflictException(
                "Dữ liệu gán cấp bậc nhân viên đã thay đổi. Vui lòng tải lại trước khi lưu.");
        }
    }
}
