using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.NhanSu.PhongBan;

public sealed class DatabaseAttendanceDepartmentService(ApplicationDbContext dbContext)
    : IAttendanceDepartmentService
{
    public async Task<IReadOnlyList<AttendanceDepartmentDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        await EnsureEmployeeSoftDeleteColumnsAsync(cancellationToken);

        var rows = await dbContext.Departments
            .AsNoTracking()
            .OrderBy(x => x.CenterName)
            .ThenBy(x => x.DepartmentOrWorkshopName)
            .ThenBy(x => x.TeamName)
            .ThenBy(x => x.GroupName)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);

        var employeeCounts = await dbContext.Employees
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .GroupBy(x => x.DepartmentId)
            .Select(x => new
            {
                DepartmentId = x.Key,
                Count = x.Count()
            })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count, cancellationToken);

        return rows
            .Select(row => MapToDto(row, employeeCounts.GetValueOrDefault(row.Id)))
            .ToArray();
    }

    public async Task<string?> ValidateAsync(
        UpsertAttendanceDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        return await ValidateAsync(dbContext, request, cancellationToken);
    }

    private static async Task<string?> ValidateAsync(
        ApplicationDbContext dbContext,
        UpsertAttendanceDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCode = Normalize(request.Code);
        var normalizedCenterName = Normalize(request.CenterName);
        var normalizedDepartmentName = Normalize(request.DepartmentOrWorkshopName);
        var normalizedTeamName = Normalize(request.TeamName);
        var normalizedGroupName = Normalize(request.GroupName);
        var normalizedNotes = Normalize(request.Notes);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return "Mã phòng ban không được để trống.";
        }

        if (normalizedCode.Length > 50)
        {
            return "Mã phòng ban không được vượt quá 50 ký tự.";
        }

        if (string.IsNullOrWhiteSpace(normalizedCenterName))
        {
            return "Trung tâm không được để trống.";
        }

        if (normalizedCenterName.Length > 200)
        {
            return "Trung tâm không được vượt quá 200 ký tự.";
        }

        if (string.IsNullOrWhiteSpace(normalizedDepartmentName))
        {
            return "Phòng ban/Xưởng không được để trống.";
        }

        if (normalizedDepartmentName.Length > 200)
        {
            return "Phòng ban/Xưởng không được vượt quá 200 ký tự.";
        }

        if (normalizedTeamName?.Length > 200)
        {
            return "Tổ không được vượt quá 200 ký tự.";
        }

        if (normalizedGroupName?.Length > 200)
        {
            return "Nhóm không được vượt quá 200 ký tự.";
        }

        if (normalizedNotes?.Length > 1000)
        {
            return "Ghi chú không được vượt quá 1000 ký tự.";
        }

        var duplicateCode = await dbContext.Departments
            .AsNoTracking()
            .AnyAsync(
                x => x.Id != request.Id
                    && x.Code.ToLower() == normalizedCode.ToLower(),
                cancellationToken);

        return duplicateCode
            ? "Mã phòng ban ZKTeco đã tồn tại. Hãy dùng mã khác."
            : null;
    }

    public async Task<AttendanceDepartmentDto> SaveAsync(
        UpsertAttendanceDepartmentRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = await ValidateAsync(dbContext, request, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            throw new InvalidOperationException(validationMessage);
        }

        var normalizedId = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        var utcNow = DateTime.UtcNow;

        AttendanceDepartmentRow row;
        if (isNew)
        {
            row = new AttendanceDepartmentRow
            {
                Id = normalizedId,
                CreatedAtUtc = request.CreatedAtUtc == default ? utcNow : request.CreatedAtUtc
            };

            dbContext.Departments.Add(row);
        }
        else
        {
            row = await dbContext.Departments.SingleOrDefaultAsync(x => x.Id == normalizedId, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy phòng ban ZKTeco để cập nhật.");

            if (row.CreatedAtUtc == default)
            {
                row.CreatedAtUtc = request.CreatedAtUtc == default ? utcNow : request.CreatedAtUtc;
            }
        }

        Apply(row, request, normalizedId);
        row.UpdatedAtUtc = request.UpdatedAtUtc ?? utcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureEmployeeSoftDeleteColumnsAsync(cancellationToken);
        var employeeCount = await dbContext.Employees
            .AsNoTracking()
            .CountAsync(x => x.DepartmentId == row.Id && !x.IsDeleted, cancellationToken);

        return MapToDto(row, employeeCount);
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

        var rows = await dbContext.Departments
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return;
        }

        dbContext.Departments.RemoveRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(
        AttendanceDepartmentRow row,
        UpsertAttendanceDepartmentRequest request,
        Guid id)
    {
        row.Id = id;
        row.Code = Normalize(request.Code) ?? string.Empty;
        row.CenterName = Normalize(request.CenterName) ?? string.Empty;
        row.DepartmentOrWorkshopName = Normalize(request.DepartmentOrWorkshopName) ?? string.Empty;
        row.TeamName = Normalize(request.TeamName);
        row.GroupName = Normalize(request.GroupName);
        row.Notes = Normalize(request.Notes);
        row.Status = request.Status;
    }

    private static AttendanceDepartmentDto MapToDto(AttendanceDepartmentRow row, int employeeCount)
    {
        var name = BuildName(row);
        var fullPath = BuildFullPath(row);

        return new AttendanceDepartmentDto
        {
            Id = row.Id,
            Code = row.Code,
            CenterName = row.CenterName,
            DepartmentOrWorkshopName = row.DepartmentOrWorkshopName,
            TeamName = row.TeamName,
            GroupName = row.GroupName,
            Notes = row.Notes,
            Name = name,
            FullPath = fullPath,
            EmployeeCount = employeeCount,
            Status = row.Status,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc
        };
    }

    private static string BuildName(AttendanceDepartmentRow row) =>
        Normalize(row.GroupName)
        ?? Normalize(row.TeamName)
        ?? Normalize(row.DepartmentOrWorkshopName)
        ?? string.Empty;

    private static string BuildFullPath(AttendanceDepartmentRow row) =>
        string.Join(
            " / ",
            new[]
            {
                Normalize(row.CenterName),
                Normalize(row.DepartmentOrWorkshopName),
                Normalize(row.TeamName),
                Normalize(row.GroupName)
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task EnsureEmployeeSoftDeleteColumnsAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE public.employees
            ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT FALSE;

            ALTER TABLE public.employees
            ADD COLUMN IF NOT EXISTS "DeletedAtUtc" timestamp without time zone NULL;

            CREATE INDEX IF NOT EXISTS "IX_employees_IsDeleted"
            ON public.employees ("IsDeleted");
            """,
            cancellationToken);
    }
}
