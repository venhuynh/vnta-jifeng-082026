using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.TinhLuong.LuongCanBan;

public sealed class DatabaseBasicSalaryService(ApplicationDbContext dbContext)
    : IBasicSalaryService
{
    private const int PreviousMonthSyncTargetMonth = 7;
    private const int PreviousMonthSyncTargetYear = 2026;
    private const int DefaultSearchTake = 2000;

    public async Task<IReadOnlyList<BasicSalaryListItemDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await SearchAsync(new BasicSalaryFilter(null, DefaultSearchTake), cancellationToken);
    }

    public async Task<BasicSalaryListItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (id == Guid.Empty)
        {
            return null;
        }

        await EnsureSchemaAsync(cancellationToken);

        var row = await (
            from salary in dbContext.BasicSalaryRecords.AsNoTracking()
            where salary.Id == id
            join employee in dbContext.Employees.AsNoTracking()
                on salary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new
            {
                Salary = salary,
                Employee = employee,
                Department = department,
                Position = position
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : MapToDto(row.Salary, row.Employee, row.Department, row.Position);
    }

    public async Task<IReadOnlyList<BasicSalaryListItemDto>> SearchAsync(
        BasicSalaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(cancellationToken);

        var query =
            from salary in dbContext.BasicSalaryRecords.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on salary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new
            {
                Salary = salary,
                Employee = employee,
                Department = department,
                Position = position
            };

        var normalizedFilter = NormalizeFilter(filter);
        if (!string.IsNullOrWhiteSpace(normalizedFilter.SearchText))
        {
            var searchText = normalizedFilter.SearchText;
            var searchPattern = $"%{searchText}%";
            var hasNumericSearch = int.TryParse(searchText, out var numericSearchValue);
            var hasPeriodSearch = TryParsePayrollPeriod(searchText, out var periodMonth, out var periodYear);

            query = query.Where(row =>
                (row.Employee != null && (
                    EF.Functions.ILike(row.Employee.EmployeeCode ?? string.Empty, searchPattern)
                    || EF.Functions.ILike(
                        (row.Employee.LastName ?? string.Empty) + " " + (row.Employee.FirstName ?? string.Empty),
                        searchPattern)
                    || EF.Functions.ILike(
                        (row.Employee.FirstName ?? string.Empty) + " " + (row.Employee.LastName ?? string.Empty),
                        searchPattern)))
                || (row.Department != null && (
                    EF.Functions.ILike(row.Department.CenterName ?? string.Empty, searchPattern)
                    || EF.Functions.ILike(row.Department.DepartmentOrWorkshopName ?? string.Empty, searchPattern)
                    || EF.Functions.ILike(row.Department.TeamName ?? string.Empty, searchPattern)
                    || EF.Functions.ILike(row.Department.GroupName ?? string.Empty, searchPattern)))
                || (row.Position != null && EF.Functions.ILike(row.Position.Name ?? string.Empty, searchPattern))
                || (hasNumericSearch
                    && (row.Salary.PayrollMonth == numericSearchValue || row.Salary.PayrollYear == numericSearchValue))
                || (hasPeriodSearch
                    && row.Salary.PayrollMonth == periodMonth
                    && row.Salary.PayrollYear == periodYear));
        }

        var rows = await query
            .OrderByDescending(row => row.Salary.PayrollYear)
            .ThenByDescending(row => row.Salary.PayrollMonth)
            .ThenBy(row => row.Employee != null ? row.Employee.EmployeeCode : null)
            .ThenBy(row => row.Employee != null ? row.Employee.LastName : null)
            .ThenBy(row => row.Employee != null ? row.Employee.FirstName : null)
            .ThenBy(row => row.Salary.Id)
            .Take(normalizedFilter.Take)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => MapToDto(row.Salary, row.Employee, row.Department, row.Position))
            .ToArray();
    }

    public async Task<string?> ValidateAsync(
        UpsertBasicSalaryRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(cancellationToken);

        if (request.EmployeeId == Guid.Empty)
        {
            return "Nhân viên không được để trống.";
        }

        if (request.PayrollMonth is < 1 or > 12)
        {
            return "Tháng áp dụng phải nằm trong khoảng từ 1 đến 12.";
        }

        if (request.PayrollYear is < 1 or > 9999)
        {
            return "Năm áp dụng không hợp lệ.";
        }

        if (request.BasicSalary <= 0)
        {
            return "Lương căn bản phải lớn hơn 0.";
        }

        if (request.StandardWorkingDays <= 0)
        {
            return "Số ngày làm việc tiêu chuẩn phải lớn hơn 0.";
        }

        if (request.DailySalary < 0)
        {
            return "Lương ngày không được nhỏ hơn 0.";
        }

        if (request.HourlySalary < 0)
        {
            return "Lương giờ không được nhỏ hơn 0.";
        }

        var employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(
                employee => employee.Id == request.EmployeeId && !employee.IsDeleted,
                cancellationToken);
        if (!employeeExists)
        {
            return "Nhân viên đã chọn không hợp lệ.";
        }

        var normalizedId = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        var duplicateExists = await dbContext.BasicSalaryRecords
            .AsNoTracking()
            .AnyAsync(
                row => row.Id != normalizedId
                    && row.EmployeeId == request.EmployeeId
                    && row.PayrollMonth == request.PayrollMonth
                    && row.PayrollYear == request.PayrollYear,
                cancellationToken);

        return duplicateExists
            ? "Đã tồn tại lương căn bản cho nhân viên này trong kỳ lương đã chọn."
            : null;
    }

    public async Task<SyncBasicSalaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncBasicSalaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSchemaAsync(cancellationToken);
        ValidateSyncFromPreviousMonthRequest(request);

        var (sourceMonth, sourceYear) = GetPreviousPayrollPeriod(request.TargetMonth, request.TargetYear);
        var synchronizedAtUtc = ToDatabaseTimestamp(DateTime.UtcNow);

        var sourceRows = await (
            from salary in dbContext.BasicSalaryRecords.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on salary.EmployeeId equals employee.Id
            where !employee.IsDeleted
                && salary.PayrollMonth == sourceMonth
                && salary.PayrollYear == sourceYear
            orderby salary.EmployeeId
            select new SourceBasicSalarySnapshot(
                salary.EmployeeId,
                salary.BasicSalary,
                salary.StandardWorkingDays,
                salary.DailySalary,
                salary.HourlySalary))
            .ToListAsync(cancellationToken);

        if (sourceRows.Count == 0)
        {
            return new SyncBasicSalaryFromPreviousMonthResult(
                sourceMonth,
                sourceYear,
                request.TargetMonth,
                request.TargetYear,
                SourceRecordCount: 0,
                CreatedRecordCount: 0,
                UpdatedRecordCount: 0,
                UnchangedRecordCount: 0,
                SynchronizedAtUtc: synchronizedAtUtc);
        }

        var sourceEmployeeIds = sourceRows
            .Select(row => row.EmployeeId)
            .Distinct()
            .ToArray();

        var targetRows = await dbContext.BasicSalaryRecords
            .Where(
                row => row.PayrollMonth == request.TargetMonth
                    && row.PayrollYear == request.TargetYear
                    && sourceEmployeeIds.Contains(row.EmployeeId))
            .ToDictionaryAsync(row => row.EmployeeId, cancellationToken);

        var createdRecordCount = 0;
        var updatedRecordCount = 0;
        var unchangedRecordCount = 0;

        foreach (var sourceRow in sourceRows)
        {
            if (targetRows.TryGetValue(sourceRow.EmployeeId, out var targetRow))
            {
                if (HasSameCompensation(targetRow, sourceRow))
                {
                    unchangedRecordCount++;
                    continue;
                }

                targetRow.BasicSalary = sourceRow.BasicSalary;
                targetRow.StandardWorkingDays = sourceRow.StandardWorkingDays;
                targetRow.DailySalary = sourceRow.DailySalary;
                targetRow.HourlySalary = sourceRow.HourlySalary;
                targetRow.UpdatedAtUtc = synchronizedAtUtc;
                updatedRecordCount++;
                continue;
            }

            var newRow = new BasicSalaryRecordRow
            {
                Id = Guid.NewGuid(),
                EmployeeId = sourceRow.EmployeeId,
                PayrollMonth = request.TargetMonth,
                PayrollYear = request.TargetYear,
                BasicSalary = sourceRow.BasicSalary,
                StandardWorkingDays = sourceRow.StandardWorkingDays,
                DailySalary = sourceRow.DailySalary,
                HourlySalary = sourceRow.HourlySalary,
                CreatedAtUtc = synchronizedAtUtc,
                UpdatedAtUtc = synchronizedAtUtc
            };

            dbContext.BasicSalaryRecords.Add(newRow);
            targetRows[sourceRow.EmployeeId] = newRow;
            createdRecordCount++;
        }

        if (createdRecordCount > 0 || updatedRecordCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SyncBasicSalaryFromPreviousMonthResult(
            sourceMonth,
            sourceYear,
            request.TargetMonth,
            request.TargetYear,
            SourceRecordCount: sourceRows.Count,
            CreatedRecordCount: createdRecordCount,
            UpdatedRecordCount: updatedRecordCount,
            UnchangedRecordCount: unchangedRecordCount,
            SynchronizedAtUtc: synchronizedAtUtc);
    }

    public async Task<BasicSalaryListItemDto> SaveAsync(
        UpsertBasicSalaryRecordRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeRequest(request);
        var validationMessage = await ValidateAsync(normalizedRequest, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            throw new InvalidOperationException(validationMessage);
        }

        await EnsureSchemaAsync(cancellationToken);

        var normalizedId = normalizedRequest.Id == Guid.Empty ? Guid.NewGuid() : normalizedRequest.Id;
        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        var normalizedCreatedAt = normalizedRequest.CreatedAtUtc == default
            ? now
            : ToDatabaseTimestamp(normalizedRequest.CreatedAtUtc);

        BasicSalaryRecordRow row;
        if (isNew)
        {
            row = new BasicSalaryRecordRow
            {
                Id = normalizedId,
                CreatedAtUtc = normalizedCreatedAt
            };

            dbContext.BasicSalaryRecords.Add(row);
        }
        else
        {
            row = await dbContext.BasicSalaryRecords.SingleOrDefaultAsync(
                      item => item.Id == normalizedId,
                      cancellationToken)
                  ?? throw new InvalidOperationException("Không tìm thấy bản ghi lương căn bản để cập nhật.");

            if (row.CreatedAtUtc == default)
            {
                row.CreatedAtUtc = normalizedCreatedAt;
            }
        }

        row.Id = normalizedId;
        row.EmployeeId = normalizedRequest.EmployeeId;
        row.PayrollMonth = normalizedRequest.PayrollMonth;
        row.PayrollYear = normalizedRequest.PayrollYear;
        row.BasicSalary = normalizedRequest.BasicSalary;
        row.StandardWorkingDays = normalizedRequest.StandardWorkingDays;
        row.DailySalary = normalizedRequest.DailySalary;
        row.HourlySalary = normalizedRequest.HourlySalary;
        row.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(row.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại bản ghi lương căn bản vừa lưu.");
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

        await EnsureSchemaAsync(cancellationToken);

        var normalizedIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return;
        }

        var rows = await dbContext.BasicSalaryRecords
            .Where(item => normalizedIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        dbContext.BasicSalaryRecords.RemoveRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static BasicSalaryListItemDto MapToDto(
        BasicSalaryRecordRow salary,
        AttendanceGatewayEmployeeRow? employee,
        AttendanceDepartmentRow? department,
        AttendanceGatewayPositionRow? position) =>
        new(
            salary.Id,
            salary.EmployeeId,
            Normalize(employee?.EmployeeCode) ?? string.Empty,
            BuildEmployeeName(employee),
            department is null ? null : BuildDepartmentName(department),
            department is null ? null : BuildDepartmentPath(department),
            Normalize(position?.Name),
            salary.PayrollMonth,
            salary.PayrollYear,
            salary.BasicSalary,
            salary.StandardWorkingDays,
            salary.DailySalary,
            salary.HourlySalary,
            salary.CreatedAtUtc,
            salary.UpdatedAtUtc);

    private static string BuildEmployeeName(AttendanceGatewayEmployeeRow? employee)
    {
        if (employee is null)
        {
            return string.Empty;
        }

        return string.Join(
            " ",
            new[] { Normalize(employee.LastName), Normalize(employee.FirstName) }
                .Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildDepartmentName(AttendanceDepartmentRow department) =>
        Normalize(department.GroupName)
        ?? Normalize(department.TeamName)
        ?? Normalize(department.DepartmentOrWorkshopName)
        ?? string.Empty;

    private static string BuildDepartmentPath(AttendanceDepartmentRow department) =>
        string.Join(
            " / ",
            new[]
            {
                Normalize(department.CenterName),
                Normalize(department.DepartmentOrWorkshopName),
                Normalize(department.TeamName),
                Normalize(department.GroupName)
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

    private static UpsertBasicSalaryRecordRequest NormalizeRequest(UpsertBasicSalaryRecordRequest request)
    {
        var normalizedDailySalary = request.DailySalary > 0
            ? request.DailySalary
            : CalculateDailySalary(request.BasicSalary, request.StandardWorkingDays);
        var normalizedHourlySalary = request.HourlySalary > 0
            ? request.HourlySalary
            : CalculateHourlySalary(normalizedDailySalary);

        return new UpsertBasicSalaryRecordRequest
        {
            Id = request.Id,
            EmployeeId = request.EmployeeId,
            PayrollMonth = request.PayrollMonth,
            PayrollYear = request.PayrollYear,
            BasicSalary = request.BasicSalary,
            StandardWorkingDays = request.StandardWorkingDays,
            DailySalary = normalizedDailySalary,
            HourlySalary = normalizedHourlySalary,
            CreatedAtUtc = request.CreatedAtUtc,
            UpdatedAtUtc = request.UpdatedAtUtc
        };
    }

    private static decimal CalculateDailySalary(decimal basicSalary, decimal standardWorkingDays)
    {
        if (basicSalary <= 0 || standardWorkingDays <= 0)
        {
            return 0;
        }

        return decimal.Round(basicSalary / standardWorkingDays, 4, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateHourlySalary(decimal dailySalary)
    {
        if (dailySalary <= 0)
        {
            return 0;
        }

        return decimal.Round(dailySalary / 8m, 4, MidpointRounding.AwayFromZero);
    }

    private static void ValidateSyncFromPreviousMonthRequest(SyncBasicSalaryFromPreviousMonthRequest request)
    {
        if (request.TargetMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng lấy dữ liệu lương căn bản không hợp lệ.");
        }

        if (request.TargetYear is < 1 or > 9999)
        {
            throw new InvalidOperationException("Năm lấy dữ liệu lương căn bản không hợp lệ.");
        }

        if (request.TargetMonth != PreviousMonthSyncTargetMonth || request.TargetYear != PreviousMonthSyncTargetYear)
        {
            throw new InvalidOperationException(
                "Đợt triển khai này chỉ cho phép lấy dữ liệu lương căn bản từ tháng trước cho mốc tháng 07/2026.");
        }
    }

    private static (int Month, int Year) GetPreviousPayrollPeriod(int month, int year)
    {
        var targetPeriod = new DateOnly(year, month, 1);
        var previousPeriod = targetPeriod.AddMonths(-1);
        return (previousPeriod.Month, previousPeriod.Year);
    }

    private static bool HasSameCompensation(BasicSalaryRecordRow targetRow, SourceBasicSalarySnapshot sourceRow) =>
        targetRow.BasicSalary == sourceRow.BasicSalary
        && targetRow.StandardWorkingDays == sourceRow.StandardWorkingDays
        && targetRow.DailySalary == sourceRow.DailySalary
        && targetRow.HourlySalary == sourceRow.HourlySalary;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BasicSalaryFilter NormalizeFilter(BasicSalaryFilter filter)
    {
        var searchText = string.IsNullOrWhiteSpace(filter.SearchText)
            ? null
            : filter.SearchText.Trim();
        var take = filter.Take <= 0 ? DefaultSearchTake : Math.Min(filter.Take, 5000);

        return filter with
        {
            SearchText = searchText,
            Take = take
        };
    }

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private static bool TryParsePayrollPeriod(string value, out int month, out int year)
    {
        month = 0;
        year = 0;

        var segments = value
            .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(segments[0], out month) || !int.TryParse(segments[1], out year))
        {
            return false;
        }

        return month is >= 1 and <= 12 && year is >= 1 and <= 9999;
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE public.employees
            ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT FALSE;

            ALTER TABLE public.employees
            ADD COLUMN IF NOT EXISTS "DeletedAtUtc" timestamp without time zone NULL;

            CREATE INDEX IF NOT EXISTS "IX_employees_IsDeleted"
            ON public.employees ("IsDeleted");

            CREATE TABLE IF NOT EXISTS public.payroll_basic_salary_records (
                "Id" uuid NOT NULL,
                "EmployeeId" uuid NOT NULL,
                "PayrollMonth" integer NOT NULL,
                "PayrollYear" integer NOT NULL,
                "BasicSalary" numeric(18,2) NOT NULL,
                "StandardWorkingDays" numeric(5,2) NOT NULL,
                "DailySalary" numeric(18,4) NOT NULL,
                "HourlySalary" numeric(18,4) NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_payroll_basic_salary_records" PRIMARY KEY ("Id")
            );

            ALTER TABLE public.payroll_basic_salary_records
            ADD COLUMN IF NOT EXISTS "EmployeeId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

            ALTER TABLE public.payroll_basic_salary_records
            ADD COLUMN IF NOT EXISTS "PayrollMonth" integer NOT NULL DEFAULT 1;

            ALTER TABLE public.payroll_basic_salary_records
            ADD COLUMN IF NOT EXISTS "PayrollYear" integer NOT NULL DEFAULT 2000;

            ALTER TABLE public.payroll_basic_salary_records
            ADD COLUMN IF NOT EXISTS "BasicSalary" numeric(18,2) NOT NULL DEFAULT 0;

            ALTER TABLE public.payroll_basic_salary_records
            ADD COLUMN IF NOT EXISTS "StandardWorkingDays" numeric(5,2) NOT NULL DEFAULT 0;

            ALTER TABLE public.payroll_basic_salary_records
            ADD COLUMN IF NOT EXISTS "DailySalary" numeric(18,4) NOT NULL DEFAULT 0;

            ALTER TABLE public.payroll_basic_salary_records
            ADD COLUMN IF NOT EXISTS "HourlySalary" numeric(18,4) NOT NULL DEFAULT 0;

            ALTER TABLE public.payroll_basic_salary_records
            ADD COLUMN IF NOT EXISTS "CreatedAtUtc" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;

            ALTER TABLE public.payroll_basic_salary_records
            ADD COLUMN IF NOT EXISTS "UpdatedAtUtc" timestamp without time zone NULL;

            CREATE INDEX IF NOT EXISTS "IX_payroll_basic_salary_records_EmployeeId"
            ON public.payroll_basic_salary_records ("EmployeeId");

            CREATE INDEX IF NOT EXISTS "IX_payroll_basic_salary_records_PayrollYear_PayrollMonth"
            ON public.payroll_basic_salary_records ("PayrollYear", "PayrollMonth");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_payroll_basic_salary_records_EmployeeId_PayrollYear_PayrollMonth"
            ON public.payroll_basic_salary_records ("EmployeeId", "PayrollYear", "PayrollMonth");

            DO $$
            BEGIN
                IF to_regclass('public.employees') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_payroll_basic_salary_records_employees_EmployeeId'
                    )
                THEN
                    ALTER TABLE public.payroll_basic_salary_records
                    ADD CONSTRAINT "FK_payroll_basic_salary_records_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id")
                    ON DELETE RESTRICT;
                END IF;
            END $$;
            """,
            cancellationToken);
    }

    private sealed record SourceBasicSalarySnapshot(
        Guid EmployeeId,
        decimal BasicSalary,
        decimal StandardWorkingDays,
        decimal DailySalary,
        decimal HourlySalary);

}
