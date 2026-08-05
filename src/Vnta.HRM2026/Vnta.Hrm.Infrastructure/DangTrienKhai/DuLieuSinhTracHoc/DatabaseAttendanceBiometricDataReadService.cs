using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vnta.Hrm.Application.Common;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.DangTrienKhai.DuLieuSinhTracHoc;

public sealed class DatabaseAttendanceBiometricDataReadService(
    ApplicationDbContext dbContext,
    ILogger<DatabaseAttendanceBiometricDataReadService> logger)
    : IAttendanceBiometricDataReadService
{
    private const int MaxSearchResultLimit = 5000;

    public async Task<IReadOnlyList<AttendanceBiometricDataListItemDto>> SearchAsync(
        AttendanceBiometricDataFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureBiometricDataTableAsync(cancellationToken);

        var normalizedTake = Math.Clamp(filter.Take, 1, MaxSearchResultLimit);

        try
        {
            var rows = await BuildFilteredQuery(
                    filter.SearchText,
                    filter.HasFaceData,
                    filter.FingerprintQuantity)
                .OrderByDescending(x => x.LastUpdated)
                .ThenBy(x => x.EmployeeCode)
                .ThenByDescending(x => x.HasFaceData)
                .ThenByDescending(x => x.FpQty)
                .ThenByDescending(x => x.Id)
                .Take(normalizedTake)
                .ToListAsync(cancellationToken);

            return rows.Select(MapListItem).ToList();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            logger.LogWarning(
                ex,
                "biometric_data is not available in the runtime database yet. Returning an empty list for /attendance/biometric-data/search.");
            return [];
        }
    }

    private async Task EnsureBiometricDataTableAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            create table if not exists "biometric_data" (
                "Id" uuid not null,
                "EmployeeId" uuid not null,
                "FpQty" integer not null default 0,
                "HasFaceData" boolean not null default false,
                "LastUpdated" timestamp without time zone not null,
                "CardNumber" character varying(255),
                "IsAdmin" boolean not null default false,
                "Password" character varying(255),
                constraint "PK_biometric_data" primary key ("Id"),
                constraint "FK_biometric_data_employees_EmployeeId"
                    foreign key ("EmployeeId")
                    references "employees" ("Id")
                    on delete restrict
            );

            create index if not exists "IX_biometric_data_EmployeeId"
                on "biometric_data" ("EmployeeId");

            create index if not exists "IX_biometric_data_LastUpdated"
                on "biometric_data" ("LastUpdated");
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private IQueryable<AttendanceBiometricDataQueryRow> BuildFilteredQuery(
        string? searchText,
        bool? hasFaceData,
        int? fingerprintQuantity)
    {
        var normalizedSearchTerm = NormalizeOptional(searchText);

        var query =
            from biometric in dbContext.BiometricData.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on biometric.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new AttendanceBiometricDataQueryRow
            {
                Id = biometric.Id,
                EmployeeId = biometric.EmployeeId,
                EmployeeCode = employee == null ? null : employee.EmployeeCode,
                EmployeeFirstName = employee == null ? null : employee.FirstName,
                EmployeeLastName = employee == null ? null : employee.LastName,
                Avatar = employee == null ? null : employee.Avatar,
                DepartmentName = department == null
                    ? null
                    : department.DepartmentOrWorkshopName
                        ?? department.TeamName
                        ?? department.GroupName
                        ?? department.CenterName,
                PositionName = position == null ? null : position.Name,
                FpQty = biometric.FpQty,
                HasFaceData = biometric.HasFaceData,
                LastUpdated = biometric.LastUpdated,
                CardNumber = biometric.CardNumber,
                IsAdmin = biometric.IsAdmin,
                Password = biometric.Password
            };

        if (hasFaceData.HasValue)
        {
            query = query.Where(x => x.HasFaceData == hasFaceData.Value);
        }

        if (fingerprintQuantity.HasValue)
        {
            query = query.Where(x => x.FpQty == fingerprintQuantity.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            var searchPattern = $"%{normalizedSearchTerm}%";
            query = query.Where(x =>
                (x.CardNumber != null && EF.Functions.ILike(x.CardNumber, searchPattern))
                || (x.EmployeeCode != null && EF.Functions.ILike(x.EmployeeCode, searchPattern))
                || (x.EmployeeFirstName != null && EF.Functions.ILike(x.EmployeeFirstName, searchPattern))
                || (x.EmployeeLastName != null && EF.Functions.ILike(x.EmployeeLastName, searchPattern))
                || (x.DepartmentName != null && EF.Functions.ILike(x.DepartmentName, searchPattern))
                || (x.PositionName != null && EF.Functions.ILike(x.PositionName, searchPattern)));
        }

        return query;
    }

    private static AttendanceBiometricDataListItemDto MapListItem(AttendanceBiometricDataQueryRow row)
    {
        return new AttendanceBiometricDataListItemDto(
            row.Id,
            row.EmployeeId,
            NormalizeOptional(row.EmployeeCode),
            BuildEmployeeName(row.EmployeeLastName, row.EmployeeFirstName),
            AvatarImageSourceHelper.NormalizeSource(row.Avatar),
            NormalizeOptional(row.DepartmentName),
            NormalizeOptional(row.PositionName),
            row.FpQty,
            row.HasFaceData,
            row.LastUpdated,
            NormalizeOptional(row.CardNumber),
            row.IsAdmin,
            !string.IsNullOrWhiteSpace(row.Password));
    }

    private static string? BuildEmployeeName(string? lastName, string? firstName)
    {
        var parts = new[] { lastName, firstName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part!.Trim())
            .ToArray();

        return parts.Length == 0 ? null : string.Join(" ", parts);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed class AttendanceBiometricDataQueryRow
    {
        public Guid Id { get; init; }
        public Guid EmployeeId { get; init; }
        public string? EmployeeCode { get; init; }
        public string? EmployeeFirstName { get; init; }
        public string? EmployeeLastName { get; init; }
        public string? Avatar { get; init; }
        public string? DepartmentName { get; init; }
        public string? PositionName { get; init; }
        public int FpQty { get; init; }
        public bool HasFaceData { get; init; }
        public DateTime LastUpdated { get; init; }
        public string? CardNumber { get; init; }
        public bool IsAdmin { get; init; }
        public string? Password { get; init; }
    }
}
