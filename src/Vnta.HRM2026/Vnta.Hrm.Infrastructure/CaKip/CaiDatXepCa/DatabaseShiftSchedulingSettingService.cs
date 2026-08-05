using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.CaKip.CaiDatXepCa;

public sealed class DatabaseShiftSchedulingSettingService(ApplicationDbContext dbContext)
    : IShiftSchedulingSettingService
{
    private const int MaxValueLength = 500;

    public async Task<IReadOnlyList<ShiftSchedulingSettingListItemDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureShiftSchedulingSettingsTableAsync(cancellationToken);

        var rows = await dbContext.ShiftSchedulingSettings
            .AsNoTracking()
            .OrderBy(row => row.ClassificationType)
            .ThenBy(row => row.ShiftId)
            .ThenBy(row => row.Value)
            .ThenBy(row => row.AssignmentScopeMode)
            .ThenBy(row => row.EffectiveFromDate)
            .ThenBy(row => row.EffectiveToDate)
            .ThenByDescending(row => row.IsActive)
            .ThenBy(row => row.Id)
            .ToListAsync(cancellationToken);

        var shiftIds = rows
            .Where(row => row.ShiftId.HasValue)
            .Select(row => row.ShiftId!.Value)
            .Distinct()
            .ToArray();

        var shifts = shiftIds.Length == 0
            ? new Dictionary<Guid, AttendanceShiftRow>()
            : await dbContext.Shifts
                .AsNoTracking()
                .Where(shift => shiftIds.Contains(shift.Id))
                .ToDictionaryAsync(shift => shift.Id, cancellationToken);

        return rows
            .Select(row => MapToDto(
                row,
                row.ShiftId.HasValue && shifts.TryGetValue(row.ShiftId.Value, out var shift)
                    ? shift
                    : null))
            .OrderBy(row => row.ClassificationType)
            .ThenBy(row => row.ShiftName)
            .ThenBy(row => row.ShiftStartTime)
            .ThenBy(row => row.Value)
            .ThenBy(row => row.AssignmentScopeMode)
            .ThenBy(row => row.EffectiveFromDate)
            .ThenBy(row => row.EffectiveToDate)
            .ThenByDescending(row => row.IsActive)
            .ThenBy(row => row.Id)
            .ToArray();
    }

    public async Task<string?> ValidateAsync(
        UpsertShiftSchedulingSettingRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedValue = Normalize(request.Value);
        var (effectiveFromDate, effectiveToDate) = NormalizeDateRange(
            request.EffectiveFromDate,
            request.EffectiveToDate);

        if (!Enum.IsDefined(typeof(ShiftSchedulingClassification), request.ClassificationType))
        {
            return "Phân loại không hợp lệ.";
        }

        if (!Enum.IsDefined(typeof(ShiftSchedulingScopeMode), request.AssignmentScopeMode))
        {
            return "Hình thức áp dụng không hợp lệ.";
        }

        if (!request.ShiftId.HasValue || request.ShiftId.Value == Guid.Empty)
        {
            return "Ca làm việc không được để trống.";
        }

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return "Giá trị không được để trống.";
        }

        if (normalizedValue.Length > MaxValueLength)
        {
            return "Giá trị không được vượt quá 500 ký tự.";
        }

        if ((ShiftSchedulingScopeMode)request.AssignmentScopeMode == ShiftSchedulingScopeMode.TheoKhoangNgay
            && (!effectiveFromDate.HasValue || !effectiveToDate.HasValue))
        {
            return "Khoảng ngày áp dụng không được để trống.";
        }

        await EnsureShiftSchedulingSettingsTableAsync(cancellationToken);
        var shiftExists = await dbContext.Shifts
            .AsNoTracking()
            .AnyAsync(shift => shift.Id == request.ShiftId.Value, cancellationToken);

        return shiftExists
            ? null
            : "Ca làm việc không hợp lệ.";
    }

    public async Task<ShiftSchedulingSettingListItemDto> SaveAsync(
        UpsertShiftSchedulingSettingRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = await ValidateAsync(request, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            throw new InvalidOperationException(validationMessage);
        }

        await EnsureShiftSchedulingSettingsTableAsync(cancellationToken);

        var normalizedId = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        var normalizedValue = Normalize(request.Value) ?? string.Empty;
        var now = DateTime.UtcNow;
        var normalizedCreatedAt = request.CreatedAtUtc == default ? now : request.CreatedAtUtc;
        var (effectiveFromDate, effectiveToDate) = NormalizeDateRange(
            request.EffectiveFromDate,
            request.EffectiveToDate);

        ShiftSchedulingSettingRow row;
        if (isNew)
        {
            row = new ShiftSchedulingSettingRow
            {
                Id = normalizedId,
                CreatedAtUtc = ToDatabaseTimestamp(normalizedCreatedAt)
            };

            dbContext.ShiftSchedulingSettings.Add(row);
        }
        else
        {
            row = await dbContext.ShiftSchedulingSettings.SingleOrDefaultAsync(
                      item => item.Id == normalizedId,
                      cancellationToken)
                  ?? throw new InvalidOperationException("Không tìm thấy cấu hình xếp ca để cập nhật.");

            if (row.CreatedAtUtc == default)
            {
                row.CreatedAtUtc = ToDatabaseTimestamp(normalizedCreatedAt);
            }
        }

        row.Id = normalizedId;
        row.ShiftId = request.ShiftId!.Value;
        row.ClassificationType = request.ClassificationType;
        row.Value = normalizedValue;
        row.AssignmentScopeMode = request.AssignmentScopeMode;
        row.EffectiveFromDate = effectiveFromDate;
        row.EffectiveToDate = effectiveToDate;
        row.IsActive = request.IsActive;
        row.UpdatedAtUtc = ToDatabaseTimestamp(now);

        await dbContext.SaveChangesAsync(cancellationToken);

        var savedShift = await dbContext.Shifts
            .AsNoTracking()
            .SingleOrDefaultAsync(shift => shift.Id == row.ShiftId, cancellationToken);

        return MapToDto(row, savedShift);
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ids.Count == 0)
        {
            return;
        }

        await EnsureShiftSchedulingSettingsTableAsync(cancellationToken);

        var rows = await dbContext.ShiftSchedulingSettings
            .Where(item => ids.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return;
        }

        dbContext.ShiftSchedulingSettings.RemoveRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ShiftSchedulingSettingListItemDto MapToDto(
        ShiftSchedulingSettingRow row,
        AttendanceShiftRow? shift = null) =>
        new(
            row.Id,
            row.ShiftId,
            shift?.Code,
            shift?.Name,
            shift?.StartTime,
            shift?.EndTime,
            row.ClassificationType,
            row.Value ?? string.Empty,
            row.AssignmentScopeMode,
            row.EffectiveFromDate,
            row.EffectiveToDate,
            row.IsActive,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

    private enum ShiftSchedulingClassification
    {
        TheoPhongBan = 2,
        TheoNhanVien = 5
    }

    private enum ShiftSchedulingScopeMode
    {
        CoDinh = 1,
        TheoKhoangNgay = 2
    }

    private async Task EnsureShiftSchedulingSettingsTableAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS public.shift_scheduling_settings (
                "Id" uuid NOT NULL,
                "ShiftId" uuid NULL,
                "ClassificationType" integer NOT NULL,
                "Value" character varying(500) NULL,
                "AssignmentScopeMode" integer NOT NULL,
                "EffectiveFromDate" date NULL,
                "EffectiveToDate" date NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_shift_scheduling_settings" PRIMARY KEY ("Id")
            );

            ALTER TABLE public.shift_scheduling_settings
            ADD COLUMN IF NOT EXISTS "Value" character varying(500) NULL;

            ALTER TABLE public.shift_scheduling_settings
            ADD COLUMN IF NOT EXISTS "ShiftId" uuid NULL;

            ALTER TABLE public.shift_scheduling_settings
            ADD COLUMN IF NOT EXISTS "EffectiveFromDate" date NULL;

            ALTER TABLE public.shift_scheduling_settings
            ADD COLUMN IF NOT EXISTS "EffectiveToDate" date NULL;

            CREATE INDEX IF NOT EXISTS "IX_shift_scheduling_settings_ShiftId"
            ON public.shift_scheduling_settings ("ShiftId");

            DO $$
            BEGIN
                IF to_regclass('public.shifts') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_shift_scheduling_settings_shifts_ShiftId'
                    )
                THEN
                    ALTER TABLE public.shift_scheduling_settings
                    ADD CONSTRAINT "FK_shift_scheduling_settings_shifts_ShiftId"
                    FOREIGN KEY ("ShiftId") REFERENCES public.shifts ("Id")
                    ON DELETE RESTRICT;
                END IF;
            END $$;
            """,
            cancellationToken);
    }
}
