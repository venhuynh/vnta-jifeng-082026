using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.CaKip.CaiDatCa;

public sealed partial class DatabaseAttendanceShiftService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope)
    : IAttendanceShiftService
{
    private static readonly string[] ValidWorkingDayCodes =
    [
        "Mon",
        "Tue",
        "Wed",
        "Thu",
        "Fri",
        "Sat",
        "Sun"
    ];

    public async Task<IReadOnlyList<AttendanceShiftListItemDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureShiftTableAsync(cancellationToken);

        var shifts = await dbContext.Shifts
            .AsNoTracking()
            .OrderBy(shift => shift.Code)
            .ThenBy(shift => shift.Name)
            .ToListAsync(cancellationToken);

        return shifts
            .Select(MapToDto)
            .ToArray();
    }

    public async Task<string?> ValidateAsync(
        UpsertAttendanceShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCode = Normalize(request.Code);
        var normalizedName = Normalize(request.Name);
        var normalizedShortName = Normalize(request.ShortName);
        var normalizedDescription = Normalize(request.Description);
        var normalizedDepartmentGroup = Normalize(request.DepartmentGroup);
        var normalizedStartTime = Normalize(request.StartTime);
        var normalizedEndTime = Normalize(request.EndTime);
        var normalizedBreakStartTime = Normalize(request.BreakStartTime);
        var normalizedBreakEndTime = Normalize(request.BreakEndTime);
        var normalizedColorHex = Normalize(request.ColorHex);
        var normalizedWorkingDays = NormalizeWorkingDays(request.WorkingDays);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return "Ma ca khong duoc de trong.";
        }

        if (normalizedCode.Length > 50)
        {
            return "Ma ca khong duoc vuot qua 50 ky tu.";
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return "Ten ca khong duoc de trong.";
        }

        if (normalizedName.Length > 200)
        {
            return "Ten ca khong duoc vuot qua 200 ky tu.";
        }

        if (normalizedShortName?.Length > 50)
        {
            return "Ten ngan khong duoc vuot qua 50 ky tu.";
        }

        if (normalizedDescription?.Length > 1000)
        {
            return "Mo ta khong duoc vuot qua 1000 ky tu.";
        }

        if (string.IsNullOrWhiteSpace(normalizedDepartmentGroup))
        {
            return "Nhom bo phan khong duoc de trong.";
        }

        if (normalizedDepartmentGroup.Length > 100)
        {
            return "Nhom bo phan khong duoc vuot qua 100 ky tu.";
        }

        if (!TryParseTime(normalizedStartTime, out var start))
        {
            return "Gio bat dau phai dung dinh dang HH:mm.";
        }

        if (!TryParseTime(normalizedEndTime, out var end))
        {
            return "Gio ket thuc phai dung dinh dang HH:mm.";
        }

        if (!request.IsOvernight && end <= start)
        {
            return "Gio ket thuc phai lon hon gio bat dau neu ca khong qua ngay.";
        }

        var hasBreakStart = !string.IsNullOrWhiteSpace(normalizedBreakStartTime);
        var hasBreakEnd = !string.IsNullOrWhiteSpace(normalizedBreakEndTime);
        if (hasBreakStart != hasBreakEnd)
        {
            return "Gio nghi bat dau va gio nghi ket thuc phai cung duoc nhap.";
        }

        if (hasBreakStart)
        {
            if (!TryParseTime(normalizedBreakStartTime, out var breakStart)
                || !TryParseTime(normalizedBreakEndTime, out var breakEnd))
            {
                return "Gio nghi phai dung dinh dang HH:mm.";
            }

            if (!IsRangeInsideShift(start, end, request.IsOvernight, breakStart, breakEnd))
            {
                return "Khoang nghi phai nam trong khoang gio ca.";
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedColorHex)
            && !HexColorRegex().IsMatch(normalizedColorHex))
        {
            return "Mau hien thi phai dung dinh dang #RRGGBB.";
        }

        if (!IsValidWorkingDays(normalizedWorkingDays))
        {
            return "Ngay lam mac dinh khong hop le.";
        }

        await EnsureShiftTableAsync(cancellationToken);
        var duplicateCode = await dbContext.Shifts
            .AsNoTracking()
            .AnyAsync(
                shift => shift.Id != request.Id
                    && shift.Code.ToLower() == normalizedCode.ToLower(),
                cancellationToken);

        if (duplicateCode)
        {
            return "Ma ca da ton tai. Hay dung ma khac.";
        }

        return null;
    }

    public async Task<AttendanceShiftListItemDto> SaveAsync(
        UpsertAttendanceShiftRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = await ValidateAsync(request, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            throw new InvalidOperationException(validationMessage);
        }

        await EnsureShiftTableAsync(cancellationToken);

        var normalizedId = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        var now = DateTime.UtcNow;
        var normalizedCreatedAt = request.CreatedAtUtc == default ? now : request.CreatedAtUtc;

        AttendanceShiftRow row;
        if (isNew)
        {
            row = new AttendanceShiftRow
            {
                Id = normalizedId,
                CreatedAtUtc = ToDatabaseTimestamp(normalizedCreatedAt)
            };

            dbContext.Shifts.Add(row);
        }
        else
        {
            row = await dbContext.Shifts.SingleOrDefaultAsync(shift => shift.Id == normalizedId, cancellationToken)
                ?? throw new InvalidOperationException("Khong tim thay ca lam de cap nhat.");

            if (row.CreatedAtUtc == default)
            {
                row.CreatedAtUtc = ToDatabaseTimestamp(normalizedCreatedAt);
            }
        }

        Apply(row, request, normalizedId);
        row.UpdatedAtUtc = ToDatabaseTimestamp(now);

        RefineAuditActionIfActive(isNew ? AuditActions.Shift.Created : AuditActions.Shift.Updated);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(row);
    }

    private static void Apply(
        AttendanceShiftRow row,
        UpsertAttendanceShiftRequest request,
        Guid id)
    {
        row.Id = id;
        row.Code = Normalize(request.Code) ?? string.Empty;
        row.Name = Normalize(request.Name) ?? string.Empty;
        row.ShortName = Normalize(request.ShortName);
        row.Description = Normalize(request.Description);
        row.DepartmentGroup = Normalize(request.DepartmentGroup) ?? string.Empty;
        row.StartTime = Normalize(request.StartTime) ?? string.Empty;
        row.EndTime = Normalize(request.EndTime) ?? string.Empty;
        row.IsOvernight = request.IsOvernight;
        row.BreakStartTime = Normalize(request.BreakStartTime);
        row.BreakEndTime = Normalize(request.BreakEndTime);
        row.Status = request.Status;
        row.ColorHex = Normalize(request.ColorHex);
        row.WorkingDays = NormalizeWorkingDays(request.WorkingDays);
    }

    private static AttendanceShiftListItemDto MapToDto(AttendanceShiftRow row) =>
        new(
            row.Id,
            row.Code,
            row.Name,
            row.ShortName,
            row.Description,
            row.DepartmentGroup,
            row.StartTime,
            row.EndTime,
            row.IsOvernight,
            row.BreakStartTime,
            row.BreakEndTime,
            row.Status,
            row.ColorHex,
            row.WorkingDays,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);

    private static bool IsRangeInsideShift(
        TimeOnly shiftStart,
        TimeOnly shiftEnd,
        bool isOvernight,
        TimeOnly breakStart,
        TimeOnly breakEnd)
    {
        var shiftStartMinutes = ToMinutes(shiftStart);
        var shiftEndMinutes = ToMinutes(shiftEnd);
        if (isOvernight && shiftEndMinutes <= shiftStartMinutes)
        {
            shiftEndMinutes += 24 * 60;
        }

        var breakStartMinutes = NormalizeToShiftDay(ToMinutes(breakStart), shiftStartMinutes, isOvernight);
        var breakEndMinutes = NormalizeToShiftDay(ToMinutes(breakEnd), shiftStartMinutes, isOvernight);
        if (breakEndMinutes <= breakStartMinutes)
        {
            breakEndMinutes += 24 * 60;
        }

        return breakStartMinutes >= shiftStartMinutes
            && breakEndMinutes <= shiftEndMinutes
            && breakEndMinutes > breakStartMinutes;
    }

    private static int NormalizeToShiftDay(int minutes, int shiftStartMinutes, bool isOvernight) =>
        isOvernight && minutes < shiftStartMinutes
            ? minutes + 24 * 60
            : minutes;

    private static int ToMinutes(TimeOnly value) => value.Hour * 60 + value.Minute;

    private static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(
            value,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);

    private static bool IsValidWorkingDays(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var codes = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        return codes.Length == codes.Distinct(StringComparer.OrdinalIgnoreCase).Count()
            && codes.All(code => ValidWorkingDayCodes.Contains(code, StringComparer.OrdinalIgnoreCase));
    }

    private static string? NormalizeWorkingDays(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var selectedCodes = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orderedCodes = ValidWorkingDayCodes
            .Where(code => selectedCodes.Contains(code))
            .ToArray();

        return orderedCodes.Length == 0 ? null : string.Join(',', orderedCodes);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private void RefineAuditActionIfActive(string action)
    {
        if (auditScope.Current is not null)
        {
            auditScope.RefineAction(action);
        }
    }

    private async Task EnsureShiftTableAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS public.shifts (
                "Id" uuid NOT NULL,
                "Code" character varying(50) NOT NULL,
                "Name" character varying(200) NOT NULL,
                "Description" character varying(1000) NULL,
                "DepartmentGroup" character varying(100) NOT NULL,
                "StartTime" character varying(5) NOT NULL,
                "EndTime" character varying(5) NOT NULL,
                "IsOvernight" boolean NOT NULL,
                "BreakStartTime" character varying(5) NULL,
                "BreakEndTime" character varying(5) NULL,
                "Status" integer NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                "ColorHex" character varying(7) NULL,
                "ShortName" character varying(50) NULL,
                "WorkingDays" character varying(50) NULL,
                CONSTRAINT "PK_shifts" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_shifts_Code"
            ON public.shifts ("Code");
            """,
            cancellationToken);
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();
}
