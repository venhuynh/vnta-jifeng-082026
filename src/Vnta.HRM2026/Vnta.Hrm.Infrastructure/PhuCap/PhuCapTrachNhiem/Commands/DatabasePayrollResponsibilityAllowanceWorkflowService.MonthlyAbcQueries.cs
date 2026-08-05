using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;

public abstract partial class PayrollResponsibilityAllowancePersistenceOperations
{
    #region Truy vấn ABC theo kỳ lương

    public async Task<PayrollResponsibilityAllowanceAbcPageDto> SearchAbcAsync(
        PayrollResponsibilityAllowanceAbcQuery request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        var skip = Math.Max(request.Skip, 0);
        var take = Math.Clamp(request.Take, 1, 200);
        var periodQuery = dbContext.PayrollResponsibilityAllowanceAbcRows
            .AsNoTracking()
            .Where(x => x.Year == request.Year && x.Month == request.Month);

        var summary = await periodQuery
            .GroupBy(_ => 1)
            .Select(group => new PayrollResponsibilityAllowanceAbcSummaryDto(
                group.Count(),
                group.Count(x => x.GradeId.HasValue && x.StandardResponsibilityAllowanceAmount > 0),
                group.Count(x => x.AbcRating == "A"),
                group.Count(x => x.AbcRating == "B"),
                group.Count(x => x.AbcRating == "C"),
                group.Count(x => x.AbcRating == "D"),
                group.Count(x => !x.IsLocked),
                group.Count(x => x.IsLocked)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new PayrollResponsibilityAllowanceAbcSummaryDto(0, 0, 0, 0, 0, 0, 0, 0);

        var filteredQuery = ApplyAbcListFilter(periodQuery, request.SearchText, request.SummaryFilterKey);
        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var rows = await filteredQuery
            .OrderBy(x => x.EmployeeCode)
            .ThenBy(x => x.EmployeeName)
            .Skip(skip)
            .Take(take)
            .Select(MapAbcDtoExpression())
            .ToListAsync(cancellationToken);

        return new PayrollResponsibilityAllowanceAbcPageDto(rows, totalCount, summary);
    }

    public async Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>> ExportAsync(
        PayrollResponsibilityAllowanceAbcExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);
        if (!string.Equals(request.Format, "xlsx", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Định dạng xuất phụ cấp trách nhiệm không hợp lệ.");
        }

        return await dbContext.PayrollResponsibilityAllowanceAbcRows
            .AsNoTracking()
            .Where(x => x.Year == request.Year && x.Month == request.Month)
            .OrderBy(x => x.EmployeeCode)
            .ThenBy(x => x.EmployeeName)
            .Select(x => new PayrollResponsibilityAllowanceAbcExportItemDto(
                x.EmployeeCode,
                x.EmployeeName,
                x.DepartmentName,
                x.PositionName,
                x.GradeCode,
                x.GradeName,
                x.ActualWorkDays,
                x.StandardWorkDays,
                x.AbcRating,
                x.MonthlyPerformanceBonusAmount,
                x.IsPerformanceBonusExcluded,
                x.StandardResponsibilityAllowanceAmount,
                x.ActualResponsibilityAllowanceAmount,
                x.IsLocked,
                x.CalculatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Đọc các snapshot ABC của kỳ lương và chiếu trực tiếp sang DTO no-tracking.
    /// Truy vấn này chỉ phục vụ hiển thị, không làm phát sinh hoặc cập nhật dòng.
    /// </summary>
    public async Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto>> GetAbcAsync(
        PayrollResponsibilityAllowanceAbcFilter filter,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(filter.Year, filter.Month);

        // Query no-tracking chỉ giữ điều kiện kỳ; bộ lọc trạng thái khóa là tùy chọn.
        var query = dbContext.PayrollResponsibilityAllowanceAbcRows
            .AsNoTracking()
            .Where(x => x.Year == filter.Year && x.Month == filter.Month);

        if (filter.IsLocked.HasValue)
        {
            query = query.Where(x => x.IsLocked == filter.IsLocked.Value);
        }

        return await query
            .OrderBy(x => x.EmployeeCode)
            .ThenBy(x => x.EmployeeName)
            .Select(MapAbcDtoExpression())
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<PayrollResponsibilityAllowanceAbcRow> ApplyAbcListFilter(
        IQueryable<PayrollResponsibilityAllowanceAbcRow> query,
        string? searchText,
        string? summaryFilterKey)
    {
        var keyword = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower();
        if (keyword is not null)
        {
            query = query.Where(x =>
                x.EmployeeCode.ToLower().Contains(keyword)
                || x.EmployeeName.ToLower().Contains(keyword)
                || (x.DepartmentName != null && x.DepartmentName.ToLower().Contains(keyword))
                || x.PositionName.ToLower().Contains(keyword)
                || x.GradeName.ToLower().Contains(keyword));
        }

        return summaryFilterKey?.Trim().ToLowerInvariant() switch
        {
            "active" => query.Where(x => x.GradeId.HasValue && x.StandardResponsibilityAllowanceAmount > 0),
            "abc-a" => query.Where(x => x.AbcRating == "A"),
            "abc-b" => query.Where(x => x.AbcRating == "B"),
            "abc-c" => query.Where(x => x.AbcRating == "C"),
            "abc-d" => query.Where(x => x.AbcRating == "D"),
            "open" => query.Where(x => !x.IsLocked),
            "locked" => query.Where(x => x.IsLocked),
            _ => query
        };
    }

    /// <summary>
    /// Gom dữ liệu nguồn và kết quả xem trước cho popup điều chỉnh: nhân viên,
    /// assignment, mapping chức vụ, bậc, chấm công, công chuẩn, snapshot hiện tại
    /// và công thức tiền. Đây là truy vấn giải thích, không ghi dữ liệu.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceUpdateContextDto> GetUpdateContextAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        var employee = await ResolveEmployeeAsync(employeeId, cancellationToken);
        // Snapshot hiện tại có thể chưa tồn tại; popup vẫn trả ngữ cảnh để người dùng hiểu nguồn dự kiến.
        var currentAbc = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.EmployeeId == employeeId && x.Year == year && x.Month == month, cancellationToken);

        // Assignment riêng được đọc trước vì nó có độ ưu tiên cao hơn mapping theo chức vụ.
        var assignment = await (
                from row in dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.AsNoTracking()
                join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                    on row.PayrollAllowanceSummaryRecordId equals summary.Id
                where summary.EmployeeId == employeeId
                    && summary.PayrollYear == year
                    && summary.PayrollMonth == month
                join grade in dbContext.PayrollResponsibilityAllowanceGrades.AsNoTracking()
                    on row.GradeId equals grade.Id into gradeGroup
                from grade in gradeGroup.DefaultIfEmpty()
                select new PayrollResponsibilityAllowanceEmployeeAssignmentContextDto(
                    "payroll_monthly_responsibility_allowance_employee_assignments",
                    row.Id,
                    summary.PayrollYear,
                    summary.PayrollMonth,
                    summary.EmployeeId,
                    employee.EmployeeCode,
                    employee.EmployeeName,
                    employee.PositionId,
                    employee.PositionName,
                    row.GradeId,
                    grade == null ? null : grade.Code,
                    grade == null ? string.Empty : grade.Name,
                    grade == null ? 0m : grade.StandardResponsibilityAllowanceAmount,
                    row.IsAssignGradeFromPosition ? PositionDefaultSourceKey : EmployeeAssignmentSourceKey,
                    row.Note))
            .SingleOrDefaultAsync(cancellationToken);

        // Không có chức vụ thì không thể có nguồn mặc định theo chức vụ.
        var positionMapping = employee.PositionId.HasValue
            ? await (
                    from row in dbContext.PayrollResponsibilityAllowanceGradePositions.AsNoTracking()
                    where row.PositionId == employee.PositionId.Value && row.Year == year && row.Month == month && row.IsActive
                    join position in dbContext.Positions.AsNoTracking()
                        on row.PositionId equals position.Id
                    select new PayrollResponsibilityAllowancePositionGradeMappingContextDto(
                        "payroll_monthly_responsibility_allowance_grade_positions",
                        row.Id,
                        row.Year,
                        row.Month,
                        row.GradeId,
                        row.PositionId,
                        position.Code,
                        position.Name,
                        row.IsActive,
                        row.Note))
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        var manualGrade = assignment?.GradeId.HasValue == true
            ? await BuildContextGradeAsync(assignment.GradeId!.Value, cancellationToken)
            : null;

        var positionDefaultGrade = positionMapping is not null
            ? await BuildContextGradeAsync(positionMapping.GradeId, cancellationToken)
            : null;

        // Dùng đúng aggregate của command ABC để phần xem trước và phần lưu không lệch công thức.
        var workdaySummary = await LoadWorkdayAggregateAsync(year, month, [employeeId], cancellationToken);
        var validWorkday = workdaySummary.TryGetValue(employeeId, out var aggregate)
            ? aggregate.SalaryWorkdays
            : 0m;
        var statusCodes = workdaySummary.TryGetValue(employeeId, out aggregate)
            ? aggregate.StatusCodes
            : [];

        var standardWorkDaysByEmployee = await basicSalaryWorkdaySource.LoadStandardWorkingDaysAsync(
            year,
            month,
            [employeeId],
            cancellationToken);
        var standardWorkDays = standardWorkDaysByEmployee.GetValueOrDefault(employeeId);

        // Resolve trả cả nguồn được chọn lẫn lý do hiển thị cho người dùng trước khi họ lưu điều chỉnh.
        var selectedSource = ResolveSelectedSource(assignment, manualGrade, positionMapping, positionDefaultGrade);
        var previewAbcRating = currentAbc?.AbcRating ?? ComputeAbcRating(
            standardWorkDays,
            validWorkday,
            aggregate?.HasUnexcusedAbsence ?? false);
        var previewActualAmount = CalculateActualResponsibilityAllowanceAmount(
            selectedSource.StandardResponsibilityAllowanceAmount,
            standardWorkDays,
            validWorkday,
            previewAbcRating,
            currentAbc?.MonthlyPerformanceBonusAmount ?? 0m,
            currentAbc?.IsPerformanceBonusExcluded ?? false);

        // Danh sách chọn chỉ chứa bậc active để không cho tạo assignment mới vào bậc đã ngừng dùng.
        var availableGrades = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .Where(x => x.Year == year && x.Month == month && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Code)
            .Select(x => new PayrollResponsibilityAllowanceContextGradeDto(
                "payroll_monthly_responsibility_allowance_grades",
                x.Id,
                x.Year,
                x.Month,
                x.Code,
                x.Name,
                x.StandardResponsibilityAllowanceAmount,
                x.DisplayOrder,
                x.IsActive,
                x.Note))
            .ToListAsync(cancellationToken);

        return new PayrollResponsibilityAllowanceUpdateContextDto(
            year,
            month,
            new PayrollResponsibilityAllowanceEmployeeSnapshotContextDto(
                "employees",
                employee.Id,
                employee.EmployeeCode,
                employee.EmployeeName,
                employee.DepartmentName,
                employee.PositionId,
                employee.PositionName),
            currentAbc is null ? null : BuildCurrentAbcContext(currentAbc),
            assignment,
            positionMapping,
            manualGrade,
            positionDefaultGrade,
            new PayrollResponsibilityAllowanceValidWorkdayContextDto(
                "attendance_workday_summaries + attendance_status_codes",
                AttendanceWorkCalendarDayTypes.Regular,
                statusCodes,
                validWorkday),
            new PayrollResponsibilityAllowanceSalaryRateContextDto(
                "payroll_basic_salary_records",
                standardWorkDays > 0,
                standardWorkDays),
            selectedSource,
            new PayrollResponsibilityAllowanceCalculationPreviewDto(
                previewAbcRating,
                GetAbcMultiplier(previewAbcRating),
                currentAbc?.MonthlyPerformanceBonusAmount ?? 0m,
                currentAbc?.IsPerformanceBonusExcluded ?? false,
                currentAbc?.IsPerformanceBonusExcluded == true ? 1m : currentAbc?.MonthlyPerformanceBonusAmount ?? 0m,
                validWorkday,
                standardWorkDays,
                Math.Max(standardWorkDays - validWorkday, 0m),
                standardWorkDays <= 0 ? 0m : decimal.Round(validWorkday / standardWorkDays, 4, MidpointRounding.AwayFromZero),
                selectedSource.StandardResponsibilityAllowanceAmount,
                previewActualAmount,
                BuildCalculationFormula(
                    selectedSource.StandardResponsibilityAllowanceAmount,
                    standardWorkDays,
                    validWorkday,
                    previewAbcRating,
                    currentAbc?.MonthlyPerformanceBonusAmount ?? 0m,
                    currentAbc?.IsPerformanceBonusExcluded ?? false)),
            BuildUpdateImpact(currentAbc, selectedSource.StandardResponsibilityAllowanceAmount, previewActualAmount),
            availableGrades);
    }

    #endregion
}
