using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.CaKip.BangXepCa;

public sealed class DatabaseAttendanceShiftAssignmentReadService(ApplicationDbContext dbContext)
    : IAttendanceShiftAssignmentReadService
{
    private const int ResignedEmployeeStatus = 5;
    private const int MaximumTake = 10000;
    private bool hasEnsuredShiftAssignmentsTable;

    public async Task<IReadOnlyList<AttendanceShiftAssignmentListItemDto>> SearchAsync(
        AttendanceShiftAssignmentFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureShiftAssignmentsTableAsync(cancellationToken);

        var normalizedTake = Math.Clamp(filter.Take, 1, MaximumTake);
        var (fromDate, toDate) = NormalizeDateRange(filter.FromDate, filter.ToDate);
        var normalizedSearchText = Normalize(filter.SearchText);
        var normalizedCreationType = Normalize(filter.CreationType);

        var query =
            from assignment in dbContext.ShiftAssignments.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on assignment.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join shift in dbContext.Shifts.AsNoTracking()
                on assignment.ShiftId equals shift.Id into shiftGroup
            from shift in shiftGroup.DefaultIfEmpty()
            select new { assignment, employee, department, shift };

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.assignment.WorkDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.assignment.WorkDate <= toDate.Value);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(x => x.assignment.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.employee != null && x.employee.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.ShiftId.HasValue)
        {
            query = query.Where(x => x.assignment.ShiftId == filter.ShiftId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedCreationType))
        {
            query = query.Where(x => x.assignment.CreationType.ToLower() == normalizedCreationType!.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearchText))
        {
            var searchPattern = $"%{normalizedSearchText}%";
            query = query.Where(x =>
                (x.employee != null && x.employee.EmployeeCode != null && EF.Functions.ILike(x.employee.EmployeeCode, searchPattern))
                || (x.employee != null && x.employee.FirstName != null && EF.Functions.ILike(x.employee.FirstName, searchPattern))
                || (x.employee != null && x.employee.LastName != null && EF.Functions.ILike(x.employee.LastName, searchPattern))
                || (x.shift != null && x.shift.Code != null && EF.Functions.ILike(x.shift.Code, searchPattern))
                || (x.shift != null && x.shift.Name != null && EF.Functions.ILike(x.shift.Name, searchPattern))
                || (x.shift != null && x.shift.ShortName != null && EF.Functions.ILike(x.shift.ShortName, searchPattern)));
        }

        return await query
            .OrderBy(x => x.department == null ? string.Empty : x.department.CenterName ?? string.Empty)
            .ThenBy(x => x.department == null ? string.Empty : x.department.DepartmentOrWorkshopName ?? string.Empty)
            .ThenBy(x => x.department == null ? string.Empty : x.department.TeamName ?? string.Empty)
            .ThenBy(x => x.department == null ? string.Empty : x.department.GroupName ?? string.Empty)
            .ThenBy(x => x.employee == null ? string.Empty : x.employee.EmployeeCode)
            .ThenBy(x => x.assignment.WorkDate)
            .ThenBy(x => x.shift == null ? string.Empty : x.shift.Code)
            .Take(normalizedTake)
            .Select(x => new AttendanceShiftAssignmentListItemDto(
                x.assignment.Id,
                x.assignment.EmployeeId,
                x.employee == null ? null : x.employee.EmployeeCode,
                x.employee == null ? null : BuildEmployeeName(x.employee),
                x.employee == null ? Guid.Empty : x.employee.DepartmentId,
                x.department == null ? null : x.department.Code,
                x.department == null ? null : BuildDepartmentName(x.department),
                x.department == null ? null : BuildDepartmentPath(x.department),
                x.assignment.ShiftId,
                x.shift == null ? null : x.shift.Code,
                x.shift == null ? null : x.shift.Name,
                x.shift == null ? null : x.shift.ShortName,
                x.shift == null ? null : x.shift.ColorHex,
                x.shift != null && x.shift.IsOvernight,
                x.assignment.WorkDate,
                x.assignment.CreationType,
                x.assignment.CreatedAtUtc,
                x.assignment.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<AttendanceShiftRosterSnapshotDto> GetRosterAsync(
        AttendanceShiftRosterFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureShiftAssignmentsTableAsync(cancellationToken);

        var normalizedFromDate = filter.FromDate;
        var normalizedToDate = filter.ToDate;
        if (normalizedToDate < normalizedFromDate)
        {
            (normalizedFromDate, normalizedToDate) = (normalizedToDate, normalizedFromDate);
        }

        var columns = BuildColumns(normalizedFromDate, normalizedToDate);
        var normalizedSearchText = Normalize(filter.SearchText);

        var query =
            from assignment in dbContext.ShiftAssignments.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on assignment.EmployeeId equals employee.Id
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join shift in dbContext.Shifts.AsNoTracking()
                on assignment.ShiftId equals shift.Id
            where assignment.WorkDate >= normalizedFromDate
                && assignment.WorkDate <= normalizedToDate
            select new { assignment, employee, department, shift };

        if (!filter.IncludeInactiveEmployees)
        {
            query = query.Where(x => !x.employee.IsDeleted && x.employee.Status != ResignedEmployeeStatus);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(x => x.assignment.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.employee.DepartmentId == filter.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearchText))
        {
            var searchPattern = $"%{normalizedSearchText}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.employee.EmployeeCode, searchPattern)
                || EF.Functions.ILike(x.employee.FirstName, searchPattern)
                || EF.Functions.ILike(x.employee.LastName, searchPattern)
                || EF.Functions.ILike(x.shift.Code, searchPattern)
                || EF.Functions.ILike(x.shift.Name, searchPattern)
                || (x.shift.ShortName != null && EF.Functions.ILike(x.shift.ShortName, searchPattern))
                || (x.department != null && x.department.CenterName != null && EF.Functions.ILike(x.department.CenterName, searchPattern))
                || (x.department != null && x.department.DepartmentOrWorkshopName != null && EF.Functions.ILike(x.department.DepartmentOrWorkshopName, searchPattern))
                || (x.department != null && x.department.TeamName != null && EF.Functions.ILike(x.department.TeamName, searchPattern))
                || (x.department != null && x.department.GroupName != null && EF.Functions.ILike(x.department.GroupName, searchPattern)));
        }

        var assignmentRows = await query
            .OrderBy(x => x.department == null ? string.Empty : x.department.CenterName ?? string.Empty)
            .ThenBy(x => x.department == null ? string.Empty : x.department.DepartmentOrWorkshopName ?? string.Empty)
            .ThenBy(x => x.department == null ? string.Empty : x.department.TeamName ?? string.Empty)
            .ThenBy(x => x.department == null ? string.Empty : x.department.GroupName ?? string.Empty)
            .ThenBy(x => x.employee.EmployeeCode)
            .ThenBy(x => x.assignment.WorkDate)
            .ThenBy(x => x.shift.Code)
            .Select(x => new
            {
                ShiftAssignmentId = x.assignment.Id,
                x.assignment.EmployeeId,
                x.employee.EmployeeCode,
                EmployeeLastName = x.employee.LastName,
                EmployeeFirstName = x.employee.FirstName,
                x.employee.DepartmentId,
                DepartmentCenterName = x.department == null ? null : x.department.CenterName,
                DepartmentOrWorkshopName = x.department == null ? null : x.department.DepartmentOrWorkshopName,
                DepartmentTeamName = x.department == null ? null : x.department.TeamName,
                DepartmentGroupName = x.department == null ? null : x.department.GroupName,
                x.assignment.ShiftId,
                ShiftCode = x.shift.Code,
                ShiftName = x.shift.Name,
                ShiftShortName = x.shift.ShortName,
                ShiftColorHex = x.shift.ColorHex,
                x.assignment.WorkDate,
                x.assignment.CreationType
            })
            .ToListAsync(cancellationToken);

        var rows = assignmentRows
            .Select(row => new ShiftRosterAssignmentProjection(
                row.ShiftAssignmentId,
                row.EmployeeId,
                row.EmployeeCode,
                BuildEmployeeName(row.EmployeeLastName, row.EmployeeFirstName),
                row.DepartmentId,
                BuildDepartmentName(
                    row.DepartmentGroupName,
                    row.DepartmentTeamName,
                    row.DepartmentOrWorkshopName,
                    row.DepartmentCenterName),
                BuildDepartmentPath(
                    row.DepartmentCenterName,
                    row.DepartmentOrWorkshopName,
                    row.DepartmentTeamName,
                    row.DepartmentGroupName),
                row.ShiftId,
                row.ShiftCode,
                row.ShiftName,
                row.ShiftShortName,
                row.ShiftColorHex,
                row.WorkDate,
                row.CreationType))
            .ToArray();

        var groupedRows = rows
            .GroupBy(
                row => new
                {
                    row.EmployeeId,
                    row.EmployeeCode,
                    row.EmployeeName,
                    row.DepartmentId,
                    row.DepartmentName,
                    row.DepartmentPath
                })
            .OrderBy(group => group.Key.DepartmentPath ?? string.Empty)
            .ThenBy(group => group.Key.EmployeeCode ?? string.Empty)
            .Select(group =>
            {
                var assignmentsByDate = group
                    .GroupBy(item => item.WorkDate)
                    .ToDictionary(item => item.Key, item => item.ToArray());

                var cells = columns
                    .Select(column =>
                    {
                        if (!assignmentsByDate.TryGetValue(column.WorkDate, out var assignmentsForDate))
                        {
                            return new AttendanceShiftRosterCellDto(
                                column.WorkDate,
                                null,
                                null,
                                null,
                                null,
                                null,
                                null,
                                null,
                                column.IsSunday,
                                false);
                        }

                        var orderedAssignments = assignmentsForDate
                            .OrderBy(item => item.ShiftCode ?? string.Empty)
                            .ThenBy(item => item.ShiftName ?? string.Empty)
                            .ToArray();

                        var firstAssignment = orderedAssignments[0];
                        var hasConflict = orderedAssignments.Length > 1;

                        return new AttendanceShiftRosterCellDto(
                            column.WorkDate,
                            hasConflict ? null : firstAssignment.ShiftAssignmentId,
                            hasConflict ? null : firstAssignment.ShiftId,
                            firstAssignment.ShiftCode,
                            firstAssignment.ShiftName,
                            ResolveShiftDisplayText(orderedAssignments),
                            firstAssignment.ShiftColorHex,
                            hasConflict
                                ? "Conflict"
                                : firstAssignment.CreationType,
                            column.IsSunday,
                            hasConflict);
                    })
                    .ToArray();

                return new AttendanceShiftRosterRowDto(
                    group.Key.EmployeeId,
                    group.Key.EmployeeCode,
                    group.Key.EmployeeName,
                    BuildEmployeeDisplay(group.Key.EmployeeCode, group.Key.EmployeeName),
                    group.Key.DepartmentId,
                    group.Key.DepartmentName,
                    group.Key.DepartmentPath,
                    cells);
            })
            .ToArray();

        return new AttendanceShiftRosterSnapshotDto(columns, groupedRows, DateTime.UtcNow);
    }

    private async Task<IReadOnlyList<AttendanceShiftAssignmentListItemDto>> FilterInactiveEmployeesAsync(
        IReadOnlyList<AttendanceShiftAssignmentListItemDto> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return rows;
        }

        var employeeIds = rows
            .Select(row => row.EmployeeId)
            .Distinct()
            .ToArray();

        var activeEmployeeIds = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employeeIds.Contains(employee.Id)
                && !employee.IsDeleted
                && employee.Status != ResignedEmployeeStatus)
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);

        var activeEmployeeIdSet = activeEmployeeIds.ToHashSet();
        return rows
            .Where(row => activeEmployeeIdSet.Contains(row.EmployeeId))
            .ToArray();
    }

    private static string ResolveShiftDisplayText(IReadOnlyList<AttendanceShiftAssignmentListItemDto> assignments)
    {
        if (assignments.Count == 0)
        {
            return string.Empty;
        }

        if (assignments.Count == 1)
        {
            return ResolveSingleShiftDisplayText(assignments[0]);
        }

        return string.Join(
            " / ",
            assignments
                .Select(ResolveSingleShiftDisplayText)
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string ResolveSingleShiftDisplayText(AttendanceShiftAssignmentListItemDto assignment) =>
        Normalize(assignment.ShiftShortName)
        ?? Normalize(assignment.ShiftCode)
        ?? Normalize(assignment.ShiftName)
        ?? "--";

    private static string ResolveShiftDisplayText(IReadOnlyList<ShiftRosterAssignmentProjection> assignments)
    {
        if (assignments.Count == 0)
        {
            return string.Empty;
        }

        if (assignments.Count == 1)
        {
            return ResolveSingleShiftDisplayText(assignments[0]);
        }

        return string.Join(
            " / ",
            assignments
                .Select(ResolveSingleShiftDisplayText)
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string ResolveSingleShiftDisplayText(ShiftRosterAssignmentProjection assignment) =>
        Normalize(assignment.ShiftShortName)
        ?? Normalize(assignment.ShiftCode)
        ?? Normalize(assignment.ShiftName)
        ?? "--";

    private static string BuildEmployeeDisplay(string? employeeCode, string? employeeName)
    {
        var parts = new[] { employeeCode, employeeName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part!.Trim())
            .ToArray();

        return parts.Length == 0 ? "--" : string.Join(" - ", parts);
    }

    private static IReadOnlyList<AttendanceShiftRosterColumnDto> BuildColumns(DateOnly fromDate, DateOnly toDate)
    {
        var columns = new List<AttendanceShiftRosterColumnDto>();
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            columns.Add(new AttendanceShiftRosterColumnDto(
                date,
                date.ToString("dd-MM", CultureInfo.GetCultureInfo("vi-VN")),
                GetWeekdayText(date.DayOfWeek),
                date.DayOfWeek == DayOfWeek.Sunday));
        }

        return columns;
    }

    private static (DateOnly? FromDate, DateOnly? ToDate) NormalizeDateRange(
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
        {
            return (toDate.Value, fromDate.Value);
        }

        return (fromDate, toDate);
    }

    private static string BuildEmployeeName(AttendanceGatewayEmployeeRow employee) =>
        BuildEmployeeName(employee.LastName, employee.FirstName);

    private static string BuildEmployeeName(string? lastName, string? firstName)
    {
        var parts = new[] { lastName, firstName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part!.Trim());

        return string.Join(" ", parts);
    }

    private static string BuildDepartmentName(AttendanceDepartmentRow department) =>
        BuildDepartmentName(
            department.GroupName,
            department.TeamName,
            department.DepartmentOrWorkshopName,
            department.CenterName);

    private static string BuildDepartmentName(
        string? groupName,
        string? teamName,
        string? departmentOrWorkshopName,
        string? centerName) =>
        Normalize(groupName)
        ?? Normalize(teamName)
        ?? Normalize(departmentOrWorkshopName)
        ?? Normalize(centerName)
        ?? string.Empty;

    private static string BuildDepartmentPath(AttendanceDepartmentRow department) =>
        BuildDepartmentPath(
            department.CenterName,
            department.DepartmentOrWorkshopName,
            department.TeamName,
            department.GroupName);

    private static string BuildDepartmentPath(
        string? centerName,
        string? departmentOrWorkshopName,
        string? teamName,
        string? groupName) =>
        string.Join(
            " / ",
            new[]
            {
                Normalize(centerName),
                Normalize(departmentOrWorkshopName),
                Normalize(teamName),
                Normalize(groupName)
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

    private static string GetWeekdayText(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "T2",
        DayOfWeek.Tuesday => "T3",
        DayOfWeek.Wednesday => "T4",
        DayOfWeek.Thursday => "T5",
        DayOfWeek.Friday => "T6",
        DayOfWeek.Saturday => "T7",
        _ => "CN"
    };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task EnsureShiftAssignmentsTableAsync(CancellationToken cancellationToken)
    {
        if (hasEnsuredShiftAssignmentsTable)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS public.shift_assignments (
                "Id" uuid NOT NULL,
                "EmployeeId" uuid NOT NULL,
                "ShiftId" uuid NOT NULL,
                "WorkDate" date NOT NULL,
                "CreationType" character varying(30) NOT NULL,
                "SourceBatchId" uuid NULL,
                "Notes" character varying(1000) NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_shift_assignments" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_shift_assignments_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_shift_assignments_ShiftId"
                    FOREIGN KEY ("ShiftId") REFERENCES public.shifts ("Id") ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_shift_assignments_EmployeeId_WorkDate"
                ON public.shift_assignments ("EmployeeId", "WorkDate");

            CREATE INDEX IF NOT EXISTS "IX_shift_assignments_WorkDate"
                ON public.shift_assignments ("WorkDate");

            CREATE INDEX IF NOT EXISTS "IX_shift_assignments_ShiftId_WorkDate"
                ON public.shift_assignments ("ShiftId", "WorkDate");

            CREATE INDEX IF NOT EXISTS "IX_shift_assignments_CreationType"
                ON public.shift_assignments ("CreationType");
            """,
            cancellationToken);

        hasEnsuredShiftAssignmentsTable = true;
    }

    private sealed record ShiftRosterAssignmentProjection(
        Guid ShiftAssignmentId,
        Guid EmployeeId,
        string? EmployeeCode,
        string? EmployeeName,
        Guid DepartmentId,
        string? DepartmentName,
        string? DepartmentPath,
        Guid ShiftId,
        string? ShiftCode,
        string? ShiftName,
        string? ShiftShortName,
        string? ShiftColorHex,
        DateOnly WorkDate,
        string CreationType);
}
