using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.NhanSu.ChucVu;

public sealed class DatabaseAttendancePositionService(ApplicationDbContext dbContext)
    : IAttendancePositionService
{
    private const int ActiveEmployeeStatus = 2;

    public async Task<IReadOnlyList<AttendancePositionListItemDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureEmployeeMetadataColumnsAsync(cancellationToken);

        var positions = await dbContext.Positions
            .AsNoTracking()
            .OrderBy(position => position.Code)
            .ThenBy(position => position.Name)
            .ToListAsync(cancellationToken);

        return positions
            .Select(MapToDto)
            .ToArray();
    }

    public async Task<string?> ValidateAsync(
        UpsertAttendancePositionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCode = Normalize(request.Code);
        var normalizedName = Normalize(request.Name);
        var normalizedDescription = Normalize(request.Description);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return "Mã chức vụ không được để trống.";
        }

        if (normalizedCode.Length > 50)
        {
            return "Mã chức vụ không được vượt quá 50 ký tự.";
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return "Tên chức vụ không được để trống.";
        }

        if (normalizedName.Length > 200)
        {
            return "Tên chức vụ không được vượt quá 200 ký tự.";
        }

        if (request.Description is not null
            && string.IsNullOrWhiteSpace(normalizedDescription)
            && request.Description.Length > 0)
        {
            return "Mô tả không được chỉ gồm khoảng trắng.";
        }

        if (normalizedDescription?.Length > 1000)
        {
            return "Mô tả không được vượt quá 1000 ký tự.";
        }

        var existingRows = await dbContext.Positions
            .AsNoTracking()
            .Select(position => new ExistingPositionSnapshot
            {
                Id = position.Id,
                Code = position.Code
            })
            .ToListAsync(cancellationToken);

        var duplicateCode = existingRows.Any(existing =>
            existing.Id != request.Id
            && string.Equals(existing.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));

        if (duplicateCode)
        {
            return "Mã chức vụ đã tồn tại. Hãy dùng mã khác.";
        }

        return null;
    }

    public async Task<AttendancePositionListItemDto> SaveAsync(
        UpsertAttendancePositionRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = await ValidateAsync(request, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            throw new InvalidOperationException(validationMessage);
        }

        await EnsureEmployeeMetadataColumnsAsync(cancellationToken);

        var normalizedId = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        var normalizedCreatedAt = request.CreatedAtUtc == default
            ? DateTime.UtcNow
            : request.CreatedAtUtc;
        var normalizedUpdatedAt = request.UpdatedAtUtc ?? DateTime.UtcNow;

        AttendanceGatewayPositionRow row;
        if (isNew)
        {
            row = new AttendanceGatewayPositionRow
            {
                Id = normalizedId,
                EmployeeCount = 0,
                CreatedAtUtc = ToDatabaseTimestamp(normalizedCreatedAt)
            };

            dbContext.Positions.Add(row);
        }
        else
        {
            row = await dbContext.Positions.SingleOrDefaultAsync(position => position.Id == normalizedId, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy chức vụ để cập nhật.");

            if (row.CreatedAtUtc == default)
            {
                row.CreatedAtUtc = ToDatabaseTimestamp(normalizedCreatedAt);
            }
        }

        Apply(row, request, normalizedId);
        row.UpdatedAtUtc = ToDatabaseTimestamp(normalizedUpdatedAt);

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(row);
    }

    public async Task RefreshEmployeeCountsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureEmployeeMetadataColumnsAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            UPDATE public.positions
            SET "EmployeeCount" = 0
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE public.positions AS p
            SET "EmployeeCount" = counts."EmployeeCount"
            FROM (
                SELECT e."PositionId", COUNT(*)::integer AS "EmployeeCount"
                FROM public.employees AS e
                WHERE e."Status" = {ActiveEmployeeStatus}
                  AND e."IsDeleted" = FALSE
                GROUP BY e."PositionId"
            ) AS counts
            WHERE p."Id" = counts."PositionId"
            """,
            cancellationToken);
    }

    private static void Apply(
        AttendanceGatewayPositionRow row,
        UpsertAttendancePositionRequest request,
        Guid id)
    {
        row.Id = id;
        row.Code = Normalize(request.Code) ?? string.Empty;
        row.Name = Normalize(request.Name) ?? string.Empty;
        row.Description = Normalize(request.Description);
        row.Status = request.Status;
    }

    private static AttendancePositionListItemDto MapToDto(AttendanceGatewayPositionRow row) =>
        new(
            row.Id,
            row.Code,
            row.Name,
            row.Description,
            row.Status,
            row.EmployeeCount,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? ToDatabaseTimestamp(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return ToDatabaseTimestamp(value.Value);
    }

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private async Task EnsureEmployeeMetadataColumnsAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE public.positions
            ADD COLUMN IF NOT EXISTS "EmployeeCount" integer NOT NULL DEFAULT 0;

            ALTER TABLE public.employees
            ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT FALSE;

            ALTER TABLE public.employees
            ADD COLUMN IF NOT EXISTS "DeletedAtUtc" timestamp without time zone NULL;

            CREATE INDEX IF NOT EXISTS "IX_employees_IsDeleted"
            ON public.employees ("IsDeleted");
            """,
            cancellationToken);
    }

    private sealed class ExistingPositionSnapshot
    {
        public Guid Id { get; init; }

        public string? Code { get; init; }
    }
}
