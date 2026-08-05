using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.ChamCong.BangCongThang;

// Read service tối ưu cho grid tháng: phân trang nhân viên trước, sau đó mới tải day-cell thuộc page.
public sealed class DatabaseAttendanceMonthlyWorkSummaryGridReadService(
    ApplicationDbContext dbContext,
    IAttendanceMonthlyWorkReadAuthorizer attendanceMonthlyWorkReadAuthorizer)
    : IAttendanceMonthlyWorkSummaryGridReadService
{
    private const int DefaultTake = 50;
    private const int MaximumTake = 200;

    public async Task<AttendanceMonthlyWorkSummaryGridPageDto> SearchAsync(
        AttendanceMonthlyWorkSummaryGridFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await attendanceMonthlyWorkReadAuthorizer.DemandAsync(cancellationToken);

        // Backend luôn normalize để consumer khác UI không thể gửi skip, take hoặc khoảng ngày bất thường.
        var normalizedSearchTerm = NormalizeOptional(filter.SearchText);
        var (fromDate, toDate) = NormalizeDateRange(filter.FromDate, filter.ToDate);
        var skip = Math.Max(0, filter.Skip);
        var take = NormalizeTake(filter.Take);

        var employeePageQuery = BuildEmployeePageQuery(
            fromDate,
            toDate,
            normalizedSearchTerm,
            filter.EmployeeId);
        var totalCount = await employeePageQuery.CountAsync(cancellationToken);
        if(totalCount == 0 || skip >= totalCount)
        {
            return new AttendanceMonthlyWorkSummaryGridPageDto([], totalCount);
        }

        // Thứ tự có EmployeeId làm tie-breaker để một request reload không làm record nhảy giữa các trang.
        var employeePage = await employeePageQuery
            .OrderBy(item => item.DepartmentSort)
            .ThenBy(item => item.LastNameSort)
            .ThenBy(item => item.FirstNameSort)
            .ThenBy(item => item.EmployeeCodeSort)
            .ThenBy(item => item.EmployeeId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var pageEmployeeIds = employeePage
            .Select(item => item.EmployeeId)
            .Distinct()
            .ToArray();

        // Không join day-cell trước khi paging: một nhân viên nhiều ngày công sẽ làm sai kích thước page và total.
        var pageSummaries = await BuildSummaryQuery(
                fromDate,
                toDate,
                pageEmployeeIds,
                filter.IncludeShiftDetails)
            .ToListAsync(cancellationToken);

        // Dữ liệu legacy có thể trùng ngày; chọn snapshot mới nhất để UI luôn nhận duy nhất một ô/ngày/nhân viên.
        var dayCellsByEmployee = pageSummaries
            .GroupBy(item => item.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    return (IReadOnlyList<AttendanceMonthlyWorkSummaryDayCellDto>)group
                        .GroupBy(item => item.WorkDate)
                        .Select(dateGroup => dateGroup
                            .OrderByDescending(item => item.UpdatedAtUtc ?? item.ComputedAtUtc)
                            .ThenByDescending(item => item.CreatedAtUtc)
                            .ThenByDescending(item => item.Id)
                            .First())
                        .OrderBy(item => item.WorkDate)
                        .Select(MapDayCell)
                        .ToArray();
                });

        var rows = employeePage
            .Select((employee, index) => new AttendanceMonthlyWorkSummaryGridRowDto(
                employee.EmployeeId,
                skip + index + 1,
                NormalizeOptional(employee.EmployeeCode),
                BuildEmployeeName(employee.LastName, employee.FirstName),
                BuildDepartmentName(
                    employee.DepartmentOrWorkshopName,
                    employee.TeamName,
                    employee.GroupName,
                    employee.CenterName),
                NormalizeOptional(employee.PositionName),
                dayCellsByEmployee.GetValueOrDefault(employee.EmployeeId) ?? []))
            .ToArray();

        return new AttendanceMonthlyWorkSummaryGridPageDto(rows, totalCount);
    }

    private IQueryable<EmployeeMonthlyGridProjection> BuildEmployeePageQuery(
        DateOnly fromDate,
        DateOnly toDate,
        string? normalizedSearchTerm,
        Guid? requestedEmployeeId)
    {
        // Chỉ nhân viên đã có summary trong kỳ mới thuộc bảng công tháng.
        var employeeIdsInMonth = dbContext.AttendanceWorkdaySummaries
            .AsNoTracking()
            .Where(summary => summary.WorkDate >= fromDate && summary.WorkDate <= toDate)
            .Select(summary => summary.EmployeeId)
            .Distinct();

        var query =
            from employeeId in employeeIdsInMonth
            join employee in dbContext.Employees.AsNoTracking()
                on employeeId equals employee.Id
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new EmployeeMonthlyGridProjection
            {
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DepartmentOrWorkshopName = department == null ? null : department.DepartmentOrWorkshopName,
                TeamName = department == null ? null : department.TeamName,
                GroupName = department == null ? null : department.GroupName,
                CenterName = department == null ? null : department.CenterName,
                PositionName = position == null ? null : position.Name,
                DepartmentSort = department == null
                    ? string.Empty
                    : department.DepartmentOrWorkshopName
                        ?? department.TeamName
                        ?? department.GroupName
                        ?? department.CenterName
                        ?? string.Empty,
                LastNameSort = employee.LastName ?? string.Empty,
                FirstNameSort = employee.FirstName ?? string.Empty,
                EmployeeCodeSort = employee.EmployeeCode ?? string.Empty
            };

        if(requestedEmployeeId.HasValue)
        {
            query = query.Where(item => item.EmployeeId == requestedEmployeeId.Value);
        }

        if(string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            return query;
        }

        var searchPattern = $"%{normalizedSearchTerm}%";
        return query.Where(item =>
            (item.EmployeeCode != null && EF.Functions.ILike(item.EmployeeCode, searchPattern))
            || (item.FirstName != null && EF.Functions.ILike(item.FirstName, searchPattern))
            || (item.LastName != null && EF.Functions.ILike(item.LastName, searchPattern))
            || (item.DepartmentOrWorkshopName != null && EF.Functions.ILike(item.DepartmentOrWorkshopName, searchPattern))
            || (item.TeamName != null && EF.Functions.ILike(item.TeamName, searchPattern))
            || (item.GroupName != null && EF.Functions.ILike(item.GroupName, searchPattern))
            || (item.CenterName != null && EF.Functions.ILike(item.CenterName, searchPattern))
            || (item.PositionName != null && EF.Functions.ILike(item.PositionName, searchPattern)));
    }

    private IQueryable<SummaryMonthlyGridProjection> BuildSummaryQuery(
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyCollection<Guid> employeeIds,
        bool includeShiftDetails) =>
        includeShiftDetails
            ? BuildSummaryQueryWithShiftDetails(fromDate, toDate, employeeIds)
            : BuildSummaryQueryWithoutShiftDetails(fromDate, toDate, employeeIds);

    private IQueryable<SummaryMonthlyGridProjection> BuildSummaryQueryWithShiftDetails(
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyCollection<Guid> employeeIds)
    {
        // Popup chi tiết còn hiển thị ca nên chỉ nhánh này mới join bảng Shifts.
        return
            from summary in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
            join shift in dbContext.Shifts.AsNoTracking()
                on summary.ShiftId equals shift.Id into shiftGroup
            from shift in shiftGroup.DefaultIfEmpty()
            join statusCode in dbContext.AttendanceStatusCodes.AsNoTracking()
                on summary.CodeKetQuaTinhCongId equals statusCode.Id into statusCodeGroup
            from statusCode in statusCodeGroup.DefaultIfEmpty()
            where summary.WorkDate >= fromDate
                  && summary.WorkDate <= toDate
                  && employeeIds.Contains(summary.EmployeeId)
            select new SummaryMonthlyGridProjection
            {
                Id = summary.Id,
                EmployeeId = summary.EmployeeId,
                WorkDate = summary.WorkDate,
                DayType = summary.DayType,
                ShiftCode = shift == null ? null : shift.Code,
                ShiftShortName = shift == null ? null : shift.ShortName,
                ShiftName = shift == null ? null : shift.Name,
                ShiftColorHex = shift == null ? null : shift.ColorHex,
                CheckInAt = summary.CheckInAt,
                CheckOutAt = summary.CheckOutAt,
                LateMinutes = summary.LateMinutes,
                EarlyLeaveMinutes = summary.EarlyLeaveMinutes,
                Status = statusCode == null ? string.Empty : statusCode.Code,
                IsLocked = summary.IsLocked,
                OvertimeMinutes = summary.OvertimeMinutes,
                OvertimeMinutes15 = summary.OvertimeMinutes15,
                OvertimeMinutes20 = summary.OvertimeMinutes20,
                OvertimeMinutes30 = summary.OvertimeMinutes30,
                ComputedAtUtc = summary.ComputedAtUtc,
                CreatedAtUtc = summary.CreatedAtUtc,
                UpdatedAtUtc = summary.UpdatedAtUtc
            };
    }

    private IQueryable<SummaryMonthlyGridProjection> BuildSummaryQueryWithoutShiftDetails(
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyCollection<Guid> employeeIds)
    {
        // Grid Bảng công tháng không hiển thị dữ liệu ca; không join Shifts để giảm chi phí query day-cell của page.
        return
            from summary in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
            join statusCode in dbContext.AttendanceStatusCodes.AsNoTracking()
                on summary.CodeKetQuaTinhCongId equals statusCode.Id into statusCodeGroup
            from statusCode in statusCodeGroup.DefaultIfEmpty()
            where summary.WorkDate >= fromDate
                  && summary.WorkDate <= toDate
                  && employeeIds.Contains(summary.EmployeeId)
            select new SummaryMonthlyGridProjection
            {
                Id = summary.Id,
                EmployeeId = summary.EmployeeId,
                WorkDate = summary.WorkDate,
                DayType = summary.DayType,
                CheckInAt = summary.CheckInAt,
                CheckOutAt = summary.CheckOutAt,
                LateMinutes = summary.LateMinutes,
                EarlyLeaveMinutes = summary.EarlyLeaveMinutes,
                Status = statusCode == null ? string.Empty : statusCode.Code,
                IsLocked = summary.IsLocked,
                OvertimeMinutes = summary.OvertimeMinutes,
                OvertimeMinutes15 = summary.OvertimeMinutes15,
                OvertimeMinutes20 = summary.OvertimeMinutes20,
                OvertimeMinutes30 = summary.OvertimeMinutes30,
                ComputedAtUtc = summary.ComputedAtUtc,
                CreatedAtUtc = summary.CreatedAtUtc,
                UpdatedAtUtc = summary.UpdatedAtUtc
            };
    }

    private static AttendanceMonthlyWorkSummaryDayCellDto MapDayCell(
        SummaryMonthlyGridProjection summary) =>
        new(
            summary.Id,
            summary.WorkDate,
            summary.DayType,
            summary.ShiftCode,
            summary.ShiftShortName,
            summary.ShiftName,
            summary.ShiftColorHex,
            summary.CheckInAt,
            summary.CheckOutAt,
            summary.LateMinutes,
            summary.EarlyLeaveMinutes,
            summary.Status,
            summary.IsLocked,
            summary.OvertimeMinutes,
            summary.OvertimeMinutes15,
            summary.OvertimeMinutes20,
            summary.OvertimeMinutes30,
            summary.ComputedAtUtc,
            summary.CreatedAtUtc,
            summary.UpdatedAtUtc);

    // Đảo range thay vì trả lỗi để read screen vẫn an toàn khi consumer truyền hai đầu mốc ngược nhau.
    private static (DateOnly FromDate, DateOnly ToDate) NormalizeDateRange(DateOnly fromDate, DateOnly toDate) =>
        toDate < fromDate
            ? (toDate, fromDate)
            : (fromDate, toDate);

    // MaximumTake bảo vệ read path khỏi request lấy toàn bộ bảng công của tháng.
    private static int NormalizeTake(int take) =>
        take <= 0
            ? DefaultTake
            : Math.Min(take, MaximumTake);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildEmployeeName(string? lastName, string? firstName)
    {
        var parts = new[] { lastName, firstName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part!.Trim());

        return string.Join(" ", parts);
    }

    private static string BuildDepartmentName(
        string? departmentOrWorkshopName,
        string? teamName,
        string? groupName,
        string? centerName) =>
        // Dùng node thấp nhất của cơ cấu phòng ban để tránh lặp cả tuyến phân cấp trong ô Nhân viên.
        NormalizeOptional(groupName)
        ?? NormalizeOptional(teamName)
        ?? NormalizeOptional(departmentOrWorkshopName)
        ?? NormalizeOptional(centerName)
        ?? string.Empty;

    private sealed class EmployeeMonthlyGridProjection
    {
        public Guid EmployeeId { get; init; }

        public string? EmployeeCode { get; init; }

        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public string? DepartmentOrWorkshopName { get; init; }

        public string? TeamName { get; init; }

        public string? GroupName { get; init; }

        public string? CenterName { get; init; }

        public string? PositionName { get; init; }

        public string DepartmentSort { get; init; } = string.Empty;

        public string LastNameSort { get; init; } = string.Empty;

        public string FirstNameSort { get; init; } = string.Empty;

        public string EmployeeCodeSort { get; init; } = string.Empty;
    }

    private sealed class SummaryMonthlyGridProjection
    {
        public Guid Id { get; init; }

        public Guid EmployeeId { get; init; }

        public DateOnly WorkDate { get; init; }

        public string DayType { get; init; } = string.Empty;

        public string? ShiftCode { get; init; }

        public string? ShiftShortName { get; init; }

        public string? ShiftName { get; init; }

        public string? ShiftColorHex { get; init; }

        public string? CheckInAt { get; init; }

        public string? CheckOutAt { get; init; }

        public int LateMinutes { get; init; }

        public int EarlyLeaveMinutes { get; init; }

        public string Status { get; init; } = string.Empty;

        public bool IsLocked { get; init; }

        public int OvertimeMinutes { get; init; }

        public int OvertimeMinutes15 { get; init; }

        public int OvertimeMinutes20 { get; init; }

        public int OvertimeMinutes30 { get; init; }

        public DateTime ComputedAtUtc { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime? UpdatedAtUtc { get; init; }
    }
}
