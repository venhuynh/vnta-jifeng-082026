using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed partial class DatabaseAttendanceWorkdaySummaryService(
    ApplicationDbContext dbContext,
    IOptions<AttendanceWorkdaySummaryOptions> optionsAccessor)
    : IAttendanceWorkdaySummaryService
{
    private const int ResignedEmployeeStatus = 5;
    private const int EmployeeClassificationType = 5;
    private const int DepartmentClassificationType = 2;
    private const int AbnormalThresholdMinutes = 240;

    private const string FullPunchStatusCode = "VR";
    private const string PartialPunchStatusCode = "TS";
    private const string UnauthorizedAbsenceStatusCode = "KP";
    private const string MissingLogStatusCode = "MISSING_LOG";
    private const string FullWorkStatusCode = "FULL_WORK";
    private const string LateEarlyStatusCode = "LATE_EARLY";
    private const string AbnormalStatusCode = "ABNORMAL";
    private const string UnauthorizedAbsenceNote = "Nghỉ không phép.";
    private const string PartialPunchNote = "Thiếu dữ liệu chấm công trong ngày.";
    private readonly AttendanceWorkdaySummaryOptions workdaySummaryOptions = optionsAccessor.Value ?? new();

    public async Task<RebuildAttendanceWorkdaySummaryResult> RebuildAsync(
        RebuildAttendanceWorkdaySummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        if(request.WorkDate == default)
        {
            throw new InvalidOperationException("Ngày công cần tính không hợp lệ.");
        }

        var dayType = await LoadWorkCalendarDayTypeAsync(request.WorkDate, cancellationToken);

        return dayType switch
        {
            AttendanceWorkCalendarDayType.Regular => await RebuildRegularDayAsync(
                request.WorkDate,
                cancellationToken),
            AttendanceWorkCalendarDayType.DayOff => await RebuildDayOffAsync(
                request.WorkDate,
                cancellationToken),
            AttendanceWorkCalendarDayType.Holiday => await RebuildHolidayAsync(
                request.WorkDate,
                cancellationToken),
            _ => throw new InvalidOperationException("Loại ngày công không được hỗ trợ.")
        };
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if(normalizedIds.Length == 0)
        {
            return;
        }

        var rows = await dbContext.AttendanceWorkdaySummaries
            .Where(summary => normalizedIds.Contains(summary.Id))
            .ToListAsync(cancellationToken);

        if(rows.Count == 0)
        {
            return;
        }

        if(rows.Any(row => row.IsLocked))
        {
            throw new InvalidOperationException("Không thể xóa dòng bảng công ngày đã khóa.");
        }

        dbContext.AttendanceWorkdaySummaries.RemoveRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AttendanceWorkdaySummaryListItemDto> UpdateAsync(
        UpdateAttendanceWorkdaySummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if(request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Dòng bảng công ngày không hợp lệ.");
        }

        if(request.LateMinutes < 0 || request.EarlyLeaveMinutes < 0)
        {
            throw new InvalidOperationException("Số phút đi trễ hoặc về sớm không hợp lệ.");
        }

        if(request.OvertimeMinutes < 0)
        {
            throw new InvalidOperationException("Số phút tăng ca không hợp lệ.");
        }

        var row = await dbContext.AttendanceWorkdaySummaries
            .SingleOrDefaultAsync(summary => summary.Id == request.Id, cancellationToken);

        if(row is null)
        {
            throw new InvalidOperationException("Không tìm thấy dòng bảng công ngày cần cập nhật.");
        }

        if(row.IsLocked)
        {
            throw new InvalidOperationException("Dòng bảng công ngày đã khóa, không thể điều chỉnh.");
        }

        var normalizedDayType = NormalizeEditableDayType(request.DayType);
        var normalizedCheckInAt = NormalizeEditableTime(request.CheckInAt, "Giờ vào");
        var normalizedCheckOutAt = NormalizeEditableTime(request.CheckOutAt, "Giờ ra");
        var normalizedStatusCode = NormalizeSettingValue(request.StatusCode);
        var normalizedNote = NormalizeSettingValue(request.Note);
        var statusCodeId = await ResolveStatusCodeIdAsync(normalizedStatusCode, cancellationToken);
        var now = GetDatabaseNow();

        row.DayType = normalizedDayType;
        row.CheckInAt = normalizedCheckInAt;
        row.CheckOutAt = normalizedCheckOutAt;
        row.CodeKetQuaTinhCongId = statusCodeId;
        row.LateMinutes = request.LateMinutes;
        row.EarlyLeaveMinutes = request.EarlyLeaveMinutes;
        row.RequireDocument = request.RequireDocument;
        row.Note = normalizedNote;
        row.ComputedAtUtc = now;
        row.UpdatedAtUtc = now;

        ApplyEditableOvertime(row, normalizedDayType, request.IsRegisterForOT, request.OvertimeMinutes);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadAttendanceWorkdaySummaryListItemAsync(request.Id, cancellationToken);
    }

    public async Task SetLockStateAsync(
        SetAttendanceWorkdaySummaryLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if(request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Dòng bảng công ngày không hợp lệ.");
        }

        var row = await dbContext.AttendanceWorkdaySummaries
            .SingleOrDefaultAsync(summary => summary.Id == request.Id, cancellationToken);

        if(row is null)
        {
            throw new InvalidOperationException("Không tìm thấy dòng bảng công ngày cần cập nhật.");
        }

        if(row.IsLocked == request.IsLocked)
        {
            return;
        }

        row.IsLocked = request.IsLocked;
        row.UpdatedAtUtc = GetDatabaseNow();

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid?> ResolveStatusCodeIdAsync(
        string? statusCode,
        CancellationToken cancellationToken)
    {
        if(statusCode is null)
        {
            return null;
        }

        var statusCodeId = await dbContext.AttendanceStatusCodes
            .AsNoTracking()
            .Where(row => row.Code == statusCode)
            .Select(row => (Guid?)row.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if(statusCodeId is null)
        {
            throw new InvalidOperationException($"Không tìm thấy code kết quả tính công '{statusCode}'.");
        }

        return statusCodeId;
    }

    private async Task<AttendanceWorkdaySummaryListItemDto> LoadAttendanceWorkdaySummaryListItemAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await (
            from summary in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            join shift in dbContext.Shifts.AsNoTracking()
                on summary.ShiftId equals shift.Id into shiftGroup
            from shift in shiftGroup.DefaultIfEmpty()
            join statusCode in dbContext.AttendanceStatusCodes.AsNoTracking()
                on summary.CodeKetQuaTinhCongId equals statusCode.Id into statusCodeGroup
            from statusCode in statusCodeGroup.DefaultIfEmpty()
            where summary.Id == id
            select new AttendanceWorkdaySummaryListItemDto(
                summary.Id,
                summary.EmployeeId,
                employee == null ? null : employee.EmployeeCode,
                employee == null ? null : BuildEmployeeFullName(employee.FirstName, employee.LastName),
                department == null ? null : BuildDepartmentName(department),
                position == null ? null : position.Name,
                summary.WorkDate,
                summary.DayType,
                summary.ShiftId,
                shift == null ? null : shift.Code,
                shift == null ? null : shift.ShortName,
                shift == null ? null : shift.Name,
                shift == null ? null : shift.ColorHex,
                summary.ScheduledStartAt,
                summary.ScheduledEndAt,
                summary.CheckInAt,
                summary.CheckOutAt,
                summary.LateMinutes,
                summary.EarlyLeaveMinutes,
                statusCode == null ? string.Empty : statusCode.Code,
                summary.IsLocked,
                summary.OvertimeMinutes,
                summary.OvertimeMinutes15,
                summary.OvertimeMinutes20,
                summary.OvertimeMinutes30,
                summary.CheckInForOT15,
                summary.IsRegisterForOT,
                summary.RequireDocument,
                summary.Note,
                summary.ComputedAtUtc,
                summary.CreatedAtUtc,
                summary.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return row ?? throw new InvalidOperationException("Không thể tải lại dòng bảng công ngày sau khi cập nhật.");
    }

    private static string NormalizeEditableDayType(string? dayType)
    {
        var normalizedValue = NormalizeSettingValue(dayType);
        if(normalizedValue is null)
        {
            throw new InvalidOperationException("Loại ngày công không được để trống.");
        }

        if(string.Equals(normalizedValue, "regular", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedValue, AttendanceWorkCalendarDayTypes.Regular, StringComparison.OrdinalIgnoreCase))
        {
            return AttendanceWorkCalendarDayTypes.Regular;
        }

        if(string.Equals(normalizedValue, "dayoff", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedValue, AttendanceWorkCalendarDayTypes.DayOff, StringComparison.OrdinalIgnoreCase))
        {
            return AttendanceWorkCalendarDayTypes.DayOff;
        }

        if(string.Equals(normalizedValue, "holiday", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedValue, AttendanceWorkCalendarDayTypes.Holiday, StringComparison.OrdinalIgnoreCase))
        {
            return AttendanceWorkCalendarDayTypes.Holiday;
        }

        throw new InvalidOperationException("Loại ngày công không hợp lệ.");
    }

    private static string? NormalizeEditableTime(string? value, string fieldName)
    {
        var normalizedValue = NormalizeSettingValue(value);
        if(normalizedValue is null)
        {
            return null;
        }

        if(!TimeOnly.TryParseExact(
                normalizedValue,
                ["HH:mm", "HH:mm:ss"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedTime))
        {
            throw new InvalidOperationException($"{fieldName} phải đúng định dạng HH:mm.");
        }

        return parsedTime.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static void ApplyEditableOvertime(
        AttendanceWorkdaySummaryRow row,
        string dayType,
        bool isRegisterForOT,
        int overtimeMinutes)
    {
        row.IsRegisterForOT = isRegisterForOT;
        row.OvertimeMinutes = isRegisterForOT ? overtimeMinutes : 0;
        row.OvertimeMinutes15 = 0;
        row.OvertimeMinutes20 = 0;
        row.OvertimeMinutes30 = 0;
        row.CheckInForOT15 = null;

        if(!isRegisterForOT || overtimeMinutes <= 0)
        {
            return;
        }

        switch(dayType)
        {
            case AttendanceWorkCalendarDayTypes.DayOff:
                row.OvertimeMinutes20 = overtimeMinutes;
                break;
            case AttendanceWorkCalendarDayTypes.Holiday:
                row.OvertimeMinutes30 = overtimeMinutes;
                break;
            default:
                row.OvertimeMinutes15 = overtimeMinutes;
                break;
        }
    }

    private async Task<AttendanceWorkCalendarDayType> LoadWorkCalendarDayTypeAsync(
        DateOnly workDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var configuredDayType = await dbContext.AttendanceWorkCalendarDays
                .AsNoTracking()
                .Where(day => day.WorkDate == workDate)
                .Select(day => (AttendanceWorkCalendarDayType?)day.DayType)
                .SingleOrDefaultAsync(cancellationToken);

            return configuredDayType ?? AttendanceWorkCalendarDayType.Regular;
        }
        catch(PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return AttendanceWorkCalendarDayType.Regular;
        }
    }

    private async Task<WorkdaySummaryRebuildContext> BuildRebuildContextAsync(
        DateOnly workDate,
        AttendanceWorkCalendarDayType dayType,
        bool includeEligibleEmployees,
        CancellationToken cancellationToken)
    {
        var startDateTime = workDate.ToDateTime(TimeOnly.MinValue);
        var endExclusiveDateTime = workDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var statusCodeIds = await LoadStatusCodeIdsAsync(cancellationToken);
        var punchGroups = await dbContext.AttendanceLogs
            .AsNoTracking()
            .Where(log =>
                log.EmployeeId != null
                && log.AttTime != null
                && log.AttTime >= startDateTime
                && log.AttTime < endExclusiveDateTime)
            .GroupBy(log => new
            {
                EmployeeId = log.EmployeeId!.Value,
                WorkDate = log.AttTime!.Value.Date
            })
            .Select(group => new WorkdayPunchAggregate(
                group.Key.EmployeeId,
                DateOnly.FromDateTime(group.Key.WorkDate),
                group.Count(),
                group.Min(x => x.AttTime),
                group.Max(x => x.AttTime)))
            .ToListAsync(cancellationToken);

        var totalPunchCount = punchGroups.Sum(group => group.PunchCount);
        var punchEmployeeIds = punchGroups
            .Select(group => group.EmployeeId)
            .Distinct()
            .ToArray();

        IReadOnlyList<Guid> eligibleEmployeeIds = [];
        if(includeEligibleEmployees)
        {
            eligibleEmployeeIds = await dbContext.Employees
                .AsNoTracking()
                .Where(employee => !employee.IsDeleted && employee.Status != ResignedEmployeeStatus)
                .Select(employee => employee.Id)
                .ToListAsync(cancellationToken);
        }

        var summaryEmployeeIds = punchEmployeeIds
            .Concat(eligibleEmployeeIds)
            .Distinct()
            .ToArray();

        IReadOnlyDictionary<Guid, EmployeeShiftProfile> employeeProfiles =
            new Dictionary<Guid, EmployeeShiftProfile>();
        IReadOnlyDictionary<Guid, AttendanceShiftRow> shiftsById =
            new Dictionary<Guid, AttendanceShiftRow>();
        IReadOnlyDictionary<(Guid EmployeeId, DateOnly WorkDate), Guid> shiftAssignmentsByKey =
            new Dictionary<(Guid EmployeeId, DateOnly WorkDate), Guid>();
        IReadOnlyList<ShiftSchedulingSettingRow> shiftSchedulingSettings = [];

        if(summaryEmployeeIds.Length > 0)
        {
            employeeProfiles = await LoadEmployeeProfilesAsync(summaryEmployeeIds, cancellationToken);
            shiftsById = await LoadShiftsAsync(cancellationToken);
            shiftAssignmentsByKey = await LoadShiftAssignmentsByKeyAsync(
                summaryEmployeeIds,
                workDate,
                cancellationToken);
            shiftSchedulingSettings = await LoadShiftSchedulingSettingsAsync(cancellationToken);
        }

        var existingRows = await dbContext.AttendanceWorkdaySummaries
            .Where(summary => summary.WorkDate == workDate)
            .ToListAsync(cancellationToken);

        var rowsByKey = existingRows
            .GroupBy(row => (row.EmployeeId, row.WorkDate))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(row => row.UpdatedAtUtc ?? row.CreatedAtUtc)
                    .First());

        return new WorkdaySummaryRebuildContext(
            workDate,
            dayType,
            AttendanceWorkCalendarDayTypes.GetDisplayName(dayType),
            statusCodeIds,
            punchGroups,
            totalPunchCount,
            eligibleEmployeeIds,
            employeeProfiles,
            shiftsById,
            shiftAssignmentsByKey,
            shiftSchedulingSettings,
            existingRows,
            rowsByKey,
            GetDatabaseNow());
    }

    private async Task<RebuildAttendanceWorkdaySummaryResult> SaveAndBuildResultAsync(
        WorkdaySummaryRebuildContext context,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var updatedSummaryCount = context.RowsByKey.Values.Count(row => !row.IsLocked);
        var skippedLockedCount = context.RowsByKey.Values.Count(row => row.IsLocked);

        await dbContext.SaveChangesAsync(cancellationToken);

        var rebuiltSummaryCount = await dbContext.AttendanceWorkdaySummaries
            .AsNoTracking()
            .Where(summary => summary.WorkDate == context.WorkDate)
            .CountAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new RebuildAttendanceWorkdaySummaryResult(
            context.WorkDate,
            rebuiltSummaryCount,
            context.TotalPunchCount,
            // Kept for contract compatibility after attendance_status_codes dropped WorkdayCredit.
            0m,
            updatedSummaryCount,
            skippedLockedCount);
    }

    private void UpsertSummaryRow(
        WorkdaySummaryRebuildContext context,
        Guid employeeId,
        string? checkInAt,
        string? checkOutAt,
        Guid? statusCodeId,
        string? note)
    {
        var key = (employeeId, context.WorkDate);
        var shiftInfo = ResolveShiftInfo(
            context.EmployeeProfiles,
            context.ShiftsById,
            context.ShiftAssignmentsByKey,
            context.ShiftSchedulingSettings,
            employeeId,
            context.WorkDate);

        if(context.RowsByKey.TryGetValue(key, out var existingRow))
        {
            if(existingRow.IsLocked)
            {
                return;
            }

            ApplySummaryRowValues(
                existingRow,
                context.WorkDate,
                context.DayTypeDisplay,
                shiftInfo,
                checkInAt,
                checkOutAt,
                statusCodeId,
                note,
                context.Now);
            return;
        }

        var row = CreateSummaryRow(
            employeeId,
            context.WorkDate,
            context.DayTypeDisplay,
            shiftInfo,
            checkInAt,
            checkOutAt,
            statusCodeId,
            note,
            context.Now);

        dbContext.AttendanceWorkdaySummaries.Add(row);
        context.RowsByKey[key] = row;
    }

    private void DeleteUnlockedRowsOutsideKeys(
        WorkdaySummaryRebuildContext context,
        ISet<(Guid EmployeeId, DateOnly WorkDate)> expectedKeys)
    {
        foreach(var row in context.ExistingRows)
        {
            var key = (row.EmployeeId, row.WorkDate);
            if(row.IsLocked || expectedKeys.Contains(key))
            {
                continue;
            }

            dbContext.AttendanceWorkdaySummaries.Remove(row);
            context.RowsByKey.Remove(key);
        }
    }

    private static AttendanceWorkdaySummaryRow CreateSummaryRow(
        Guid employeeId,
        DateOnly workDate,
        string dayType,
        ShiftInfo? shiftInfo,
        string? checkInAt,
        string? checkOutAt,
        Guid? statusCodeId,
        string? note,
        DateTime now)
    {
        var row = new AttendanceWorkdaySummaryRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            CreatedAtUtc = now
        };

        ApplySummaryRowValues(
            row,
            workDate,
            dayType,
            shiftInfo,
            checkInAt,
            checkOutAt,
            statusCodeId,
            note,
            now);

        return row;
    }

    private static void ApplySummaryRowValues(
        AttendanceWorkdaySummaryRow row,
        DateOnly workDate,
        string dayType,
        ShiftInfo? shiftInfo,
        string? checkInAt,
        string? checkOutAt,
        Guid? statusCodeId,
        string? note,
        DateTime now)
    {
        row.WorkDate = workDate;
        row.DayType = dayType;
        row.ShiftId = shiftInfo?.ShiftId;
        row.ScheduledStartAt = shiftInfo?.ScheduledStartAt;
        row.ScheduledEndAt = shiftInfo?.ScheduledEndAt;
        row.CheckInAt = checkInAt;
        row.CheckOutAt = checkOutAt;
        row.LateMinutes = 0;
        row.EarlyLeaveMinutes = 0;
        row.ComputedAtUtc = now;
        row.UpdatedAtUtc = now;
        row.Note = note;
        row.CodeKetQuaTinhCongId = statusCodeId;
        row.IsLocked = false;
        row.OvertimeMinutes = 0;
        row.OvertimeMinutes15 = 0;
        row.OvertimeMinutes20 = 0;
        row.OvertimeMinutes30 = 0;
        row.CheckInForOT15 = null;
        row.IsRegisterForOT = false;
        row.RequireDocument = false;

        if(row.CreatedAtUtc == default)
        {
            row.CreatedAtUtc = now;
        }
    }

    private async Task<IReadOnlyDictionary<string, Guid>> LoadStatusCodeIdsAsync(CancellationToken cancellationToken)
    {
        var statusCodes = await dbContext.AttendanceStatusCodes
            .AsNoTracking()
            .Where(statusCode =>
                statusCode.Code == FullPunchStatusCode
                || statusCode.Code == PartialPunchStatusCode
                || statusCode.Code == UnauthorizedAbsenceStatusCode
                || statusCode.Code == MissingLogStatusCode
                || statusCode.Code == FullWorkStatusCode
                || statusCode.Code == LateEarlyStatusCode
                || statusCode.Code == AbnormalStatusCode)
            .Select(statusCode => new { statusCode.Code, statusCode.Id })
            .ToListAsync(cancellationToken);

        return statusCodes.ToDictionary(
            statusCode => statusCode.Code,
            statusCode => statusCode.Id,
            StringComparer.OrdinalIgnoreCase);
    }

    private static Guid? ResolveStatusCodeId(
        IReadOnlyDictionary<string, Guid> statusCodeIds,
        int punchCount)
    {
        var statusCode = punchCount > 1 ? FullPunchStatusCode : PartialPunchStatusCode;
        return ResolveStatusCodeId(statusCodeIds, statusCode);
    }

    private static Guid? ResolveStatusCodeId(
        IReadOnlyDictionary<string, Guid> statusCodeIds,
        string statusCode)
    {
        return statusCodeIds.TryGetValue(statusCode, out var statusCodeId)
            ? statusCodeId
            : null;
    }

    private static Guid ResolveRequiredStatusCodeId(
        IReadOnlyDictionary<string, Guid> statusCodeIds,
        string statusCode)
    {
        return statusCodeIds.TryGetValue(statusCode, out var statusCodeId)
            ? statusCodeId
            : throw new InvalidOperationException(
                $"Không tìm thấy code kết quả tính công mặc định '{statusCode}'.");
    }

    private static string? FormatPunchTime(DateTime? value) =>
        value.HasValue
            ? value.Value.ToString("HH:mm:ss")
            : null;

    private static DateTime GetDatabaseNow() =>
        DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified);

    private async Task<IReadOnlyDictionary<Guid, EmployeeShiftProfile>> LoadEmployeeProfilesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        if(employeeIds.Count == 0)
        {
            return new Dictionary<Guid, EmployeeShiftProfile>();
        }

        var profiles = await (
            from employee in dbContext.Employees.AsNoTracking()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            where employeeIds.Contains(employee.Id)
            select new EmployeeShiftProfile(
                employee.Id,
                employee.EmployeeCode,
                employee.FirstName,
                employee.LastName,
                department == null ? null : BuildDepartmentPath(department),
                department == null ? null : BuildDepartmentName(department)))
            .ToListAsync(cancellationToken);

        return profiles.ToDictionary(profile => profile.EmployeeId);
    }

    private async Task<IReadOnlyDictionary<Guid, AttendanceShiftRow>> LoadShiftsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Shifts
            .AsNoTracking()
            .ToDictionaryAsync(shift => shift.Id, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<(Guid EmployeeId, DateOnly WorkDate), Guid>> LoadShiftAssignmentsByKeyAsync(
        IReadOnlyCollection<Guid> employeeIds,
        DateOnly workDate,
        CancellationToken cancellationToken)
    {
        if(employeeIds.Count == 0)
        {
            return new Dictionary<(Guid EmployeeId, DateOnly WorkDate), Guid>();
        }

        var assignments = await dbContext.ShiftAssignments
            .AsNoTracking()
            .Where(assignment =>
                employeeIds.Contains(assignment.EmployeeId)
                && assignment.WorkDate == workDate)
            .Select(assignment => new
            {
                assignment.EmployeeId,
                assignment.WorkDate,
                assignment.ShiftId
            })
            .ToListAsync(cancellationToken);

        return assignments.ToDictionary(
            assignment => (assignment.EmployeeId, assignment.WorkDate),
            assignment => assignment.ShiftId);
    }

    private async Task<IReadOnlyList<ShiftSchedulingSettingRow>> LoadShiftSchedulingSettingsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.ShiftSchedulingSettings
            .AsNoTracking()
            .Where(setting => setting.IsActive && setting.ShiftId.HasValue)
            .OrderByDescending(setting => setting.UpdatedAtUtc ?? setting.CreatedAtUtc)
            .ThenBy(setting => setting.Id)
            .ToListAsync(cancellationToken);
    }

    private static ShiftInfo? ResolveShiftInfo(
        IReadOnlyDictionary<Guid, EmployeeShiftProfile> employeeProfiles,
        IReadOnlyDictionary<Guid, AttendanceShiftRow> shiftsById,
        IReadOnlyDictionary<(Guid EmployeeId, DateOnly WorkDate), Guid> shiftAssignmentsByKey,
        IReadOnlyList<ShiftSchedulingSettingRow> shiftSchedulingSettings,
        Guid employeeId,
        DateOnly workDate)
    {
        if(shiftAssignmentsByKey.TryGetValue((employeeId, workDate), out var assignedShiftId))
        {
            return TryBuildShiftInfo(shiftsById, assignedShiftId);
        }

        if(!employeeProfiles.TryGetValue(employeeId, out var employeeProfile))
        {
            return null;
        }

        var employeeSetting = shiftSchedulingSettings.FirstOrDefault(setting =>
            setting.ClassificationType == EmployeeClassificationType
            && MatchesEmployeeSetting(setting, employeeProfile));
        if(employeeSetting?.ShiftId is Guid employeeShiftId)
        {
            var shiftInfo = TryBuildShiftInfo(shiftsById, employeeShiftId);
            if(shiftInfo is not null)
            {
                return shiftInfo;
            }
        }

        var departmentSetting = shiftSchedulingSettings
            .Where(setting =>
                setting.ClassificationType == DepartmentClassificationType
                && MatchesDepartmentSetting(setting, employeeProfile))
            .OrderByDescending(setting => NormalizeSettingValue(setting.Value)?.Length ?? 0)
            .ThenByDescending(setting => setting.UpdatedAtUtc ?? setting.CreatedAtUtc)
            .FirstOrDefault();
        if(departmentSetting?.ShiftId is Guid departmentShiftId)
        {
            return TryBuildShiftInfo(shiftsById, departmentShiftId);
        }

        return null;
    }

    private static ShiftInfo? TryBuildShiftInfo(
        IReadOnlyDictionary<Guid, AttendanceShiftRow> shiftsById,
        Guid shiftId)
    {
        return shiftsById.TryGetValue(shiftId, out var shift)
            ? new ShiftInfo(
                shift.Id,
                NormalizeSettingValue(shift.StartTime),
                NormalizeSettingValue(shift.EndTime),
                NormalizeSettingValue(shift.BreakStartTime),
                NormalizeSettingValue(shift.BreakEndTime))
            : null;
    }

    private static bool MatchesEmployeeSetting(
        ShiftSchedulingSettingRow setting,
        EmployeeShiftProfile employeeProfile)
    {
        var normalizedSettingValue = NormalizeSettingValue(setting.Value);
        if(normalizedSettingValue is null)
        {
            return false;
        }

        return employeeProfile.CandidateLookupValues.Any(value =>
            string.Equals(value, normalizedSettingValue, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesDepartmentSetting(
        ShiftSchedulingSettingRow setting,
        EmployeeShiftProfile employeeProfile)
    {
        var normalizedSettingValue = NormalizeSettingValue(setting.Value);
        if(normalizedSettingValue is null)
        {
            return false;
        }

        if(employeeProfile.DepartmentPath is not null)
        {
            if(string.Equals(employeeProfile.DepartmentPath, normalizedSettingValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if(employeeProfile.DepartmentPath.StartsWith($"{normalizedSettingValue} / ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return employeeProfile.DepartmentName is not null
            && string.Equals(employeeProfile.DepartmentName, normalizedSettingValue, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDepartmentPath(AttendanceDepartmentRow department) =>
        string.Join(
            " / ",
            new[]
            {
                NormalizeSettingValue(department.CenterName),
                NormalizeSettingValue(department.DepartmentOrWorkshopName),
                NormalizeSettingValue(department.TeamName),
                NormalizeSettingValue(department.GroupName)
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

    private static string? BuildDepartmentName(AttendanceDepartmentRow department) =>
        NormalizeSettingValue(department.GroupName)
        ?? NormalizeSettingValue(department.TeamName)
        ?? NormalizeSettingValue(department.DepartmentOrWorkshopName)
        ?? NormalizeSettingValue(department.CenterName);

    private static string BuildEmployeeLookupText(string? employeeCode, string fullName)
    {
        return string.IsNullOrWhiteSpace(employeeCode)
            ? fullName
            : $"{employeeCode.Trim()} - {fullName}";
    }

    private static string BuildEmployeeFullName(string? firstName, string? lastName)
    {
        var parts = new[] { lastName, firstName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part!.Trim())
            .ToArray();

        return parts.Length == 0 ? string.Empty : string.Join(" ", parts);
    }

    private static string? NormalizeSettingValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record WorkdayPunchAggregate(
        Guid EmployeeId,
        DateOnly WorkDate,
        int PunchCount,
        DateTime? FirstPunchAt,
        DateTime? LastPunchAt);

    private sealed record ShiftInfo(
        Guid ShiftId,
        string? ScheduledStartAt,
        string? ScheduledEndAt,
        string? BreakStartAt,
        string? BreakEndAt);

    private sealed record EmployeeShiftProfile(
        Guid EmployeeId,
        string? EmployeeCode,
        string? FirstName,
        string? LastName,
        string? DepartmentPath,
        string? DepartmentName)
    {
        public string FullName { get; } = BuildEmployeeFullName(FirstName, LastName);

        public IReadOnlyList<string> CandidateLookupValues { get; } = BuildCandidateLookupValues(
            EmployeeCode,
            BuildEmployeeFullName(FirstName, LastName));

        private static IReadOnlyList<string> BuildCandidateLookupValues(
            string? employeeCode,
            string fullName)
        {
            var values = new string?[]
                {
                    BuildEmployeeLookupText(employeeCode, fullName),
                    NormalizeSettingValue(employeeCode),
                    NormalizeSettingValue(fullName)
                }
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return values;
        }
    }

    private sealed class WorkdaySummaryRebuildContext(
        DateOnly workDate,
        AttendanceWorkCalendarDayType dayType,
        string dayTypeDisplay,
        IReadOnlyDictionary<string, Guid> statusCodeIds,
        IReadOnlyList<WorkdayPunchAggregate> punchGroups,
        int totalPunchCount,
        IReadOnlyList<Guid> eligibleEmployeeIds,
        IReadOnlyDictionary<Guid, EmployeeShiftProfile> employeeProfiles,
        IReadOnlyDictionary<Guid, AttendanceShiftRow> shiftsById,
        IReadOnlyDictionary<(Guid EmployeeId, DateOnly WorkDate), Guid> shiftAssignmentsByKey,
        IReadOnlyList<ShiftSchedulingSettingRow> shiftSchedulingSettings,
        IReadOnlyList<AttendanceWorkdaySummaryRow> existingRows,
        Dictionary<(Guid EmployeeId, DateOnly WorkDate), AttendanceWorkdaySummaryRow> rowsByKey,
        DateTime now)
    {
        public DateOnly WorkDate { get; } = workDate;
        public AttendanceWorkCalendarDayType DayType { get; } = dayType;
        public string DayTypeDisplay { get; } = dayTypeDisplay;
        public IReadOnlyDictionary<string, Guid> StatusCodeIds { get; } = statusCodeIds;
        public IReadOnlyList<WorkdayPunchAggregate> PunchGroups { get; } = punchGroups;
        public int TotalPunchCount { get; } = totalPunchCount;
        public IReadOnlyList<Guid> EligibleEmployeeIds { get; } = eligibleEmployeeIds;
        public IReadOnlyDictionary<Guid, EmployeeShiftProfile> EmployeeProfiles { get; } = employeeProfiles;
        public IReadOnlyDictionary<Guid, AttendanceShiftRow> ShiftsById { get; } = shiftsById;
        public IReadOnlyDictionary<(Guid EmployeeId, DateOnly WorkDate), Guid> ShiftAssignmentsByKey { get; } =
            shiftAssignmentsByKey;
        public IReadOnlyList<ShiftSchedulingSettingRow> ShiftSchedulingSettings { get; } =
            shiftSchedulingSettings;
        public IReadOnlyList<AttendanceWorkdaySummaryRow> ExistingRows { get; } = existingRows;
        public Dictionary<(Guid EmployeeId, DateOnly WorkDate), AttendanceWorkdaySummaryRow> RowsByKey { get; } =
            rowsByKey;
        public DateTime Now { get; } = now;
    }
}
