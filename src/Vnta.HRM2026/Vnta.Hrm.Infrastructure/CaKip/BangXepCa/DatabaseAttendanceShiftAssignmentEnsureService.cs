using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.CaKip.BangXepCa;

public sealed class DatabaseAttendanceShiftAssignmentEnsureService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : IAttendanceShiftAssignmentEnsureService
{
    private const int ResignedEmployeeStatus = 5;
    private const int ActiveShiftStatus = 1;
    private const int BlockSpecificity = 10;
    private const int DepartmentSpecificity = 20;
    private const int TeamSpecificity = 30;
    private const int GroupSpecificity = 40;
    private const int EmployeeSpecificity = 50;
    private const string AutoRuleCreationType = "AutoRule";

    private static readonly StringComparer TextComparer = StringComparer.OrdinalIgnoreCase;

    public async Task<AttendanceShiftAssignmentEnsureResult> EnsureFromSchedulingSettingsAsync(
        AttendanceShiftAssignmentEnsureRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (fromDate, toDate) = NormalizeDateRange(request.FromDate, request.ToDate);
        var dateCount = toDate.DayNumber - fromDate.DayNumber + 1;

        await EnsureRequiredTablesAsync(cancellationToken);

        if (auditScope.Current is { } command)
        {
            return await auditedMutation.ExecuteAsync(
                    command,
                    token => StageEnsureFromSchedulingSettingsAsync(
                        request,
                        fromDate,
                        toDate,
                        dateCount,
                        token),
                    CreateBatchGeneratedAuditEvent,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return await ExecuteWithoutAuditAsync(
                request,
                fromDate,
                toDate,
                dateCount,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<AttendanceShiftAssignmentEnsureResult> StageEnsureFromSchedulingSettingsAsync(
        AttendanceShiftAssignmentEnsureRequest request,
        DateOnly fromDate,
        DateOnly toDate,
        int dateCount,
        CancellationToken cancellationToken)
    {
        var settings = await dbContext.ShiftSchedulingSettings
            .AsNoTracking()
            .Where(setting => setting.IsActive)
            .OrderBy(setting => setting.ClassificationType)
            .ThenBy(setting => setting.Value)
            .ThenBy(setting => setting.UpdatedAtUtc ?? setting.CreatedAtUtc)
            .ThenBy(setting => setting.Id)
            .ToListAsync(cancellationToken);

        var employees = await LoadEligibleEmployeesAsync(cancellationToken);
        var issues = new List<AttendanceShiftAssignmentEnsureIssueDto>();
        var dayTypesByDate = await LoadWorkCalendarDayTypesAsync(
            fromDate,
            toDate,
            cancellationToken);
        var assignableDates = EnumerateDateRange(fromDate, toDate)
            .Where(date => !IsNonWorkingDate(dayTypesByDate, date))
            .ToArray();
        var skippedNonWorkingDateCount = dateCount - assignableDates.Length;

        if (employees.Count == 0)
        {
            return new AttendanceShiftAssignmentEnsureResult(
                fromDate,
                toDate,
                dateCount,
                0,
                0,
                0,
                0,
                0,
                [],
                skippedNonWorkingDateCount);
        }

        var employeeIds = employees
            .Select(employee => employee.Id)
            .ToArray();

        if (assignableDates.Length == 0)
        {
            var existingNonWorkingRows = await LoadExistingAssignmentRowsAsync(
                employeeIds,
                fromDate,
                toDate,
                cancellationToken);
            var existingNonWorkingRowsByKey = existingNonWorkingRows.ToDictionary(
                row => (row.EmployeeId, row.WorkDate),
                row => row);

            var cleanupResult = RemoveNonWorkingAutoRuleAssignments(
                existingNonWorkingRows,
                existingNonWorkingRowsByKey,
                dayTypesByDate);

            return new AttendanceShiftAssignmentEnsureResult(
                fromDate,
                toDate,
                dateCount,
                employees.Count,
                0,
                0,
                0,
                cleanupResult.ProtectedCount,
                [],
                skippedNonWorkingDateCount,
                cleanupResult.DeletedCount);
        }

        if (settings.Count == 0)
        {
            issues.Add(new AttendanceShiftAssignmentEnsureIssueDto(
                "NoActiveSchedulingSettings",
                "Chưa có cấu hình xếp ca đang hoạt động để đồng bộ."));

            return BuildIssueResult(
                fromDate,
                toDate,
                dateCount,
                employees.Count,
                issues,
                skippedNonWorkingDateCount);
        }

        var shiftIds = settings
            .Where(setting => setting.ShiftId.HasValue)
            .Select(setting => setting.ShiftId!.Value)
            .Distinct()
            .ToArray();
        var shifts = await dbContext.Shifts
            .AsNoTracking()
            .Where(shift => shiftIds.Contains(shift.Id) && shift.Status == ActiveShiftStatus)
            .ToDictionaryAsync(shift => shift.Id, cancellationToken);

        var rules = ResolveRules(settings, employees, shifts, issues);
        if (issues.Count > 0)
        {
            return BuildIssueResult(
                fromDate,
                toDate,
                dateCount,
                employees.Count,
                issues,
                skippedNonWorkingDateCount);
        }

        var ruleByEmployeeId = ResolveWinningRulesByEmployee(rules);
        var uncoveredEmployees = employees
            .Where(employee => !ruleByEmployeeId.ContainsKey(employee.Id))
            .OrderBy(employee => employee.EmployeeCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(employee => employee.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (uncoveredEmployees.Length > 0)
        {
            foreach (var employee in uncoveredEmployees)
            {
                issues.Add(new AttendanceShiftAssignmentEnsureIssueDto(
                    "MissingEmployeeShiftRule",
                    $"Nhân viên {employee.DisplayText} chưa có cấu hình xếp ca phù hợp.",
                    employee.Id));
            }

            return BuildIssueResult(
                fromDate,
                toDate,
                dateCount,
                employees.Count,
                issues,
                skippedNonWorkingDateCount);
        }

        var existingRows = await LoadExistingAssignmentRowsAsync(
            employeeIds,
            fromDate,
            toDate,
            cancellationToken);
        var existingRowsByKey = existingRows.ToDictionary(
            row => (row.EmployeeId, row.WorkDate),
            row => row);

        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        var insertedCount = 0;
        var updatedCount = 0;
        var unchangedCount = 0;
        var protectedCount = 0;
        var deletedNonWorkingAutoRuleCount = 0;

        var cleanup = RemoveNonWorkingAutoRuleAssignments(
            existingRows,
            existingRowsByKey,
            dayTypesByDate);
        protectedCount += cleanup.ProtectedCount;
        deletedNonWorkingAutoRuleCount += cleanup.DeletedCount;

        foreach (var workDate in assignableDates)
        {
            foreach (var employee in employees)
            {
                var rule = ruleByEmployeeId[employee.Id];
                if (!existingRowsByKey.TryGetValue((employee.Id, workDate), out var existingRow))
                {
                    var newRow = new AttendanceShiftAssignmentRow
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = employee.Id,
                        ShiftId = rule.ShiftId,
                        WorkDate = workDate,
                        CreationType = AutoRuleCreationType,
                        Notes = BuildSyncNote(request.Source, rule.SettingId),
                        CreatedAtUtc = now,
                        UpdatedAtUtc = null
                    };

                    dbContext.ShiftAssignments.Add(newRow);
                    existingRowsByKey[(employee.Id, workDate)] = newRow;
                    insertedCount++;
                    continue;
                }

                if (!string.Equals(existingRow.CreationType, AutoRuleCreationType, StringComparison.OrdinalIgnoreCase))
                {
                    protectedCount++;
                    continue;
                }

                if (existingRow.ShiftId == rule.ShiftId)
                {
                    unchangedCount++;
                    continue;
                }

                existingRow.ShiftId = rule.ShiftId;
                existingRow.Notes = BuildSyncNote(request.Source, rule.SettingId);
                existingRow.UpdatedAtUtc = now;
                updatedCount++;
            }
        }

        return new AttendanceShiftAssignmentEnsureResult(
            fromDate,
            toDate,
            dateCount,
            employees.Count,
            insertedCount,
            updatedCount,
            unchangedCount,
            protectedCount,
            [],
            skippedNonWorkingDateCount,
            deletedNonWorkingAutoRuleCount);
    }

    private static AttendanceShiftAssignmentEnsureResult BuildIssueResult(
        DateOnly fromDate,
        DateOnly toDate,
        int dateCount,
        int eligibleEmployeeCount,
        IReadOnlyList<AttendanceShiftAssignmentEnsureIssueDto> issues,
        int skippedNonWorkingDateCount) =>
        new(
            fromDate,
            toDate,
            dateCount,
            eligibleEmployeeCount,
            0,
            0,
            0,
            0,
            issues,
            skippedNonWorkingDateCount);

    private async Task<AttendanceShiftAssignmentEnsureResult> ExecuteWithoutAuditAsync(
        AttendanceShiftAssignmentEnsureRequest request,
        DateOnly fromDate,
        DateOnly toDate,
        int dateCount,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginTransactionIfNeededAsync(cancellationToken);
        var result = await StageEnsureFromSchedulingSettingsAsync(
                request,
                fromDate,
                toDate,
                dateCount,
                cancellationToken)
            .ConfigureAwait(false);

        if (!HasPersistedMutations(result))
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private static bool HasPersistedMutations(AttendanceShiftAssignmentEnsureResult result) =>
        result.InsertedCount > 0
        || result.UpdatedCount > 0
        || result.DeletedNonWorkingAutoRuleCount > 0;

    private static AuditOperationEvent CreateBatchGeneratedAuditEvent(
        AttendanceShiftAssignmentEnsureResult result)
    {
        var affectedCount = result.InsertedCount
            + result.UpdatedCount
            + result.DeletedNonWorkingAutoRuleCount;

        return new AuditOperationEvent(
            AuditActions.ShiftAssignment.BatchGenerated,
            AuditEntityTypes.ShiftAssignment,
            EntityId: $"{FormatDate(result.FromDate)}..{FormatDate(result.ToDate)}",
            EntityDisplayName: $"Shift assignments {FormatDate(result.FromDate)} to {FormatDate(result.ToDate)}",
            Outcome: affectedCount == 0
                ? AuditOperationOutcome.NoChanges
                : AuditOperationOutcome.Succeeded,
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["fromDate"] = FormatDate(result.FromDate),
                ["toDate"] = FormatDate(result.ToDate),
                ["dateCount"] = FormatNumber(result.DateCount),
                ["eligibleEmployeeCount"] = FormatNumber(result.EligibleEmployeeCount),
                ["insertedCount"] = FormatNumber(result.InsertedCount),
                ["updatedCount"] = FormatNumber(result.UpdatedCount),
                ["unchangedCount"] = FormatNumber(result.UnchangedCount),
                ["protectedCount"] = FormatNumber(result.ProtectedCount),
                ["skippedNonWorkingDateCount"] = FormatNumber(result.SkippedNonWorkingDateCount),
                ["deletedNonWorkingAutoRuleCount"] = FormatNumber(result.DeletedNonWorkingAutoRuleCount),
                ["issueCount"] = FormatNumber(result.Issues.Count),
                ["affectedCount"] = FormatNumber(affectedCount),
                ["ruleVersion"] = "1"
            });
    }

    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static string FormatNumber(int value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private async Task<IReadOnlyList<EmployeeContext>> LoadEligibleEmployeesAsync(CancellationToken cancellationToken)
    {
        var rows = await (
                from employee in dbContext.Employees.AsNoTracking()
                join department in dbContext.Departments.AsNoTracking()
                    on employee.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                where !employee.IsDeleted && employee.Status != ResignedEmployeeStatus
                select new { employee, department })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new EmployeeContext(
                row.employee.Id,
                row.employee.EmployeeCode,
                BuildEmployeeName(row.employee),
                row.department,
                BuildDepartmentPathCandidates(row.department)))
            .OrderBy(employee => employee.DepartmentPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(employee => employee.EmployeeCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<ResolvedShiftRule> ResolveRules(
        IReadOnlyList<ShiftSchedulingSettingRow> settings,
        IReadOnlyList<EmployeeContext> employees,
        IReadOnlyDictionary<Guid, AttendanceShiftRow> shifts,
        List<AttendanceShiftAssignmentEnsureIssueDto> issues)
    {
        var rules = new List<ResolvedShiftRule>();
        foreach (var setting in settings)
        {
            if (!setting.ShiftId.HasValue || setting.ShiftId.Value == Guid.Empty)
            {
                issues.Add(new AttendanceShiftAssignmentEnsureIssueDto(
                    "MissingShift",
                    "Cấu hình xếp ca chưa chọn ca làm việc.",
                    SettingId: setting.Id));
                continue;
            }

            if (!shifts.ContainsKey(setting.ShiftId.Value))
            {
                issues.Add(new AttendanceShiftAssignmentEnsureIssueDto(
                    "InvalidShift",
                    "Cấu hình xếp ca đang trỏ tới ca làm việc không tồn tại hoặc đã ngừng sử dụng.",
                    SettingId: setting.Id));
                continue;
            }

            var normalizedValue = NormalizeForCompare(setting.Value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                issues.Add(new AttendanceShiftAssignmentEnsureIssueDto(
                    "MissingTarget",
                    "Cấu hình xếp ca chưa chọn đối tượng áp dụng.",
                    SettingId: setting.Id));
                continue;
            }

            var resolvedRule = setting.ClassificationType switch
            {
                2 => ResolveDepartmentRule(setting, normalizedValue, employees),
                5 => ResolveEmployeeRule(setting, normalizedValue, employees),
                _ => null
            };

            if (resolvedRule is null)
            {
                issues.Add(new AttendanceShiftAssignmentEnsureIssueDto(
                    "UnresolvedTarget",
                    $"Không thể xác định đối tượng áp dụng của cấu hình xếp ca '{setting.Value}'.",
                    SettingId: setting.Id));
                continue;
            }

            rules.Add(resolvedRule);
        }

        return rules;
    }

    private static ResolvedShiftRule? ResolveDepartmentRule(
        ShiftSchedulingSettingRow setting,
        string normalizedValue,
        IReadOnlyList<EmployeeContext> employees)
    {
        var matchedEmployees = new List<Guid>();
        int? specificity = null;

        foreach (var employee in employees)
        {
            var pathMatch = employee.DepartmentPaths.FirstOrDefault(path =>
                TextComparer.Equals(path.NormalizedPath, normalizedValue));

            if (pathMatch is null)
            {
                continue;
            }

            specificity ??= pathMatch.Specificity;
            matchedEmployees.Add(employee.Id);
        }

        return matchedEmployees.Count == 0
            ? null
            : new ResolvedShiftRule(
                setting.Id,
                setting.ShiftId!.Value,
                specificity ?? DepartmentSpecificity,
                GetEffectiveTimestamp(setting),
                matchedEmployees);
    }

    private static ResolvedShiftRule? ResolveEmployeeRule(
        ShiftSchedulingSettingRow setting,
        string normalizedValue,
        IReadOnlyList<EmployeeContext> employees)
    {
        var employeeCode = ExtractEmployeeCode(setting.Value);
        var matches = !string.IsNullOrWhiteSpace(employeeCode)
            ? employees
                .Where(employee => TextComparer.Equals(employee.EmployeeCode, employeeCode))
                .ToArray()
            : Array.Empty<EmployeeContext>();

        if (matches.Length == 0)
        {
            matches = employees
                .Where(employee => TextComparer.Equals(
                    NormalizeForCompare(employee.DisplayText),
                    normalizedValue))
                .ToArray();
        }

        return matches.Length == 1
            ? new ResolvedShiftRule(
                setting.Id,
                setting.ShiftId!.Value,
                EmployeeSpecificity,
                GetEffectiveTimestamp(setting),
                [matches[0].Id])
            : null;
    }

    private static Dictionary<Guid, ResolvedShiftRule> ResolveWinningRulesByEmployee(
        IReadOnlyList<ResolvedShiftRule> rules)
    {
        var ruleByEmployeeId = new Dictionary<Guid, ResolvedShiftRule>();
        foreach (var rule in rules
                     .OrderBy(rule => rule.Specificity)
                     .ThenBy(rule => rule.EffectiveAtUtc)
                     .ThenBy(rule => rule.SettingId))
        {
            foreach (var employeeId in rule.EmployeeIds)
            {
                ruleByEmployeeId[employeeId] = rule;
            }
        }

        return ruleByEmployeeId;
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfNeededAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return null;
        }

        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayType>> LoadWorkCalendarDayTypesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var calendarDays = await dbContext.AttendanceWorkCalendarDays
                .AsNoTracking()
                .Where(day => day.WorkDate >= fromDate && day.WorkDate <= toDate)
                .Select(day => new { day.WorkDate, day.DayType })
                .ToListAsync(cancellationToken);

            return calendarDays.ToDictionary(
                day => day.WorkDate,
                day => day.DayType);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return new Dictionary<DateOnly, AttendanceWorkCalendarDayType>();
        }
    }

    private async Task<IReadOnlyList<AttendanceShiftAssignmentRow>> LoadExistingAssignmentRowsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var employeeIdArray = employeeIds.ToArray();
        if (employeeIdArray.Length == 0)
        {
            return [];
        }

        return await dbContext.ShiftAssignments
            .Where(assignment =>
                employeeIdArray.Contains(assignment.EmployeeId)
                && assignment.WorkDate >= fromDate
                && assignment.WorkDate <= toDate)
            .ToListAsync(cancellationToken);
    }

    private static bool IsNonWorkingDate(
        IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayType> dayTypesByDate,
        DateOnly workDate)
    {
        var dayType = dayTypesByDate.TryGetValue(workDate, out var configuredDayType)
            ? configuredDayType
            : AttendanceWorkCalendarDayTypes.ResolveDefaultDayType(workDate);

        return AttendanceWorkCalendarDayTypes.IsSpecialDay(dayType);
    }

    private NonWorkingAssignmentCleanupResult RemoveNonWorkingAutoRuleAssignments(
        IReadOnlyCollection<AttendanceShiftAssignmentRow> rows,
        IDictionary<(Guid EmployeeId, DateOnly WorkDate), AttendanceShiftAssignmentRow> rowsByKey,
        IReadOnlyDictionary<DateOnly, AttendanceWorkCalendarDayType> dayTypesByDate)
    {
        var deletedCount = 0;
        var protectedCount = 0;

        foreach (var row in rows)
        {
            if (!IsNonWorkingDate(dayTypesByDate, row.WorkDate))
            {
                continue;
            }

            if (!string.Equals(row.CreationType, AutoRuleCreationType, StringComparison.OrdinalIgnoreCase))
            {
                protectedCount++;
                continue;
            }

            dbContext.ShiftAssignments.Remove(row);
            rowsByKey.Remove((row.EmployeeId, row.WorkDate));
            deletedCount++;
        }

        return new NonWorkingAssignmentCleanupResult(deletedCount, protectedCount);
    }

    private async Task EnsureRequiredTablesAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS public.shift_scheduling_settings (
                "Id" uuid NOT NULL,
                "ShiftId" uuid NULL,
                "ClassificationType" integer NOT NULL,
                "Value" character varying(500) NULL,
                "AssignmentScopeMode" integer NOT NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_shift_scheduling_settings" PRIMARY KEY ("Id")
            );

            ALTER TABLE public.shift_scheduling_settings
            ADD COLUMN IF NOT EXISTS "Value" character varying(500) NULL;

            ALTER TABLE public.shift_scheduling_settings
            ADD COLUMN IF NOT EXISTS "ShiftId" uuid NULL;

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
    }

    private static (DateOnly FromDate, DateOnly ToDate) NormalizeDateRange(DateOnly fromDate, DateOnly toDate) =>
        toDate < fromDate ? (toDate, fromDate) : (fromDate, toDate);

    private static IReadOnlyList<DepartmentPathCandidate> BuildDepartmentPathCandidates(AttendanceDepartmentRow? department)
    {
        if (department is null)
        {
            return [];
        }

        var blockName = NormalizePathPart(department.CenterName) ?? "(Chưa có khối)";
        var departmentName = NormalizePathPart(department.DepartmentOrWorkshopName) ?? "(Chưa có phòng ban)";
        var teamName = NormalizePathPart(department.TeamName);
        var groupName = NormalizePathPart(department.GroupName);

        var candidates = new List<DepartmentPathCandidate>
        {
            CreateDepartmentPathCandidate(BlockSpecificity, blockName),
            CreateDepartmentPathCandidate(DepartmentSpecificity, blockName, departmentName)
        };

        if (!string.IsNullOrWhiteSpace(teamName))
        {
            candidates.Add(CreateDepartmentPathCandidate(TeamSpecificity, blockName, departmentName, teamName));
        }

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            candidates.Add(CreateDepartmentPathCandidate(GroupSpecificity, blockName, departmentName, teamName, groupName));
        }

        return candidates;
    }

    private static DepartmentPathCandidate CreateDepartmentPathCandidate(int specificity, params string?[] parts)
    {
        var path = JoinPath(parts);
        return new DepartmentPathCandidate(path, NormalizeForCompare(path), specificity);
    }

    private static string JoinPath(params string?[] parts) =>
        string.Join(" / ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));

    private static string? ExtractEmployeeCode(string? value)
    {
        var normalizedValue = NormalizePathPart(value);
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return null;
        }

        var separatorIndex = normalizedValue.IndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex <= 0
            ? normalizedValue
            : normalizedValue[..separatorIndex].Trim();
    }

    private static string BuildEmployeeName(AttendanceGatewayEmployeeRow employee) =>
        string.Join(
            ' ',
            new[] { employee.LastName, employee.FirstName }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part.Trim()));

    private static string? NormalizePathPart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeForCompare(string? value) =>
        NormalizePathPart(value)?.ToUpperInvariant() ?? string.Empty;

    private static string BuildSyncNote(string source, Guid settingId) =>
        $"Synced from shift_scheduling_settings:{settingId}; source:{NormalizePathPart(source) ?? "Unknown"}";

    private static DateTime GetEffectiveTimestamp(ShiftSchedulingSettingRow setting) =>
        setting.UpdatedAtUtc ?? setting.CreatedAtUtc;

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private static IEnumerable<DateOnly> EnumerateDateRange(DateOnly fromDate, DateOnly toDate)
    {
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private sealed record EmployeeContext(
        Guid Id,
        string EmployeeCode,
        string FullName,
        AttendanceDepartmentRow? Department,
        IReadOnlyList<DepartmentPathCandidate> DepartmentPaths)
    {
        public string DisplayText => string.IsNullOrWhiteSpace(EmployeeCode)
            ? FullName
            : $"{EmployeeCode} - {FullName}";

        public string DepartmentPath => DepartmentPaths.LastOrDefault()?.Path ?? string.Empty;
    }

    private sealed record DepartmentPathCandidate(
        string Path,
        string NormalizedPath,
        int Specificity);

    private sealed record ResolvedShiftRule(
        Guid SettingId,
        Guid ShiftId,
        int Specificity,
        DateTime EffectiveAtUtc,
        IReadOnlyList<Guid> EmployeeIds);

    private sealed record NonWorkingAssignmentCleanupResult(
        int DeletedCount,
        int ProtectedCount);
}
