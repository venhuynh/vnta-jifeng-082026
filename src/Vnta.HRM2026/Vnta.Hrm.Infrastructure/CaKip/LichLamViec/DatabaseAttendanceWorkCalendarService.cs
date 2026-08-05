using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.CaKip.LichLamViec;

// Calendar là nguồn cấu hình ngày đặc biệt; Bảng công tháng chỉ đọc nó để bổ sung ý nghĩa hiển thị cho cột ngày.
public sealed class DatabaseAttendanceWorkCalendarService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IAttendanceWorkCalendarService
{
    private const int MinimumSupportedYear = 1900;
    private const int MaximumSupportedYear = 2100;

    public async Task<AttendanceWorkCalendarYearDto> GetYearAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        ValidateYear(year);
        // Guard tương thích schema được giữ ở service calendar để mọi consumer nhận cùng behavior.
        await EnsureWorkCalendarTableAsync(cancellationToken);

        var fromDate = new DateOnly(year, 1, 1);
        var toDate = new DateOnly(year, 12, 31);

        var days = await dbContext.AttendanceWorkCalendarDays
            .AsNoTracking()
            .Where(day => day.WorkDate >= fromDate && day.WorkDate <= toDate)
            .OrderBy(day => day.WorkDate)
            .Select(day => new AttendanceWorkCalendarDayDto(
                day.Id,
                day.WorkDate,
                day.DayType,
                day.Name,
                day.Note,
                day.CreatedAtUtc,
                day.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new AttendanceWorkCalendarYearDto(year, days);
    }

    public async Task<AttendanceWorkCalendarYearDto> EnsureSundayDayOffsAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        ValidateYear(year);
        await EnsureWorkCalendarTableAsync(cancellationToken);

        if (auditScope.Current is { } command)
        {
            await auditedMutation.ExecuteAsync(
                    command,
                    token => StageSundayDayOffsAsync(year, token),
                    result => CreateEnsureSundayDaysOffAuditEvent(year, result),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var result = await StageSundayDayOffsAsync(year, cancellationToken).ConfigureAwait(false);
            if (result.AffectedCount > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return await GetYearAsync(year, cancellationToken).ConfigureAwait(false);
    }

    private async Task<EnsureSundayDayOffsResult> StageSundayDayOffsAsync(
        int year,
        CancellationToken cancellationToken)
    {
        var now = GetDatabaseNow();
        var sundayDates = GetSundayDates(year);
        var fromDate = new DateOnly(year, 1, 1);
        var toDate = new DateOnly(year, 12, 31);
        // Đọc toàn bộ năm trước khi upsert để mỗi Chủ nhật có đúng một record, không phát sinh query theo từng ngày.
        var existingRows = await dbContext.AttendanceWorkCalendarDays
            .Where(day => day.WorkDate >= fromDate && day.WorkDate <= toDate)
            .ToListAsync(cancellationToken);
        var rowsByDate = existingRows.ToDictionary(day => day.WorkDate, day => day);
        var createdCount = 0;
        var updatedCount = 0;

        foreach(var sundayDate in sundayDates)
        {
            if(rowsByDate.TryGetValue(sundayDate, out var row))
            {
                var needsUpdate = row.DayType != AttendanceWorkCalendarDayType.DayOff
                    || row.Name is not null
                    || row.CreatedAtUtc == default;
                if (!needsUpdate)
                {
                    continue;
                }

                row.DayType = AttendanceWorkCalendarDayType.DayOff;
                row.Name = null;
                row.UpdatedAtUtc = now;
                if(row.CreatedAtUtc == default)
                {
                    row.CreatedAtUtc = now;
                }
                updatedCount++;
                continue;
            }

            dbContext.AttendanceWorkCalendarDays.Add(new AttendanceWorkCalendarDayRow
            {
                Id = Guid.NewGuid(),
                WorkDate = sundayDate,
                DayType = AttendanceWorkCalendarDayType.DayOff,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            createdCount++;
        }

        return new EnsureSundayDayOffsResult(createdCount, updatedCount);
    }

    public async Task<string?> ValidateAsync(
        UpsertAttendanceWorkCalendarDayRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedDayType = request.DayType;
        var normalizedName = Normalize(request.Name);
        var normalizedNote = Normalize(request.Note);

        if(request.WorkDate == default)
        {
            return "Ngày làm việc không hợp lệ.";
        }

        ValidateYear(request.WorkDate.Year);

        if(!AttendanceWorkCalendarDayTypes.IsSpecialDay(normalizedDayType))
        {
            return "Loại ngày chỉ được là Ngày nghỉ hoặc Ngày lễ.";
        }

        if(normalizedDayType == AttendanceWorkCalendarDayType.Holiday
            && string.IsNullOrWhiteSpace(normalizedName))
        {
            return "Tên ngày lễ không được để trống.";
        }

        if(normalizedName?.Length > 200)
        {
            return "Tên ngày không được vượt quá 200 ký tự.";
        }

        if(normalizedNote?.Length > 500)
        {
            return "Ghi chú không được vượt quá 500 ký tự.";
        }

        await EnsureWorkCalendarTableAsync(cancellationToken);
        var duplicateDate = await dbContext.AttendanceWorkCalendarDays
            .AsNoTracking()
            .AnyAsync(
                day => day.Id != request.Id && day.WorkDate == request.WorkDate,
                cancellationToken);

        return duplicateDate
            ? "Ngày này đã có cấu hình lịch làm việc."
            : null;
    }

    public async Task<AttendanceWorkCalendarDayDto> SaveAsync(
        UpsertAttendanceWorkCalendarDayRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = await ValidateAsync(request, cancellationToken);
        if(!string.IsNullOrWhiteSpace(validationMessage))
        {
            throw new InvalidOperationException(validationMessage);
        }

        await EnsureWorkCalendarTableAsync(cancellationToken);

        var normalizedId = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        var now = GetDatabaseNow();
        var normalizedCreatedAt = request.CreatedAtUtc == default
            ? now
            : ToDatabaseTimestamp(request.CreatedAtUtc);

        AttendanceWorkCalendarDayRow row;
        if(isNew)
        {
            row = new AttendanceWorkCalendarDayRow
            {
                Id = normalizedId,
                CreatedAtUtc = normalizedCreatedAt
            };

            dbContext.AttendanceWorkCalendarDays.Add(row);
        }
        else
        {
            row = await dbContext.AttendanceWorkCalendarDays
                .SingleOrDefaultAsync(day => day.Id == normalizedId, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy ngày cần cập nhật.");

            if(row.CreatedAtUtc == default)
            {
                row.CreatedAtUtc = normalizedCreatedAt;
            }
        }

        Apply(row, request, normalizedId);
        row.UpdatedAtUtc = now;

        RefineAuditActionIfActive(isNew ? AuditActions.WorkCalendarDay.Created : AuditActions.WorkCalendarDay.Updated);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(row);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if(id == Guid.Empty)
        {
            throw new InvalidOperationException("Thiếu ngày cần đưa về Ngày thường.");
        }

        await EnsureWorkCalendarTableAsync(cancellationToken);

        var row = await dbContext.AttendanceWorkCalendarDays
            .SingleOrDefaultAsync(day => day.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy ngày cần đưa về Ngày thường.");

        dbContext.AttendanceWorkCalendarDays.Remove(row);
        RefineAuditActionIfActive(AuditActions.WorkCalendarDay.Deleted);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(
        AttendanceWorkCalendarDayRow row,
        UpsertAttendanceWorkCalendarDayRequest request,
        Guid id)
    {
        row.Id = id;
        row.WorkDate = request.WorkDate;
        row.DayType = request.DayType;
        row.Name = Normalize(request.Name);
        row.Note = Normalize(request.Note);
    }

    private static AttendanceWorkCalendarDayDto MapToDto(AttendanceWorkCalendarDayRow row) =>
        new(
            row.Id,
            row.WorkDate,
            row.DayType,
            row.Name,
            row.Note,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);

    private static void ValidateYear(int year)
    {
        if(year is < MinimumSupportedYear or > MaximumSupportedYear)
        {
            throw new InvalidOperationException("Năm lịch làm việc phải nằm trong khoảng 1900 đến 2100.");
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime GetDatabaseNow() =>
        DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified);

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private static IReadOnlyList<DateOnly> GetSundayDates(int year)
    {
        var firstDate = new DateOnly(year, 1, 1);
        var offset = ((int)DayOfWeek.Sunday - (int)firstDate.DayOfWeek + 7) % 7;
        var sundayDate = firstDate.AddDays(offset);
        var toDate = new DateOnly(year, 12, 31);
        var dates = new List<DateOnly>();

        while(sundayDate <= toDate)
        {
            dates.Add(sundayDate);
            sundayDate = sundayDate.AddDays(7);
        }

        return dates;
    }

    private void RefineAuditActionIfActive(string action)
    {
        if (auditScope.Current is not null)
        {
            auditScope.RefineAction(action);
        }
    }

    private static AuditOperationEvent CreateEnsureSundayDaysOffAuditEvent(
        int year,
        EnsureSundayDayOffsResult result) =>
        new(
            AuditActions.WorkCalendarDay.EnsureSundayDaysOff,
            AuditEntityTypes.WorkCalendarDay,
            EntityId: $"year:{year}",
            EntityDisplayName: $"Sunday days off for {year}",
            Outcome: result.AffectedCount == 0
                ? AuditOperationOutcome.NoChanges
                : AuditOperationOutcome.Succeeded,
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["year"] = year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["ruleVersion"] = "1",
                ["createdCount"] = result.CreatedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["updatedCount"] = result.UpdatedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["affectedCount"] = result.AffectedCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });

    private sealed record EnsureSundayDayOffsResult(int CreatedCount, int UpdatedCount)
    {
        public int AffectedCount => CreatedCount + UpdatedCount;
    }

    private async Task EnsureWorkCalendarTableAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS public.attendance_work_calendar_days (
                "Id" uuid NOT NULL,
                "WorkDate" date NOT NULL,
                "DayType" smallint NOT NULL,
                "Name" character varying(200) NULL,
                "Note" character varying(500) NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_attendance_work_calendar_days" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_attendance_work_calendar_days_WorkDate"
            ON public.attendance_work_calendar_days ("WorkDate");

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                        AND table_name = 'attendance_work_calendar_days'
                        AND column_name = 'DayType'
                        AND data_type <> 'smallint'
                ) THEN
                    ALTER TABLE public.attendance_work_calendar_days
                    DROP CONSTRAINT IF EXISTS "CK_attendance_work_calendar_days_DayType";

                    ALTER TABLE public.attendance_work_calendar_days
                    ALTER COLUMN "DayType" TYPE smallint
                    USING CASE
                        WHEN "DayType" IN ('1', 'DayOff', 'day_off', 'dayoff', 'off', 'rest_day', 'Ngày nghỉ') THEN 1
                        WHEN "DayType" IN ('2', 'Holiday', 'holiday', 'Ngày lễ') THEN 2
                        ELSE 1
                    END;
                END IF;

                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'CK_attendance_work_calendar_days_DayType'
                ) THEN
                    ALTER TABLE public.attendance_work_calendar_days
                    ADD CONSTRAINT "CK_attendance_work_calendar_days_DayType"
                    CHECK ("DayType" IN (1, 2));
                END IF;
            END $$;
            """,
            cancellationToken);
    }
}
