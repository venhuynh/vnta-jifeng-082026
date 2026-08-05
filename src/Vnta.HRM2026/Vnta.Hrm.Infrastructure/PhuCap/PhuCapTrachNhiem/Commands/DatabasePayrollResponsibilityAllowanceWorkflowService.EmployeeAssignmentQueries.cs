using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;

public abstract partial class PayrollResponsibilityAllowancePersistenceOperations
{
    /// <summary>
    /// Đọc danh sách gán cấp bậc theo đúng tập Summary của kỳ. Search, lọc trạng thái gán
    /// và phân trang thực hiện tại server để không tải toàn bộ danh sách về Blazor circuit.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto> SearchEmployeeAssignmentsAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentQuery request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 200);
        var periodQuery = BuildEmployeeAssignmentReadRowsQuery(request.Year, request.Month);
        var gradeIdQuery = BuildEmployeeAssignmentGradeIdQuery(request.Year, request.Month);
        var totalAssignmentCount = await gradeIdQuery.CountAsync(cancellationToken);
        var assignedGradeCount = await gradeIdQuery.CountAsync(gradeId => gradeId != null, cancellationToken);
        var summary = new PayrollResponsibilityAllowanceEmployeeAssignmentSummaryDto(
            totalAssignmentCount,
            assignedGradeCount,
            totalAssignmentCount - assignedGradeCount);

        var filtered = ApplyEmployeeAssignmentFilter(periodQuery, request.SearchText, request.GradePresenceKey);
        var totalCount = await filtered.CountAsync(cancellationToken);
        var rows = (await filtered
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken))
            .Select(MapEmployeeAssignmentDto)
            .ToArray();
        var activeGrades = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .Where(grade => grade.Year == request.Year && grade.Month == request.Month && grade.IsActive)
            .OrderBy(grade => grade.DisplayOrder)
            .ThenBy(grade => grade.Code)
            .Select(grade => new PayrollResponsibilityAllowanceGradeDto(
                grade.Id, grade.Year, grade.Month, grade.Code, grade.Name,
                grade.StandardResponsibilityAllowanceAmount, grade.DisplayOrder, grade.IsActive, grade.Note))
            .ToListAsync(cancellationToken);

        return new PayrollResponsibilityAllowanceEmployeeAssignmentPageDto(rows, totalCount, summary, activeGrades);
    }

    /// <summary>Xuất toàn bộ tập assignment của kỳ với field allowlist độc lập grid hiện hành.</summary>
    public async Task<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>> ExportEmployeeAssignmentsAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);
        if (!string.Equals(request.Format, "xlsx", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Định dạng xuất danh sách gán cấp bậc nhân viên không hợp lệ.");
        }

        return await BuildEmployeeAssignmentReadRowsQuery(request.Year, request.Month)
            .Select(item => new PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto(
                item.EmployeeCode,
                item.EmployeeName,
                item.PositionName,
                item.GradeCode,
                item.GradeName,
                item.StandardResponsibilityAllowanceAmount,
                item.AssignmentSource))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<EmployeeAssignmentReadRow> BuildEmployeeAssignmentReadRowsQuery(int year, int month) =>
        from summary in PayrollAllowanceSummaryPopulationQuery.ForPeriod(dbContext, year, month)
        join employee in dbContext.Employees.AsNoTracking()
            on summary.EmployeeId equals employee.Id into employeeGroup
        from employee in employeeGroup.DefaultIfEmpty()
        join assignment in dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.AsNoTracking()
            on summary.Id equals assignment.PayrollAllowanceSummaryRecordId into assignmentGroup
        from assignment in assignmentGroup.DefaultIfEmpty()
        join position in dbContext.Positions.AsNoTracking()
            on employee.PositionId equals position.Id into positionGroup
        from position in positionGroup.DefaultIfEmpty()
        join grade in dbContext.PayrollResponsibilityAllowanceGrades.AsNoTracking()
            on assignment.GradeId equals grade.Id into gradeGroup
        from grade in gradeGroup.DefaultIfEmpty()
        orderby employee == null ? string.Empty : employee.EmployeeCode,
                employee == null ? string.Empty : employee.LastName,
                employee == null ? string.Empty : employee.FirstName
        select new EmployeeAssignmentReadRow
        {
            Id = assignment == null ? summary.Id : assignment.Id,
            Year = year,
            Month = month,
            EmployeeId = summary.EmployeeId,
            EmployeeCode = employee == null ? string.Empty : employee.EmployeeCode,
            EmployeeName = employee == null ? string.Empty : (employee.LastName + " " + employee.FirstName).Trim(),
            PositionId = employee == null ? null : employee.PositionId,
            PositionName = position == null ? string.Empty : position.Name,
            GradeId = assignment == null ? null : assignment.GradeId,
            GradeCode = grade == null ? null : grade.Code,
            GradeName = grade == null ? string.Empty : grade.Name,
            StandardResponsibilityAllowanceAmount = grade == null ? 0m : grade.StandardResponsibilityAllowanceAmount,
            IsAssignGradeFromPosition = assignment != null && assignment.IsAssignGradeFromPosition,
            AssignmentSource = assignment == null
                ? string.Empty
                : assignment.IsAssignGradeFromPosition ? PositionDefaultSourceKey : EmployeeAssignmentSourceKey,
            Note = assignment == null ? null : assignment.Note,
            UpdatedAtUtc = assignment == null ? null : assignment.UpdatedAtUtc ?? assignment.CreatedAtUtc
        };

    /// <summary>
    /// Chỉ lấy khóa cấp bậc để tính thống kê. Không group trên DTO projection vì Npgsql/EF Core
    /// không thể dịch một số GroupBy trên shape có left join phức tạp thành SQL.
    /// </summary>
    private IQueryable<Guid?> BuildEmployeeAssignmentGradeIdQuery(int year, int month) =>
        from summary in PayrollAllowanceSummaryPopulationQuery.ForPeriod(dbContext, year, month)
        join assignment in dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.AsNoTracking()
            on summary.Id equals assignment.PayrollAllowanceSummaryRecordId into assignmentGroup
        from assignment in assignmentGroup.DefaultIfEmpty()
        select assignment == null ? (Guid?)null : assignment.GradeId;

    private static IQueryable<EmployeeAssignmentReadRow> ApplyEmployeeAssignmentFilter(
        IQueryable<EmployeeAssignmentReadRow> query,
        string? searchText,
        string? gradePresenceKey)
    {
        var keyword = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower();
        if (keyword is not null)
        {
            query = query.Where(item =>
                item.EmployeeCode.ToLower().Contains(keyword)
                || item.EmployeeName.ToLower().Contains(keyword)
                || item.PositionName.ToLower().Contains(keyword)
                || (item.GradeCode != null && item.GradeCode.ToLower().Contains(keyword))
                || item.GradeName.ToLower().Contains(keyword));
        }

        return gradePresenceKey?.Trim().ToLowerInvariant() switch
        {
            "assigned" => query.Where(item => item.GradeId != null),
            "unassigned" => query.Where(item => item.GradeId == null),
            _ => query
        };
    }

    private static PayrollResponsibilityAllowanceEmployeeAssignmentDto MapEmployeeAssignmentDto(EmployeeAssignmentReadRow item) =>
        new(
            item.Id,
            item.Year,
            item.Month,
            item.EmployeeId,
            item.EmployeeCode,
            item.EmployeeName,
            item.PositionId,
            item.PositionName,
            item.GradeId,
            item.GradeCode,
            item.GradeName,
            item.StandardResponsibilityAllowanceAmount,
            item.IsAssignGradeFromPosition,
            item.AssignmentSource,
            item.Note,
            item.UpdatedAtUtc);

    private sealed class EmployeeAssignmentReadRow
    {
        public Guid Id { get; init; }
        public int Year { get; init; }
        public int Month { get; init; }
        public Guid EmployeeId { get; init; }
        public string EmployeeCode { get; init; } = string.Empty;
        public string EmployeeName { get; init; } = string.Empty;
        public Guid? PositionId { get; init; }
        public string PositionName { get; init; } = string.Empty;
        public Guid? GradeId { get; init; }
        public string? GradeCode { get; init; }
        public string GradeName { get; init; } = string.Empty;
        public decimal StandardResponsibilityAllowanceAmount { get; init; }
        public bool IsAssignGradeFromPosition { get; init; }
        public string AssignmentSource { get; init; } = string.Empty;
        public string? Note { get; init; }
        public DateTime? UpdatedAtUtc { get; init; }
    }
}
