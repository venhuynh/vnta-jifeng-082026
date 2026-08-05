using System.Diagnostics;
using System.Globalization;
using System.Text;
using Vnta.PostgresSync.Console.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Vnta.PostgresSync.Console.Services;

public sealed class PostgresSyncRunner
{
    private const string DevicesTableName = "public.devices";
    private const string AttendanceLogsTableName = "public.attendance_logs";
    private const string PayrollBasicSalaryTableName = "public.payroll_basic_salary_records";

    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgresSyncRunner> _logger;
    private readonly IOptionsMonitor<PostgresSyncOptions> _optionsMonitor;
    private readonly Dictionary<Guid, Guid> _deviceIdMappings = [];

    public PostgresSyncRunner(
        IConfiguration configuration,
        ILogger<PostgresSyncRunner> logger,
        IOptionsMonitor<PostgresSyncOptions> optionsMonitor)
    {
        _configuration = configuration;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    public async Task RunOnceAsync(
        IReadOnlyList<SyncPhase> phases,
        IReadOnlyDictionary<string, string>? tokens,
        IReadOnlySet<string>? includedTables,
        CancellationToken cancellationToken)
    {
        _deviceIdMappings.Clear();

        var options = _optionsMonitor.CurrentValue;
        var sourceConnectionString = ResolveConnectionString(
            options.SourceConnectionString,
            "SourcePostgres",
            "VNTA_POSTGRES_SYNC_SOURCE");
        var targetConnectionString = ResolveTargetConnectionString(options.TargetConnectionString);

        ValidateWorkerOptions(options, sourceConnectionString, targetConnectionString);

        var enabledTables = options.Tables
            .Where(table => table.Enabled && phases.Contains(table.Phase))
            .Where(table => includedTables is null || includedTables.Contains(ResolveTableName(table)))
            .OrderBy(table => table.Phase)
            .ThenBy(table => table.Order)
            .ThenBy(table => table.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (enabledTables.Length == 0)
        {
            _logger.LogWarning(
                "PostgreSQL sync worker has no enabled table mappings for phases {Phases}.",
                string.Join(", ", phases));
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        await using var sourceConnection = new NpgsqlConnection(sourceConnectionString);
        await using var targetConnection = new NpgsqlConnection(targetConnectionString);

        await sourceConnection.OpenAsync(cancellationToken);
        await targetConnection.OpenAsync(cancellationToken);

        var totalRows = 0;
        foreach (var table in enabledTables)
        {
            totalRows += await SyncTableAsync(
                sourceConnection,
                targetConnection,
                table,
                tokens,
                cancellationToken);
        }

        _logger.LogInformation(
            "PostgreSQL sync cycle completed. Tables={TableCount}; Rows={TotalRows}; ElapsedMs={ElapsedMs}.",
            enabledTables.Length,
            totalRows,
            stopwatch.ElapsedMilliseconds);
    }

    public async Task<PayrollBasicSalarySyncResult> SyncPayrollBasicSalaryFromPreviousMonthAsync(
        int targetMonth,
        int targetYear,
        CancellationToken cancellationToken)
    {
        var sourceConnectionString = ResolveConnectionString(
            _optionsMonitor.CurrentValue.SourceConnectionString,
            "SourcePostgres",
            "VNTA_POSTGRES_SYNC_SOURCE");
        var targetConnectionString = ResolveTargetConnectionString(_optionsMonitor.CurrentValue.TargetConnectionString);

        ValidateWorkerOptions(_optionsMonitor.CurrentValue, sourceConnectionString, targetConnectionString);

        if (targetMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Target payroll month must be between 1 and 12.");
        }

        if (targetYear is < 1 or > 9999)
        {
            throw new InvalidOperationException("Target payroll year must be between 1 and 9999.");
        }

        var (sourceMonth, sourceYear) = GetPreviousPayrollPeriod(targetMonth, targetYear);
        var synchronizedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        await using var targetConnection = new NpgsqlConnection(targetConnectionString);
        await targetConnection.OpenAsync(cancellationToken);
        await using var transaction = await targetConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            var sourceRows = await ReadPayrollBasicSalaryRowsAsync(
                targetConnection,
                transaction,
                sourceMonth,
                sourceYear,
                cancellationToken);

            if (sourceRows.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return new PayrollBasicSalarySyncResult(
                    sourceMonth,
                    sourceYear,
                    targetMonth,
                    targetYear,
                    SourceRecordCount: 0,
                    CreatedRecordCount: 0,
                    UpdatedRecordCount: 0,
                    UnchangedRecordCount: 0,
                    SynchronizedAtUtc: synchronizedAtUtc);
            }

            var targetRows = await ReadExistingPayrollBasicSalaryRowsAsync(
                targetConnection,
                transaction,
                targetMonth,
                targetYear,
                sourceRows.Select(row => row.EmployeeId).ToArray(),
                cancellationToken);

            var createdRecordCount = 0;
            var updatedRecordCount = 0;
            var unchangedRecordCount = 0;

            foreach (var sourceRow in sourceRows)
            {
                if (targetRows.TryGetValue(sourceRow.EmployeeId, out var targetRow))
                {
                    if (HasSamePayrollBasicSalary(targetRow, sourceRow))
                    {
                        unchangedRecordCount++;
                        continue;
                    }

                    await UpdatePayrollBasicSalaryRowAsync(
                        targetConnection,
                        transaction,
                        targetRow.Id,
                        sourceRow,
                        synchronizedAtUtc,
                        cancellationToken);

                    updatedRecordCount++;
                    continue;
                }

                await InsertPayrollBasicSalaryRowAsync(
                    targetConnection,
                    transaction,
                    targetMonth,
                    targetYear,
                    sourceRow,
                    synchronizedAtUtc,
                    cancellationToken);

                createdRecordCount++;
            }

            await transaction.CommitAsync(cancellationToken);

            return new PayrollBasicSalarySyncResult(
                sourceMonth,
                sourceYear,
                targetMonth,
                targetYear,
                SourceRecordCount: sourceRows.Count,
                CreatedRecordCount: createdRecordCount,
                UpdatedRecordCount: updatedRecordCount,
                UnchangedRecordCount: unchangedRecordCount,
                SynchronizedAtUtc: synchronizedAtUtc);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }






    public async Task<PayrollOtherAllowanceSyncResult> SyncPayrollOtherAllowanceAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken)
    {
        if (payrollMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Payroll month must be between 1 and 12.");
        }

        if (payrollYear is < 1 or > 9999)
        {
            throw new InvalidOperationException("Payroll year must be between 1 and 9999.");
        }

        var sourceConnectionString = ResolveConnectionString(
            _optionsMonitor.CurrentValue.SourceConnectionString,
            "SourcePostgres",
            "VNTA_POSTGRES_SYNC_SOURCE");
        var targetConnectionString = ResolveTargetConnectionString(_optionsMonitor.CurrentValue.TargetConnectionString);
        ValidateWorkerOptions(_optionsMonitor.CurrentValue, sourceConnectionString, targetConnectionString);

        await using var sourceConnection = new NpgsqlConnection(sourceConnectionString);
        await sourceConnection.OpenAsync(cancellationToken);
        var sourceRows = await ReadPayrollOtherAllowanceRowsAsync(
            sourceConnection,
            payrollMonth,
            payrollYear,
            cancellationToken);
        ValidatePayrollOtherAllowanceSourceRows(sourceRows);

        var sourceTotalAmount = sourceRows.Sum(row => row.AllowanceAmount);
        var normalizedToFixedSnapshotCount = sourceRows.Count(row =>
            !row.SourceIsFixedAmount && row.AllowanceAmount > 0m);
        if (sourceRows.Count == 0)
        {
            return new PayrollOtherAllowanceSyncResult(
                payrollMonth, payrollYear, 0, 0, 0, 0, 0, 0, sourceTotalAmount, 0m, 0);
        }

        await using var targetConnection = new NpgsqlConnection(targetConnectionString);
        await targetConnection.OpenAsync(cancellationToken);
        await using var transaction = await targetConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            var employeeIds = sourceRows.Select(row => row.EmployeeId).Distinct().ToArray();
            var targetSummaries = await ReadPayrollAllowanceSummariesAsync(
                targetConnection, transaction, payrollMonth, payrollYear, employeeIds, cancellationToken);
            EnsureAllPayrollOtherAllowanceParentsExist(sourceRows, targetSummaries, payrollMonth, payrollYear);

            var existingRows = await ReadExistingPayrollOtherAllowanceRowsAsync(
                targetConnection, transaction, sourceRows.Select(row => row.Id).ToArray(), cancellationToken);

            var createdCount = 0;
            var updatedCount = 0;
            var unchangedCount = 0;
            var skippedLockedCount = 0;
            var synchronizedTotalAmount = 0m;
            var affectedSummaryIds = new HashSet<Guid>();
            var synchronizedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            foreach (var sourceRow in sourceRows)
            {
                var targetSummary = targetSummaries[sourceRow.EmployeeId];
                if (targetSummary.IsLocked
                    || (existingRows.TryGetValue(sourceRow.Id, out var existingLockedRow) && existingLockedRow.IsLocked))
                {
                    skippedLockedCount++;
                    continue;
                }

                synchronizedTotalAmount += sourceRow.AllowanceAmount;
                affectedSummaryIds.Add(targetSummary.Id);

                if (!existingRows.TryGetValue(sourceRow.Id, out var existingRow))
                {
                    await InsertPayrollOtherAllowanceRowAsync(
                        targetConnection, transaction, sourceRow, targetSummary.Id, cancellationToken);
                    createdCount++;
                    continue;
                }

                if (HasSamePayrollOtherAllowance(existingRow, sourceRow, targetSummary.Id))
                {
                    unchangedCount++;
                    continue;
                }

                affectedSummaryIds.Add(existingRow.PayrollAllowanceSummaryRecordId);
                await UpdatePayrollOtherAllowanceRowAsync(
                    targetConnection, transaction, sourceRow, targetSummary.Id, cancellationToken);
                updatedCount++;
            }

            var synchronizedSummaryCount = await SynchronizePayrollOtherAllowanceSummaryTotalsAsync(
                targetConnection, transaction, affectedSummaryIds, synchronizedAtUtc, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new PayrollOtherAllowanceSyncResult(
                payrollMonth,
                payrollYear,
                sourceRows.Count,
                createdCount,
                updatedCount,
                unchangedCount,
                skippedLockedCount,
                normalizedToFixedSnapshotCount,
                sourceTotalAmount,
                synchronizedTotalAmount,
                synchronizedSummaryCount);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<List<PayrollOtherAllowanceSourceRow>> ReadPayrollOtherAllowanceRowsAsync(
        NpgsqlConnection sourceConnection,
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT detail."Id",
                   detail."EmployeeId",
                   detail."Ten_Phu_Cap",
                   detail."IsFixed",
                   detail."So_Tien"::numeric,
                   detail."GhiChu",
                   detail."CreatedAtUtc" AT TIME ZONE 'Asia/Ho_Chi_Minh',
                   detail."CreatedBy",
                   detail."UpdatedAtUtc" AT TIME ZONE 'Asia/Ho_Chi_Minh',
                   detail."UpdatedBy"
            FROM public.payroll_monthly_employee_other_allowance_details AS detail
            WHERE detail."Nam" = @payrollYear
              AND detail."Thang" = @payrollMonth
            ORDER BY detail."EmployeeId", detail."Id";
            """;

        await using var command = new NpgsqlCommand(sql, sourceConnection);
        command.Parameters.AddWithValue("payrollMonth", payrollMonth);
        command.Parameters.AddWithValue("payrollYear", payrollYear);

        var rows = new List<PayrollOtherAllowanceSourceRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PayrollOtherAllowanceSourceRow(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetBoolean(3),
                Convert.ToDecimal(reader.GetValue(4), CultureInfo.InvariantCulture),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetDateTime(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                reader.IsDBNull(9) ? null : reader.GetString(9)));
        }

        return rows;
    }

    private static void ValidatePayrollOtherAllowanceSourceRows(
        IReadOnlyList<PayrollOtherAllowanceSourceRow> sourceRows)
    {
        var invalidRows = sourceRows
            .Select(row => new { Row = row, Reason = GetPayrollOtherAllowanceValidationReason(row) })
            .Where(item => item.Reason is not null)
            .Take(10)
            .Select(item => $"{item.Row.Id} ({item.Reason})")
            .ToArray();

        if (invalidRows.Length > 0)
        {
            throw new InvalidOperationException(
                "Nguồn phụ cấp khác có dữ liệu không phù hợp với ràng buộc đích. "
                + $"Các dòng đầu tiên: {string.Join("; ", invalidRows)}.");
        }
    }

    private static string? GetPayrollOtherAllowanceValidationReason(PayrollOtherAllowanceSourceRow row)
    {
        if (row.Id == Guid.Empty || row.EmployeeId == Guid.Empty)
        {
            return "thiếu Id";
        }

        if (string.IsNullOrWhiteSpace(row.AllowanceName))
        {
            return "tên phụ cấp trống";
        }

        if (row.AllowanceName.Trim().Length > 256)
        {
            return "tên phụ cấp dài quá 256 ký tự";
        }

        if (row.AllowanceAmount < 0m)
        {
            return "số tiền âm";
        }

        if (NormalizeAuditActor(row.CreatedBy, "postgres-sync").Length > 128
            || (row.UpdatedBy is not null && NormalizeAuditActor(row.UpdatedBy, string.Empty).Length > 128))
        {
            return "người tạo/sửa dài quá 128 ký tự";
        }

        return null;
    }

    private static async Task<Dictionary<Guid, PayrollAllowanceSummaryTargetRow>> ReadPayrollAllowanceSummariesAsync(
        NpgsqlConnection targetConnection,
        NpgsqlTransaction transaction,
        int payrollMonth,
        int payrollYear,
        IReadOnlyList<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id", "EmployeeId", "IsLocked"
            FROM public.payroll_allowance_summary_records
            WHERE "PayrollMonth" = @payrollMonth
              AND "PayrollYear" = @payrollYear
              AND "EmployeeId" = ANY(@employeeIds)
            FOR UPDATE;
            """;

        await using var command = new NpgsqlCommand(sql, targetConnection, transaction);
        command.Parameters.AddWithValue("payrollMonth", payrollMonth);
        command.Parameters.AddWithValue("payrollYear", payrollYear);
        command.Parameters.AddWithValue("employeeIds", employeeIds.ToArray());

        var rows = new Dictionary<Guid, PayrollAllowanceSummaryTargetRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new PayrollAllowanceSummaryTargetRow(
                reader.GetGuid(0), reader.GetGuid(1), reader.GetBoolean(2));
            if (!rows.TryAdd(row.EmployeeId, row))
            {
                throw new InvalidOperationException(
                    $"Có nhiều bản ghi tổng hợp phụ cấp cho nhân viên '{row.EmployeeId}' trong kỳ {payrollMonth:00}/{payrollYear:0000}.");
            }
        }

        return rows;
    }

    private static void EnsureAllPayrollOtherAllowanceParentsExist(
        IReadOnlyList<PayrollOtherAllowanceSourceRow> sourceRows,
        IReadOnlyDictionary<Guid, PayrollAllowanceSummaryTargetRow> targetSummaries,
        int payrollMonth,
        int payrollYear)
    {
        var missingEmployeeIds = sourceRows
            .Select(row => row.EmployeeId)
            .Distinct()
            .Where(employeeId => !targetSummaries.ContainsKey(employeeId))
            .Take(20)
            .ToArray();

        if (missingEmployeeIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Thiếu bản ghi tổng hợp phụ cấp đích cho kỳ {payrollMonth:00}/{payrollYear:0000}. "
                + $"Các EmployeeId đầu tiên: {string.Join(", ", missingEmployeeIds)}");
        }
    }

    private static async Task<Dictionary<Guid, ExistingPayrollOtherAllowanceRow>> ReadExistingPayrollOtherAllowanceRowsAsync(
        NpgsqlConnection targetConnection,
        NpgsqlTransaction transaction,
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id",
                   "PayrollAllowanceSummaryRecordId",
                   "AllowanceName",
                   "IsFixedAmount",
                   "AllowanceAmount",
                   "Note",
                   "IsLocked",
                   "CreatedAtUtc",
                   "CreatedBy",
                   "UpdatedAtUtc",
                   "UpdatedBy"
            FROM public.payroll_allowance_other
            WHERE "Id" = ANY(@ids)
            FOR UPDATE;
            """;

        await using var command = new NpgsqlCommand(sql, targetConnection, transaction);
        command.Parameters.AddWithValue("ids", ids.ToArray());

        var rows = new Dictionary<Guid, ExistingPayrollOtherAllowanceRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new ExistingPayrollOtherAllowanceRow(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetBoolean(3),
                reader.GetDecimal(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetBoolean(6),
                reader.GetDateTime(7),
                reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                reader.IsDBNull(10) ? null : reader.GetString(10));
            rows.Add(row.Id, row);
        }

        return rows;
    }

    private static bool HasSamePayrollOtherAllowance(
        ExistingPayrollOtherAllowanceRow existingRow,
        PayrollOtherAllowanceSourceRow sourceRow,
        Guid targetSummaryId) =>
        existingRow.PayrollAllowanceSummaryRecordId == targetSummaryId
        && string.Equals(existingRow.AllowanceName, sourceRow.AllowanceName.Trim(), StringComparison.Ordinal)
        && existingRow.IsFixedAmount == sourceRow.IsFixedAmount
        && existingRow.AllowanceAmount == sourceRow.AllowanceAmount
        && string.Equals(existingRow.Note, NormalizeOptionalText(sourceRow.Note), StringComparison.Ordinal)
        && existingRow.CreatedAtUtc == sourceRow.CreatedAtUtc
        && string.Equals(existingRow.CreatedBy, NormalizeAuditActor(sourceRow.CreatedBy, "postgres-sync"), StringComparison.Ordinal)
        && existingRow.UpdatedAtUtc == sourceRow.UpdatedAtUtc
        && string.Equals(existingRow.UpdatedBy, NormalizeOptionalText(sourceRow.UpdatedBy), StringComparison.Ordinal);

    private static async Task InsertPayrollOtherAllowanceRowAsync(
        NpgsqlConnection targetConnection,
        NpgsqlTransaction transaction,
        PayrollOtherAllowanceSourceRow sourceRow,
        Guid targetSummaryId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO public.payroll_allowance_other
            (
                "Id", "PayrollAllowanceSummaryRecordId", "AllowanceName", "IsFixedAmount",
                "AllowanceAmount", "Note", "IsLocked", "CreatedAtUtc", "CreatedBy",
                "UpdatedAtUtc", "UpdatedBy"
            )
            VALUES
            (
                @id, @summaryId, @allowanceName, @isFixedAmount,
                @allowanceAmount, @note, FALSE, @createdAtUtc, @createdBy,
                @updatedAtUtc, @updatedBy
            );
            """;

        await using var command = new NpgsqlCommand(sql, targetConnection, transaction);
        AddPayrollOtherAllowanceParameters(command, sourceRow, targetSummaryId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdatePayrollOtherAllowanceRowAsync(
        NpgsqlConnection targetConnection,
        NpgsqlTransaction transaction,
        PayrollOtherAllowanceSourceRow sourceRow,
        Guid targetSummaryId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE public.payroll_allowance_other
            SET "PayrollAllowanceSummaryRecordId" = @summaryId,
                "AllowanceName" = @allowanceName,
                "IsFixedAmount" = @isFixedAmount,
                "AllowanceAmount" = @allowanceAmount,
                "Note" = @note,
                "CreatedAtUtc" = @createdAtUtc,
                "CreatedBy" = @createdBy,
                "UpdatedAtUtc" = @updatedAtUtc,
                "UpdatedBy" = @updatedBy
            WHERE "Id" = @id;
            """;

        await using var command = new NpgsqlCommand(sql, targetConnection, transaction);
        AddPayrollOtherAllowanceParameters(command, sourceRow, targetSummaryId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddPayrollOtherAllowanceParameters(
        NpgsqlCommand command,
        PayrollOtherAllowanceSourceRow sourceRow,
        Guid targetSummaryId)
    {
        command.Parameters.AddWithValue("id", sourceRow.Id);
        command.Parameters.AddWithValue("summaryId", targetSummaryId);
        command.Parameters.AddWithValue("allowanceName", sourceRow.AllowanceName.Trim());
        command.Parameters.AddWithValue("isFixedAmount", sourceRow.IsFixedAmount);
        command.Parameters.AddWithValue("allowanceAmount", sourceRow.AllowanceAmount);
        command.Parameters.AddWithValue("note", (object?)NormalizeOptionalText(sourceRow.Note) ?? DBNull.Value);
        command.Parameters.AddWithValue("createdAtUtc", sourceRow.CreatedAtUtc);
        command.Parameters.AddWithValue("createdBy", NormalizeAuditActor(sourceRow.CreatedBy, "postgres-sync"));
        command.Parameters.AddWithValue("updatedAtUtc", (object?)sourceRow.UpdatedAtUtc ?? DBNull.Value);
        command.Parameters.AddWithValue("updatedBy", (object?)NormalizeOptionalText(sourceRow.UpdatedBy) ?? DBNull.Value);
    }

    private static async Task<int> SynchronizePayrollOtherAllowanceSummaryTotalsAsync(
        NpgsqlConnection targetConnection,
        NpgsqlTransaction transaction,
        IReadOnlySet<Guid> summaryIds,
        DateTime synchronizedAtUtc,
        CancellationToken cancellationToken)
    {
        if (summaryIds.Count == 0)
        {
            return 0;
        }

        const string sql = """
            WITH totals AS
            (
                SELECT summary."Id",
                       COALESCE(SUM(detail."AllowanceAmount"), 0) AS "TotalAmount"
                FROM public.payroll_allowance_summary_records AS summary
                LEFT JOIN public.payroll_allowance_other AS detail
                    ON detail."PayrollAllowanceSummaryRecordId" = summary."Id"
                WHERE summary."Id" = ANY(@summaryIds)
                GROUP BY summary."Id"
            )
            UPDATE public.payroll_allowance_summary_records AS summary
            SET "OtherAllowanceAmount" = totals."TotalAmount",
                "UpdatedAtUtc" = @updatedAtUtc,
                "UpdatedBy" = 'postgres-sync'
            FROM totals
            WHERE summary."Id" = totals."Id"
              AND summary."OtherAllowanceAmount" IS DISTINCT FROM totals."TotalAmount";
            """;

        await using var command = new NpgsqlCommand(sql, targetConnection, transaction);
        command.Parameters.AddWithValue("summaryIds", summaryIds.ToArray());
        command.Parameters.AddWithValue("updatedAtUtc", synchronizedAtUtc);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string NormalizeAuditActor(string? value, string fallback)
    {
        var normalized = NormalizeOptionalText(value);
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<PayrollInsuranceDeductionSyncResult> SyncPayrollInsuranceDeductionAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken)
    {
        if (payrollMonth is < 1 or > 12 || payrollYear is < 1 or > 9999)
        {
            throw new InvalidOperationException("Payroll period is invalid.");
        }

        var sourceConnectionString = ResolveConnectionString(
            _optionsMonitor.CurrentValue.SourceConnectionString, "SourcePostgres", "VNTA_POSTGRES_SYNC_SOURCE");
        var targetConnectionString = ResolveTargetConnectionString(_optionsMonitor.CurrentValue.TargetConnectionString);
        ValidateWorkerOptions(_optionsMonitor.CurrentValue, sourceConnectionString, targetConnectionString);

        await using var sourceConnection = new NpgsqlConnection(sourceConnectionString);
        await sourceConnection.OpenAsync(cancellationToken);
        var sourceRows = await ReadPayrollInsuranceDeductionRowsAsync(
            sourceConnection, payrollMonth, payrollYear, cancellationToken);
        ValidatePayrollInsuranceDeductionSourceRows(sourceRows);

        var sourceTotal = sourceRows.Sum(row => row.TotalDeductionAmount);
        if (sourceRows.Count == 0)
        {
            return new PayrollInsuranceDeductionSyncResult(
                payrollMonth, payrollYear, 0, 0, 0, 0, 0, sourceTotal, 0m);
        }

        await using var targetConnection = new NpgsqlConnection(targetConnectionString);
        await targetConnection.OpenAsync(cancellationToken);
        await using var transaction = await targetConnection.BeginTransactionAsync(cancellationToken);
        try
        {
            var summaries = await ReadPayrollDeductionSummariesAsync(
                targetConnection,
                transaction,
                payrollMonth,
                payrollYear,
                sourceRows.Select(row => row.EmployeeId).Distinct().ToArray(),
                cancellationToken);
            EnsureAllPayrollInsuranceParentsExist(sourceRows, summaries, payrollMonth, payrollYear);

            var existingRows = await ReadExistingPayrollInsuranceDeductionRowsAsync(
                targetConnection, transaction, summaries.Values.Select(row => row.Id).ToArray(), cancellationToken);
            var createdCount = 0;
            var updatedCount = 0;
            var unchangedCount = 0;
            var skippedLockedCount = 0;
            var synchronizedTotal = 0m;
            var updatedSummaryIds = new HashSet<Guid>();

            foreach (var sourceRow in sourceRows)
            {
                var summary = summaries[sourceRow.EmployeeId];
                if (summary.IsLocked
                    || (existingRows.TryGetValue(summary.Id, out var existingLockedRow) && existingLockedRow.IsLocked))
                {
                    skippedLockedCount++;
                    continue;
                }

                synchronizedTotal += sourceRow.TotalDeductionAmount;
                updatedSummaryIds.Add(summary.Id);
                if (!existingRows.TryGetValue(summary.Id, out var existingRow))
                {
                    await InsertPayrollInsuranceDeductionRowAsync(
                        targetConnection, transaction, sourceRow, summary.Id, cancellationToken);
                    createdCount++;
                    continue;
                }

                if (HasSamePayrollInsuranceDeduction(existingRow, sourceRow))
                {
                    unchangedCount++;
                    continue;
                }

                await UpdatePayrollInsuranceDeductionRowAsync(
                    targetConnection, transaction, sourceRow, summary.Id, cancellationToken);
                updatedCount++;
            }

            await SynchronizePayrollInsuranceDeductionSummaryTotalsAsync(
                targetConnection,
                transaction,
                updatedSummaryIds,
                DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new PayrollInsuranceDeductionSyncResult(
                payrollMonth,
                payrollYear,
                sourceRows.Count,
                createdCount,
                updatedCount,
                unchangedCount,
                skippedLockedCount,
                sourceTotal,
                synchronizedTotal);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<List<PayrollInsuranceDeductionSourceRow>> ReadPayrollInsuranceDeductionRowsAsync(
        NpgsqlConnection sourceConnection,
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "EmployeeId",
                   "TotalSocialInsuranceSalaryAmount",
                   "SocialInsuranceRate",
                   "HealthInsuranceRate",
                   "UnemploymentInsuranceRate",
                   "SocialInsuranceAmount",
                   "HealthInsuranceAmount",
                   "UnemploymentInsuranceAmount",
                   "TotalEmployeeInsuranceAmount",
                   "ParticipationStatus",
                   "ChangeType",
                   "EffectiveFrom",
                   "IsLock",
                   COALESCE("CalculatedAtUtc", "UpdatedAtUtc", CURRENT_TIMESTAMP) AT TIME ZONE 'Asia/Ho_Chi_Minh',
                   "UpdatedAtUtc" AT TIME ZONE 'Asia/Ho_Chi_Minh',
                   "GhiChu"
            FROM public.payroll_monthly_employee_social_health_insurance
            WHERE "Nam" = @payrollYear
              AND "Thang" = @payrollMonth
            ORDER BY "EmployeeId", "Id";
            """;

        await using var command = new NpgsqlCommand(sql, sourceConnection);
        command.Parameters.AddWithValue("payrollMonth", payrollMonth);
        command.Parameters.AddWithValue("payrollYear", payrollYear);
        var rows = new List<PayrollInsuranceDeductionSourceRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var participationStatus = reader.GetString(9);
            var changeType = reader.GetString(10);
            rows.Add(new PayrollInsuranceDeductionSourceRow(
                reader.GetGuid(0),
                reader.GetDecimal(1),
                NormalizeInsuranceRate(reader.GetDecimal(2)),
                NormalizeInsuranceRate(reader.GetDecimal(3)),
                NormalizeInsuranceRate(reader.GetDecimal(4)),
                reader.GetDecimal(5),
                reader.GetDecimal(6),
                reader.GetDecimal(7),
                reader.GetDecimal(8),
                ParseInsuranceParticipationStatus(participationStatus),
                ParseInsuranceParticipationChangeType(changeType),
                reader.IsDBNull(11) ? null : reader.GetFieldValue<DateOnly>(11),
                reader.GetBoolean(12),
                reader.GetDateTime(13),
                reader.IsDBNull(14) ? null : reader.GetDateTime(14),
                reader.IsDBNull(15) ? null : reader.GetString(15),
                participationStatus,
                changeType));
        }

        return rows;
    }

    private static async Task<Dictionary<Guid, PayrollDeductionSummaryTargetRow>> ReadPayrollDeductionSummariesAsync(
        NpgsqlConnection targetConnection,
        NpgsqlTransaction transaction,
        int payrollMonth,
        int payrollYear,
        IReadOnlyList<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id", "EmployeeId", "IsLocked"
            FROM public.payroll_decuction_summary_records
            WHERE "PayrollMonth" = @payrollMonth
              AND "PayrollYear" = @payrollYear
              AND "EmployeeId" = ANY(@employeeIds)
            FOR UPDATE;
            """;

        await using var command = new NpgsqlCommand(sql, targetConnection, transaction);
        command.Parameters.AddWithValue("payrollMonth", payrollMonth);
        command.Parameters.AddWithValue("payrollYear", payrollYear);
        command.Parameters.AddWithValue("employeeIds", employeeIds.ToArray());
        var rows = new Dictionary<Guid, PayrollDeductionSummaryTargetRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new PayrollDeductionSummaryTargetRow(
                reader.GetGuid(0), reader.GetGuid(1), reader.GetBoolean(2));
            if (!rows.TryAdd(row.EmployeeId, row))
            {
                throw new InvalidOperationException(
                    $"Có nhiều bản ghi tổng hợp khấu trừ cho nhân viên '{row.EmployeeId}' trong kỳ {payrollMonth:00}/{payrollYear:0000}.");
            }
        }

        return rows;
    }

    private static void EnsureAllPayrollInsuranceParentsExist(
        IReadOnlyList<PayrollInsuranceDeductionSourceRow> sourceRows,
        IReadOnlyDictionary<Guid, PayrollDeductionSummaryTargetRow> summaries,
        int payrollMonth,
        int payrollYear)
    {
        var missingEmployeeIds = sourceRows.Select(row => row.EmployeeId).Distinct()
            .Where(employeeId => !summaries.ContainsKey(employeeId)).Take(20).ToArray();
        if (missingEmployeeIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Thiếu bản ghi tổng hợp khấu trừ đích cho kỳ {payrollMonth:00}/{payrollYear:0000}. "
                + $"Các EmployeeId đầu tiên: {string.Join(", ", missingEmployeeIds)}");
        }
    }

    private static void ValidatePayrollInsuranceDeductionSourceRows(
        IReadOnlyList<PayrollInsuranceDeductionSourceRow> sourceRows)
    {
        var invalidRows = sourceRows.Select(row => new
            {
                Row = row,
                Reason = row.EmployeeId == Guid.Empty ? "thiếu EmployeeId"
                    : row.InsuranceSalaryBaseAmount < 0m ? "mức đóng âm"
                    : row.SocialInsuranceRate is < 0m or > 1m ? "tỷ lệ BHXH ngoài 0..1"
                    : row.HealthInsuranceRate is < 0m or > 1m ? "tỷ lệ BHYT ngoài 0..1"
                    : row.UnemploymentInsuranceRate is < 0m or > 1m ? "tỷ lệ BHTN ngoài 0..1"
                    : row.TotalInsuranceRate > 1m
                        ? $"tổng tỷ lệ={row.TotalInsuranceRate:N4} (BHXH={row.SocialInsuranceRate:N4}; BHYT={row.HealthInsuranceRate:N4}; BHTN={row.UnemploymentInsuranceRate:N4})"
                    : row.SocialInsuranceAmount < 0m || row.HealthInsuranceAmount < 0m
                        || row.UnemploymentInsuranceAmount < 0m || row.TotalDeductionAmount < 0m
                        ? "số tiền khấu trừ âm"
                    : row.IsParticipating is null ? $"ParticipationStatus không nhận diện: {row.SourceParticipationStatus}"
                    : row.ParticipationChangeType is null ? $"ChangeType không nhận diện: {row.SourceChangeType}"
                    : null
            })
            .Where(item => item.Reason is not null)
            .Take(10)
            .Select(item => $"{item.Row.EmployeeId} ({item.Reason})")
            .ToArray();

        if (invalidRows.Length > 0)
        {
            throw new InvalidOperationException(
                "Nguồn khấu trừ BHXH-Y tế có dữ liệu không phù hợp với ràng buộc đích. "
                + $"Các dòng đầu tiên: {string.Join("; ", invalidRows)}.");
        }
    }

    private static async Task<Dictionary<Guid, ExistingPayrollInsuranceDeductionRow>> ReadExistingPayrollInsuranceDeductionRowsAsync(
        NpgsqlConnection targetConnection,
        NpgsqlTransaction transaction,
        IReadOnlyList<Guid> summaryIds,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "PayrollDeductionSummaryRecordId", "InsuranceSalaryBaseAmount", "SocialInsuranceRate",
                   "HealthInsuranceRate", "UnemploymentInsuranceRate", "TotalInsuranceRate",
                   "SocialInsuranceAmount", "HealthInsuranceAmount", "UnemploymentInsuranceAmount",
                   "TotalDeductionAmount", "IsParticipating", "ParticipationChangeType", "EffectiveDate",
                   "IsLocked", "CreatedAtUtc", "UpdatedAtUtc", "InsuranceNote"
            FROM public.payroll_decuction_insurance_records
            WHERE "PayrollDeductionSummaryRecordId" = ANY(@summaryIds)
            FOR UPDATE;
            """;

        await using var command = new NpgsqlCommand(sql, targetConnection, transaction);
        command.Parameters.AddWithValue("summaryIds", summaryIds.ToArray());
        var rows = new Dictionary<Guid, ExistingPayrollInsuranceDeductionRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new ExistingPayrollInsuranceDeductionRow(
                reader.GetGuid(0), reader.GetDecimal(1), reader.GetDecimal(2), reader.GetDecimal(3),
                reader.GetDecimal(4), reader.GetDecimal(5), reader.GetDecimal(6), reader.GetDecimal(7),
                reader.GetDecimal(8), reader.GetDecimal(9), reader.GetBoolean(10), reader.GetInt16(11),
                reader.IsDBNull(12) ? null : reader.GetFieldValue<DateOnly>(12), reader.GetBoolean(13),
                reader.GetDateTime(14), reader.IsDBNull(15) ? null : reader.GetDateTime(15),
                reader.IsDBNull(16) ? null : reader.GetString(16));
            rows.Add(row.PayrollDeductionSummaryRecordId, row);
        }

        return rows;
    }

    private static bool HasSamePayrollInsuranceDeduction(
        ExistingPayrollInsuranceDeductionRow existingRow,
        PayrollInsuranceDeductionSourceRow sourceRow) =>
        existingRow.InsuranceSalaryBaseAmount == sourceRow.InsuranceSalaryBaseAmount
        && existingRow.SocialInsuranceRate == sourceRow.SocialInsuranceRate
        && existingRow.HealthInsuranceRate == sourceRow.HealthInsuranceRate
        && existingRow.UnemploymentInsuranceRate == sourceRow.UnemploymentInsuranceRate
        && existingRow.TotalInsuranceRate == sourceRow.TotalInsuranceRate
        && existingRow.SocialInsuranceAmount == sourceRow.SocialInsuranceAmount
        && existingRow.HealthInsuranceAmount == sourceRow.HealthInsuranceAmount
        && existingRow.UnemploymentInsuranceAmount == sourceRow.UnemploymentInsuranceAmount
        && existingRow.TotalDeductionAmount == sourceRow.TotalDeductionAmount
        && existingRow.IsParticipating == sourceRow.IsParticipating!.Value
        && existingRow.ParticipationChangeType == sourceRow.ParticipationChangeType!.Value
        && existingRow.EffectiveDate == sourceRow.EffectiveDate
        && existingRow.IsLocked == sourceRow.IsLocked
        && existingRow.CreatedAtUtc == sourceRow.CreatedAtUtc
        && existingRow.UpdatedAtUtc == sourceRow.UpdatedAtUtc
        && string.Equals(existingRow.InsuranceNote, NormalizeOptionalText(sourceRow.Note), StringComparison.Ordinal);

    private static async Task InsertPayrollInsuranceDeductionRowAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        PayrollInsuranceDeductionSourceRow sourceRow,
        Guid summaryId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO public.payroll_decuction_insurance_records
            (
                "PayrollDeductionSummaryRecordId", "InsuranceSalaryBaseAmount", "SocialInsuranceRate",
                "HealthInsuranceRate", "UnemploymentInsuranceRate", "TotalInsuranceRate",
                "SocialInsuranceAmount", "HealthInsuranceAmount", "UnemploymentInsuranceAmount",
                "TotalDeductionAmount", "IsParticipating", "ParticipationChangeType", "EffectiveDate",
                "IsLocked", "CreatedAtUtc", "UpdatedAtUtc", "InsuranceNote"
            )
            VALUES
            (
                @summaryId, @salaryBase, @socialRate, @healthRate, @unemploymentRate, @totalRate,
                @socialAmount, @healthAmount, @unemploymentAmount, @totalAmount, @isParticipating,
                @changeType, @effectiveDate, @isLocked, @createdAtUtc, @updatedAtUtc, @insuranceNote
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        AddPayrollInsuranceDeductionParameters(command, sourceRow, summaryId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdatePayrollInsuranceDeductionRowAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        PayrollInsuranceDeductionSourceRow sourceRow,
        Guid summaryId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE public.payroll_decuction_insurance_records
            SET "InsuranceSalaryBaseAmount" = @salaryBase,
                "SocialInsuranceRate" = @socialRate,
                "HealthInsuranceRate" = @healthRate,
                "UnemploymentInsuranceRate" = @unemploymentRate,
                "TotalInsuranceRate" = @totalRate,
                "SocialInsuranceAmount" = @socialAmount,
                "HealthInsuranceAmount" = @healthAmount,
                "UnemploymentInsuranceAmount" = @unemploymentAmount,
                "TotalDeductionAmount" = @totalAmount,
                "IsParticipating" = @isParticipating,
                "ParticipationChangeType" = @changeType,
                "EffectiveDate" = @effectiveDate,
                "IsLocked" = @isLocked,
                "CreatedAtUtc" = @createdAtUtc,
                "UpdatedAtUtc" = @updatedAtUtc,
                "InsuranceNote" = @insuranceNote
            WHERE "PayrollDeductionSummaryRecordId" = @summaryId;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        AddPayrollInsuranceDeductionParameters(command, sourceRow, summaryId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddPayrollInsuranceDeductionParameters(
        NpgsqlCommand command,
        PayrollInsuranceDeductionSourceRow sourceRow,
        Guid summaryId)
    {
        command.Parameters.AddWithValue("summaryId", summaryId);
        command.Parameters.AddWithValue("salaryBase", sourceRow.InsuranceSalaryBaseAmount);
        command.Parameters.AddWithValue("socialRate", sourceRow.SocialInsuranceRate);
        command.Parameters.AddWithValue("healthRate", sourceRow.HealthInsuranceRate);
        command.Parameters.AddWithValue("unemploymentRate", sourceRow.UnemploymentInsuranceRate);
        command.Parameters.AddWithValue("totalRate", sourceRow.TotalInsuranceRate);
        command.Parameters.AddWithValue("socialAmount", sourceRow.SocialInsuranceAmount);
        command.Parameters.AddWithValue("healthAmount", sourceRow.HealthInsuranceAmount);
        command.Parameters.AddWithValue("unemploymentAmount", sourceRow.UnemploymentInsuranceAmount);
        command.Parameters.AddWithValue("totalAmount", sourceRow.TotalDeductionAmount);
        command.Parameters.AddWithValue("isParticipating", sourceRow.IsParticipating!.Value);
        command.Parameters.AddWithValue("changeType", sourceRow.ParticipationChangeType!.Value);
        command.Parameters.AddWithValue("effectiveDate", (object?)sourceRow.EffectiveDate ?? DBNull.Value);
        command.Parameters.AddWithValue("isLocked", sourceRow.IsLocked);
        command.Parameters.AddWithValue("createdAtUtc", sourceRow.CreatedAtUtc);
        command.Parameters.AddWithValue("updatedAtUtc", (object?)sourceRow.UpdatedAtUtc ?? DBNull.Value);
        command.Parameters.AddWithValue("insuranceNote", (object?)NormalizeOptionalText(sourceRow.Note) ?? DBNull.Value);
    }

    private static async Task SynchronizePayrollInsuranceDeductionSummaryTotalsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlySet<Guid> summaryIds,
        DateTime synchronizedAtUtc,
        CancellationToken cancellationToken)
    {
        if (summaryIds.Count == 0)
        {
            return;
        }

        const string sql = """
            UPDATE public.payroll_decuction_summary_records AS summary
            SET "SocialInsuranceDeductionAmount" = insurance."TotalDeductionAmount",
                "UpdatedAtUtc" = @updatedAtUtc,
                "UpdatedBy" = 'postgres-sync'
            FROM public.payroll_decuction_insurance_records AS insurance
            WHERE insurance."PayrollDeductionSummaryRecordId" = summary."Id"
              AND summary."Id" = ANY(@summaryIds)
              AND summary."SocialInsuranceDeductionAmount" IS DISTINCT FROM insurance."TotalDeductionAmount";
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("summaryIds", summaryIds.ToArray());
        command.Parameters.AddWithValue("updatedAtUtc", synchronizedAtUtc);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool? ParseInsuranceParticipationStatus(string value)
    {
        return NormalizeInsuranceCode(value) switch
        {
            "1" or "TRUE" or "PARTICIPATING" or "THAMGIA" or "DANGTHAMGIA" or "ACTIVE" => true,
            "0" or "FALSE" or "NOTPARTICIPATING" or "KHONGTHAMGIA" or "NGUNGTHAMGIA" or "INACTIVE" or "SUSPENDED" => false,
            _ => null
        };
    }

    private static decimal NormalizeInsuranceRate(decimal value) =>
        value >= 1m && value <= 100m ? value / 100m : value;

    private static short? ParseInsuranceParticipationChangeType(string value)
    {
        return NormalizeInsuranceCode(value) switch
        {
            "0" or "NONE" or "NOCHANGE" or "KHONGDOI" => 0,
            "1" or "INCREASE" or "TANG" or "NEW" => 1,
            "2" or "DECREASE" or "GIAM" => 2,
            "3" or "ADJUSTMENT" or "DIEUCHINH" => 3,
            _ => null
        };
    }

    private static string NormalizeInsuranceCode(string value)
    {
        var decomposed = (value ?? string.Empty).Trim().ToUpperInvariant()
            .Normalize(NormalizationForm.FormD);
        return new string(decomposed
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    public async Task<PayrollResponsibilityAllowanceSyncResult> SyncPayrollResponsibilityAllowanceAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken)
    {
        if (payrollMonth is < 1 or > 12 || payrollYear is < 1 or > 9999)
        {
            throw new InvalidOperationException("Payroll period is invalid.");
        }

        var sourceConnectionString = ResolveConnectionString(
            _optionsMonitor.CurrentValue.SourceConnectionString, "SourcePostgres", "VNTA_POSTGRES_SYNC_SOURCE");
        var targetConnectionString = ResolveTargetConnectionString(_optionsMonitor.CurrentValue.TargetConnectionString);
        ValidateWorkerOptions(_optionsMonitor.CurrentValue, sourceConnectionString, targetConnectionString);

        await using var sourceConnection = new NpgsqlConnection(sourceConnectionString);
        await sourceConnection.OpenAsync(cancellationToken);
        var sourceRows = await ReadPayrollResponsibilityAllowanceRowsAsync(sourceConnection, payrollMonth, payrollYear, cancellationToken);
        ValidatePayrollResponsibilityAllowanceSourceRows(sourceRows);
        var sourceTotal = sourceRows.Sum(row => row.ActualResponsibilityAllowanceAmount);
        if (sourceRows.Count == 0)
        {
            return new PayrollResponsibilityAllowanceSyncResult(payrollMonth, payrollYear, 0, 0, 0, 0, 0, 0, sourceTotal, 0m, 0);
        }

        await using var targetConnection = new NpgsqlConnection(targetConnectionString);
        await targetConnection.OpenAsync(cancellationToken);
        await using var transaction = await targetConnection.BeginTransactionAsync(cancellationToken);
        try
        {
            var summaries = await ReadPayrollResponsibilitySummariesAsync(targetConnection, transaction, payrollMonth, payrollYear,
                sourceRows.Select(row => row.EmployeeId).Distinct().ToArray(), cancellationToken);
            var missingParentEmployees = FindMissingPayrollResponsibilityParents(sourceRows, summaries);
            var availableGradeIds = await ReadAvailablePayrollResponsibilityGradeIdsAsync(targetConnection, transaction,
                sourceRows.Where(row => row.GradeId.HasValue).Select(row => row.GradeId!.Value).Distinct().ToArray(), cancellationToken);
            var existingRows = await ReadExistingPayrollResponsibilityRowsAsync(targetConnection, transaction,
                summaries.Values.Select(row => row.Id).ToArray(), cancellationToken);
            var created = 0; var updated = 0; var unchanged = 0; var skipped = 0; var synchronizedTotal = 0m;
            var summaryIds = new HashSet<Guid>();
            foreach (var sourceRow in sourceRows)
            {
                if (missingParentEmployees.Contains(sourceRow.EmployeeId))
                {
                    continue;
                }
                var summary = summaries[sourceRow.EmployeeId];
                var sourceForTarget = sourceRow with
                {
                    GradeId = sourceRow.GradeId is Guid gradeId && availableGradeIds.Contains(gradeId) ? gradeId : null
                };
                if (summary.IsLocked || (existingRows.TryGetValue(summary.Id, out var locked) && locked.IsLocked))
                {
                    skipped++;
                    continue;
                }

                summaryIds.Add(summary.Id);
                synchronizedTotal += sourceRow.ActualResponsibilityAllowanceAmount;
                if (!existingRows.TryGetValue(summary.Id, out var existing))
                {
                    await InsertPayrollResponsibilityRowAsync(targetConnection, transaction, sourceForTarget, summary, cancellationToken);
                    created++;
                }
                else if (HasSamePayrollResponsibilityRow(existing, sourceForTarget, summary.DepartmentName))
                {
                    unchanged++;
                }
                else
                {
                    await UpdatePayrollResponsibilityRowAsync(targetConnection, transaction, sourceForTarget, summary, cancellationToken);
                    updated++;
                }
            }

            var updatedSummaryCount = await SynchronizePayrollResponsibilitySummaryTotalsAsync(targetConnection, transaction,
                summaryIds, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new PayrollResponsibilityAllowanceSyncResult(payrollMonth, payrollYear, sourceRows.Count, created, updated,
                unchanged, skipped, missingParentEmployees.Count, sourceTotal, synchronizedTotal, updatedSummaryCount);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<List<PayrollResponsibilityAllowanceSourceRow>> ReadPayrollResponsibilityAllowanceRowsAsync(
        NpgsqlConnection connection, int month, int year, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id", "EmployeeId", "EmployeeCode", "EmployeeName", "PositionId", "PositionName", "GradeId", "GradeCode", "GradeName",
                   "Nam", "Thang", "ActualWorkDays", "StandardWorkDays", "AbcRating", "MonthlyPerformanceBonusAmount",
                   "StandardResponsibilityAllowanceAmount", "ActualResponsibilityAllowanceAmount", "IsLock",
                   "CalculatedAtUtc" AT TIME ZONE 'Asia/Ho_Chi_Minh', "CalculatedBy",
                   "UpdatedAtUtc" AT TIME ZONE 'Asia/Ho_Chi_Minh', "UpdatedBy", "LockedAtUtc" AT TIME ZONE 'Asia/Ho_Chi_Minh', "LockedBy", "GhiChu", "IsPerformanceBonusExcluded"
            FROM public.payroll_monthly_responsibility_allowance_abc
            WHERE "Nam" = @year AND "Thang" = @month
            ORDER BY "EmployeeId", "Id";
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("year", year); command.Parameters.AddWithValue("month", month);
        var rows = new List<PayrollResponsibilityAllowanceSourceRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PayrollResponsibilityAllowanceSourceRow(
                reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetGuid(4),
                reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetGuid(6), reader.IsDBNull(7) ? null : reader.GetString(7), reader.GetString(8),
                reader.GetInt32(9), reader.GetInt32(10), reader.GetDecimal(11), reader.GetDecimal(12), reader.GetString(13), reader.GetDecimal(14),
                reader.GetDecimal(15), reader.GetDecimal(16), reader.GetBoolean(17), reader.GetDateTime(18), reader.IsDBNull(19) ? null : reader.GetString(19),
                reader.IsDBNull(20) ? null : reader.GetDateTime(20), reader.IsDBNull(21) ? null : reader.GetString(21), reader.IsDBNull(22) ? null : reader.GetDateTime(22),
                reader.IsDBNull(23) ? null : reader.GetString(23), reader.IsDBNull(24) ? null : reader.GetString(24), reader.GetBoolean(25)));
        }
        return rows;
    }

    private static void ValidatePayrollResponsibilityAllowanceSourceRows(IReadOnlyList<PayrollResponsibilityAllowanceSourceRow> rows)
    {
        var duplicateEmployees = rows.GroupBy(row => row.EmployeeId).Where(group => group.Count() > 1).Select(group => group.Key).Take(10).ToArray();
        if (duplicateEmployees.Length > 0) throw new InvalidOperationException($"Nguồn phụ cấp trách nhiệm có nhiều dòng cho EmployeeId: {string.Join(", ", duplicateEmployees)}.");
        var invalid = rows.Select(row => row.EmployeeId == Guid.Empty ? "thiếu EmployeeId" : row.EmployeeCode.Length > 50 ? "EmployeeCode quá dài" : row.EmployeeName.Length > 200 ? "EmployeeName quá dài" : row.PositionName.Length > 200 ? "PositionName quá dài" : row.GradeName.Length > 200 ? "GradeName quá dài" : row.GradeCode?.Length > 50 ? "GradeCode quá dài" : row.AbcRating.Length > 10 ? "AbcRating quá dài" : row.ActualWorkDays < 0 || row.StandardWorkDays < 0 ? "ngày công âm" : row.MonthlyPerformanceBonusAmount < 0 || row.StandardResponsibilityAllowanceAmount < 0 || row.ActualResponsibilityAllowanceAmount < 0 ? "số tiền âm" : null).Where(reason => reason is not null).Take(10).ToArray();
        if (invalid.Length > 0) throw new InvalidOperationException($"Nguồn phụ cấp trách nhiệm không hợp lệ: {string.Join("; ", invalid)}.");
    }

    private static async Task<Dictionary<Guid, PayrollResponsibilitySummaryTargetRow>> ReadPayrollResponsibilitySummariesAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, int month, int year, IReadOnlyList<Guid> employeeIds, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT summary."Id", summary."EmployeeId", summary."IsLocked", department."DepartmentOrWorkshopName"
            FROM public.payroll_allowance_summary_records AS summary
            LEFT JOIN public.employees AS employee ON employee."Id" = summary."EmployeeId"
            LEFT JOIN public.departments AS department ON department."Id" = employee."DepartmentId"
            WHERE summary."PayrollMonth" = @month AND summary."PayrollYear" = @year AND summary."EmployeeId" = ANY(@employeeIds)
            FOR UPDATE OF summary;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("month", month); command.Parameters.AddWithValue("year", year); command.Parameters.AddWithValue("employeeIds", employeeIds.ToArray());
        var result = new Dictionary<Guid, PayrollResponsibilitySummaryTargetRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new PayrollResponsibilitySummaryTargetRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetBoolean(2), reader.IsDBNull(3) ? null : reader.GetString(3));
            if (!result.TryAdd(row.EmployeeId, row)) throw new InvalidOperationException($"Có nhiều bản ghi tổng hợp phụ cấp trách nhiệm cho EmployeeId '{row.EmployeeId}'.");
        }
        return result;
    }

    private static HashSet<Guid> FindMissingPayrollResponsibilityParents(IReadOnlyList<PayrollResponsibilityAllowanceSourceRow> sourceRows, IReadOnlyDictionary<Guid, PayrollResponsibilitySummaryTargetRow> summaries)
    {
        return sourceRows.Select(row => row.EmployeeId).Distinct().Where(id => !summaries.ContainsKey(id)).ToHashSet();
    }

    private static async Task<Dictionary<Guid, ExistingPayrollResponsibilityRow>> ReadExistingPayrollResponsibilityRowsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, IReadOnlyList<Guid> summaryIds, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "PayrollAllowanceSummaryRecordId", "EmployeeId", "EmployeeCode", "EmployeeName", "DepartmentName", "PositionId", "PositionName", "GradeId", "GradeCode", "GradeName", "Year", "Month", "ActualWorkDays", "StandardWorkDays", "AbcRating", "MonthlyPerformanceBonusAmount", "IsPerformanceBonusExcluded", "StandardResponsibilityAllowanceAmount", "ActualResponsibilityAllowanceAmount", "IsLocked", "CreatedAtUtc", "CalculatedAtUtc", "CalculatedBy", "UpdatedAtUtc", "UpdatedBy", "LockedAtUtc", "LockedBy", "Note"
            FROM public.payroll_allowance_responsibility_abc WHERE "PayrollAllowanceSummaryRecordId" = ANY(@summaryIds) FOR UPDATE;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction); command.Parameters.AddWithValue("summaryIds", summaryIds.ToArray());
        var result = new Dictionary<Guid, ExistingPayrollResponsibilityRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new ExistingPayrollResponsibilityRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetGuid(5), reader.GetString(6), reader.IsDBNull(7) ? null : reader.GetGuid(7), reader.IsDBNull(8) ? null : reader.GetString(8), reader.GetString(9), reader.GetInt32(10), reader.GetInt32(11), reader.GetDecimal(12), reader.GetDecimal(13), reader.IsDBNull(14) ? null : reader.GetString(14), reader.GetDecimal(15), reader.GetBoolean(16), reader.GetDecimal(17), reader.GetDecimal(18), reader.GetBoolean(19), reader.GetDateTime(20), reader.IsDBNull(21) ? null : reader.GetDateTime(21), reader.IsDBNull(22) ? null : reader.GetString(22), reader.IsDBNull(23) ? null : reader.GetDateTime(23), reader.IsDBNull(24) ? null : reader.GetString(24), reader.IsDBNull(25) ? null : reader.GetDateTime(25), reader.IsDBNull(26) ? null : reader.GetString(26), reader.IsDBNull(27) ? null : reader.GetString(27));
            if (!result.TryAdd(row.SummaryId, row)) throw new InvalidOperationException($"Có nhiều dòng chi tiết phụ cấp trách nhiệm cho SummaryId '{row.SummaryId}'.");
        }
        return result;
    }

    private static async Task<HashSet<Guid>> ReadAvailablePayrollResponsibilityGradeIdsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, IReadOnlyList<Guid> gradeIds, CancellationToken cancellationToken)
    {
        if (gradeIds.Count == 0) return [];
        const string sql = "SELECT \"Id\" FROM public.payroll_allowance_responsibility_grade WHERE \"Id\" = ANY(@gradeIds);";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gradeIds", gradeIds.ToArray());
        var result = new HashSet<Guid>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(reader.GetGuid(0));
        return result;
    }

    private static bool HasSamePayrollResponsibilityRow(ExistingPayrollResponsibilityRow existing, PayrollResponsibilityAllowanceSourceRow source, string? departmentName) =>
        existing.EmployeeId == source.EmployeeId && existing.EmployeeCode == source.EmployeeCode && existing.EmployeeName == source.EmployeeName && existing.DepartmentName == departmentName && existing.PositionId == source.PositionId && existing.PositionName == source.PositionName && existing.GradeId == source.GradeId && existing.GradeCode == NormalizeOptionalText(source.GradeCode) && existing.GradeName == source.GradeName && existing.Year == source.Year && existing.Month == source.Month && existing.ActualWorkDays == source.ActualWorkDays && existing.StandardWorkDays == source.StandardWorkDays && existing.AbcRating == NormalizeOptionalText(source.AbcRating) && existing.MonthlyPerformanceBonusAmount == source.MonthlyPerformanceBonusAmount && existing.IsPerformanceBonusExcluded == source.IsPerformanceBonusExcluded && existing.StandardResponsibilityAllowanceAmount == source.StandardResponsibilityAllowanceAmount && existing.ActualResponsibilityAllowanceAmount == source.ActualResponsibilityAllowanceAmount && existing.IsLocked == source.IsLocked && existing.CreatedAtUtc == source.CalculatedAtUtc && existing.CalculatedAtUtc == source.CalculatedAtUtc && existing.CalculatedBy == NormalizeOptionalText(source.CalculatedBy) && existing.UpdatedAtUtc == source.UpdatedAtUtc && existing.UpdatedBy == NormalizeOptionalText(source.UpdatedBy) && existing.LockedAtUtc == source.LockedAtUtc && existing.LockedBy == NormalizeOptionalText(source.LockedBy) && existing.Note == NormalizeOptionalText(source.Note);

    private static async Task InsertPayrollResponsibilityRowAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, PayrollResponsibilityAllowanceSourceRow source, PayrollResponsibilitySummaryTargetRow summary, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO public.payroll_allowance_responsibility_abc ("Id", "PayrollAllowanceSummaryRecordId", "EmployeeId", "EmployeeCode", "EmployeeName", "DepartmentName", "PositionId", "PositionName", "GradeId", "GradeCode", "GradeName", "Year", "Month", "ActualWorkDays", "StandardWorkDays", "AbcRating", "MonthlyPerformanceBonusAmount", "IsPerformanceBonusExcluded", "StandardResponsibilityAllowanceAmount", "ActualResponsibilityAllowanceAmount", "IsLocked", "CreatedAtUtc", "CalculatedAtUtc", "CalculatedBy", "UpdatedAtUtc", "UpdatedBy", "LockedAtUtc", "LockedBy", "Note")
            VALUES (@id, @summaryId, @employeeId, @employeeCode, @employeeName, @departmentName, @positionId, @positionName, @gradeId, @gradeCode, @gradeName, @year, @month, @actualWorkDays, @standardWorkDays, @abcRating, @monthlyBonus, @isExcluded, @standardAmount, @actualAmount, @isLocked, @createdAtUtc, @calculatedAtUtc, @calculatedBy, @updatedAtUtc, @updatedBy, @lockedAtUtc, @lockedBy, @note);
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction); AddPayrollResponsibilityParameters(command, source, summary); command.Parameters.AddWithValue("id", Guid.NewGuid()); await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdatePayrollResponsibilityRowAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, PayrollResponsibilityAllowanceSourceRow source, PayrollResponsibilitySummaryTargetRow summary, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE public.payroll_allowance_responsibility_abc SET "EmployeeId"=@employeeId,"EmployeeCode"=@employeeCode,"EmployeeName"=@employeeName,"DepartmentName"=@departmentName,"PositionId"=@positionId,"PositionName"=@positionName,"GradeId"=@gradeId,"GradeCode"=@gradeCode,"GradeName"=@gradeName,"Year"=@year,"Month"=@month,"ActualWorkDays"=@actualWorkDays,"StandardWorkDays"=@standardWorkDays,"AbcRating"=@abcRating,"MonthlyPerformanceBonusAmount"=@monthlyBonus,"IsPerformanceBonusExcluded"=@isExcluded,"StandardResponsibilityAllowanceAmount"=@standardAmount,"ActualResponsibilityAllowanceAmount"=@actualAmount,"IsLocked"=@isLocked,"CreatedAtUtc"=@createdAtUtc,"CalculatedAtUtc"=@calculatedAtUtc,"CalculatedBy"=@calculatedBy,"UpdatedAtUtc"=@updatedAtUtc,"UpdatedBy"=@updatedBy,"LockedAtUtc"=@lockedAtUtc,"LockedBy"=@lockedBy,"Note"=@note WHERE "PayrollAllowanceSummaryRecordId"=@summaryId;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction); AddPayrollResponsibilityParameters(command, source, summary); await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddPayrollResponsibilityParameters(NpgsqlCommand command, PayrollResponsibilityAllowanceSourceRow source, PayrollResponsibilitySummaryTargetRow summary)
    {
        command.Parameters.AddWithValue("summaryId", summary.Id); command.Parameters.AddWithValue("employeeId", source.EmployeeId); command.Parameters.AddWithValue("employeeCode", source.EmployeeCode); command.Parameters.AddWithValue("employeeName", source.EmployeeName); command.Parameters.AddWithValue("departmentName", (object?)summary.DepartmentName ?? DBNull.Value); command.Parameters.AddWithValue("positionId", (object?)source.PositionId ?? DBNull.Value); command.Parameters.AddWithValue("positionName", source.PositionName); command.Parameters.AddWithValue("gradeId", (object?)source.GradeId ?? DBNull.Value); command.Parameters.AddWithValue("gradeCode", (object?)NormalizeOptionalText(source.GradeCode) ?? DBNull.Value); command.Parameters.AddWithValue("gradeName", source.GradeName); command.Parameters.AddWithValue("year", source.Year); command.Parameters.AddWithValue("month", source.Month); command.Parameters.AddWithValue("actualWorkDays", source.ActualWorkDays); command.Parameters.AddWithValue("standardWorkDays", source.StandardWorkDays); command.Parameters.AddWithValue("abcRating", (object?)NormalizeOptionalText(source.AbcRating) ?? DBNull.Value); command.Parameters.AddWithValue("monthlyBonus", source.MonthlyPerformanceBonusAmount); command.Parameters.AddWithValue("isExcluded", source.IsPerformanceBonusExcluded); command.Parameters.AddWithValue("standardAmount", source.StandardResponsibilityAllowanceAmount); command.Parameters.AddWithValue("actualAmount", source.ActualResponsibilityAllowanceAmount); command.Parameters.AddWithValue("isLocked", source.IsLocked); command.Parameters.AddWithValue("createdAtUtc", source.CalculatedAtUtc); command.Parameters.AddWithValue("calculatedAtUtc", source.CalculatedAtUtc); command.Parameters.AddWithValue("calculatedBy", (object?)NormalizeOptionalText(source.CalculatedBy) ?? DBNull.Value); command.Parameters.AddWithValue("updatedAtUtc", (object?)source.UpdatedAtUtc ?? DBNull.Value); command.Parameters.AddWithValue("updatedBy", (object?)NormalizeOptionalText(source.UpdatedBy) ?? DBNull.Value); command.Parameters.AddWithValue("lockedAtUtc", (object?)source.LockedAtUtc ?? DBNull.Value); command.Parameters.AddWithValue("lockedBy", (object?)NormalizeOptionalText(source.LockedBy) ?? DBNull.Value); command.Parameters.AddWithValue("note", (object?)NormalizeOptionalText(source.Note) ?? DBNull.Value);
    }

    private static async Task<int> SynchronizePayrollResponsibilitySummaryTotalsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, IReadOnlySet<Guid> summaryIds, DateTime updatedAtUtc, CancellationToken cancellationToken)
    {
        if (summaryIds.Count == 0) return 0;
        const string sql = """
            WITH totals AS (SELECT summary."Id", COALESCE(SUM(detail."ActualResponsibilityAllowanceAmount"),0) AS "TotalAmount" FROM public.payroll_allowance_summary_records summary LEFT JOIN public.payroll_allowance_responsibility_abc detail ON detail."PayrollAllowanceSummaryRecordId"=summary."Id" WHERE summary."Id"=ANY(@summaryIds) GROUP BY summary."Id")
            UPDATE public.payroll_allowance_summary_records summary SET "ResponsibilityAllowanceAmount"=totals."TotalAmount","UpdatedAtUtc"=@updatedAtUtc,"UpdatedBy"='postgres-sync' FROM totals WHERE summary."Id"=totals."Id" AND summary."ResponsibilityAllowanceAmount" IS DISTINCT FROM totals."TotalAmount";
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction); command.Parameters.AddWithValue("summaryIds", summaryIds.ToArray()); command.Parameters.AddWithValue("updatedAtUtc", updatedAtUtc); return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<int> SyncTableAsync(
        NpgsqlConnection sourceConnection,
        NpgsqlConnection targetConnection,
        PostgresTableSyncOptions table,
        IReadOnlyDictionary<string, string>? tokens,
        CancellationToken cancellationToken)
    {
        var tableName = ResolveTableName(table);
        var sourceQuery = ResolveSourceQuery(table, tableName);
        var targetTable = ResolveTargetTable(table, tableName);
        sourceQuery = ResolveTokens(sourceQuery, tokens);
        var targetSetupSql = ResolveTokens(table.TargetSetupSql, tokens);

        if (string.Equals(tableName, "public.attendance_logs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tableName, "public.payroll_monthly_salary_rates", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Resolved source query for {TableName}: {SourceQuery}", tableName, sourceQuery);
        }

        _logger.LogInformation(
            "Starting table sync. Name={TableName}; Target={TargetTable}.",
            tableName,
            targetTable);

        var stopwatch = Stopwatch.StartNew();

        await using var sourceCommand = new NpgsqlCommand(sourceQuery, sourceConnection)
        {
            CommandTimeout = table.CommandTimeoutSeconds
        };

        await using var sourceReader = await sourceCommand.ExecuteReaderAsync(cancellationToken);
        var resolvedColumns = ResolveColumns(sourceReader, table);
        ValidateConflictKeys(table.ConflictKeys, resolvedColumns);

        var insertCommandText = BuildInsertCommandText(
            targetTable,
            resolvedColumns.Select(column => column.TargetColumn).ToArray(),
            table.ConflictKeys);

        await sourceReader.CloseAsync();

        await using var targetTransaction = await targetConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            await ExecuteTargetSetupSqlAsync(
                targetConnection,
                targetTransaction,
                tableName,
                targetTable,
                targetSetupSql,
                table.CommandTimeoutSeconds,
                cancellationToken);

            if (table.ClearTargetBeforeInsert)
            {
                await using var clearCommand = new NpgsqlCommand(
                    $"DELETE FROM {SqlIdentifier.QuoteQualifiedIdentifier(targetTable)};",
                    targetConnection,
                    targetTransaction)
                {
                    CommandTimeout = table.CommandTimeoutSeconds
                };

                await clearCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var insertCommand = CreateInsertCommand(
                targetConnection,
                targetTransaction,
                insertCommandText,
                resolvedColumns,
                table.CommandTimeoutSeconds);

            await using var reloadCommand = new NpgsqlCommand(sourceQuery, sourceConnection)
            {
                CommandTimeout = table.CommandTimeoutSeconds
            };

            await using var reloadReader = await reloadCommand.ExecuteReaderAsync(cancellationToken);

            var rowCount = IsDevicesTable(tableName)
                ? await SyncDevicesAsync(
                    reloadReader,
                    resolvedColumns,
                    insertCommand,
                    targetConnection,
                    targetTransaction,
                    table.CommandTimeoutSeconds,
                    cancellationToken)
                : await SyncStandardRowsAsync(
                    tableName,
                    reloadReader,
                    resolvedColumns,
                    insertCommand,
                    cancellationToken);

            await targetTransaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Completed table sync. Name={TableName}; Rows={RowCount}; ElapsedMs={ElapsedMs}.",
                tableName,
                rowCount,
                stopwatch.ElapsedMilliseconds);

            return rowCount;
        }
        catch
        {
            await targetTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<int> SyncStandardRowsAsync(
        string tableName,
        NpgsqlDataReader reloadReader,
        IReadOnlyList<ResolvedColumn> resolvedColumns,
        NpgsqlCommand insertCommand,
        CancellationToken cancellationToken)
    {
        var rowCount = 0;
        while (await reloadReader.ReadAsync(cancellationToken))
        {
            for (var index = 0; index < resolvedColumns.Count; index++)
            {
                insertCommand.Parameters[index].Value = ResolveParameterValue(
                    tableName,
                    resolvedColumns[index],
                    reloadReader);
            }

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            rowCount++;
        }

        return rowCount;
    }

    private static (int Month, int Year) GetPreviousPayrollPeriod(int month, int year)
    {
        return month == 1 ? (12, year - 1) : (month - 1, year);
    }

    private static bool HasSamePayrollBasicSalary(
        ExistingPayrollBasicSalaryRow targetRow,
        PayrollBasicSalarySnapshot sourceRow)
    {
        return targetRow.BasicSalary == sourceRow.BasicSalary
            && targetRow.StandardWorkingDays == sourceRow.StandardWorkingDays
            && targetRow.DailySalary == sourceRow.DailySalary
            && targetRow.HourlySalary == sourceRow.HourlySalary;
    }

    private static async Task<List<PayrollBasicSalarySnapshot>> ReadPayrollBasicSalaryRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int sourceMonth,
        int sourceYear,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT salary."EmployeeId",
                   salary."BasicSalary",
                   salary."StandardWorkingDays",
                   salary."DailySalary",
                   salary."HourlySalary"
            FROM public.payroll_basic_salary_records AS salary
            INNER JOIN public.employees AS employee
                ON employee."Id" = salary."EmployeeId"
            WHERE COALESCE(employee."IsDeleted", FALSE) = FALSE
              AND salary."PayrollMonth" = @sourceMonth
              AND salary."PayrollYear" = @sourceYear
            ORDER BY salary."EmployeeId";
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("sourceMonth", sourceMonth);
        command.Parameters.AddWithValue("sourceYear", sourceYear);

        var rows = new List<PayrollBasicSalarySnapshot>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PayrollBasicSalarySnapshot(
                reader.GetGuid(0),
                reader.GetDecimal(1),
                reader.GetDecimal(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4)));
        }

        return rows;
    }

    private static async Task<Dictionary<Guid, ExistingPayrollBasicSalaryRow>> ReadExistingPayrollBasicSalaryRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int targetMonth,
        int targetYear,
        IReadOnlyList<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id",
                   "EmployeeId",
                   "BasicSalary",
                   "StandardWorkingDays",
                   "DailySalary",
                   "HourlySalary"
            FROM public.payroll_basic_salary_records
            WHERE "PayrollMonth" = @targetMonth
              AND "PayrollYear" = @targetYear
              AND "EmployeeId" = ANY(@employeeIds);
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("targetMonth", targetMonth);
        command.Parameters.AddWithValue("targetYear", targetYear);
        command.Parameters.AddWithValue("employeeIds", employeeIds.ToArray());

        var rows = new Dictionary<Guid, ExistingPayrollBasicSalaryRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new ExistingPayrollBasicSalaryRow(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetDecimal(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4),
                reader.GetDecimal(5));
            rows[row.EmployeeId] = row;
        }

        return rows;
    }

    private static async Task InsertPayrollBasicSalaryRowAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int targetMonth,
        int targetYear,
        PayrollBasicSalarySnapshot sourceRow,
        DateTime synchronizedAtUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO public.payroll_basic_salary_records
            (
                "Id",
                "EmployeeId",
                "PayrollMonth",
                "PayrollYear",
                "BasicSalary",
                "StandardWorkingDays",
                "DailySalary",
                "HourlySalary",
                "CreatedAtUtc",
                "UpdatedAtUtc"
            )
            VALUES
            (
                @id,
                @employeeId,
                @targetMonth,
                @targetYear,
                @basicSalary,
                @standardWorkingDays,
                @dailySalary,
                @hourlySalary,
                @createdAtUtc,
                @updatedAtUtc
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("employeeId", sourceRow.EmployeeId);
        command.Parameters.AddWithValue("targetMonth", targetMonth);
        command.Parameters.AddWithValue("targetYear", targetYear);
        command.Parameters.AddWithValue("basicSalary", sourceRow.BasicSalary);
        command.Parameters.AddWithValue("standardWorkingDays", sourceRow.StandardWorkingDays);
        command.Parameters.AddWithValue("dailySalary", sourceRow.DailySalary);
        command.Parameters.AddWithValue("hourlySalary", sourceRow.HourlySalary);
        command.Parameters.AddWithValue("createdAtUtc", synchronizedAtUtc);
        command.Parameters.AddWithValue("updatedAtUtc", synchronizedAtUtc);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdatePayrollBasicSalaryRowAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid id,
        PayrollBasicSalarySnapshot sourceRow,
        DateTime synchronizedAtUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE public.payroll_basic_salary_records
            SET "BasicSalary" = @basicSalary,
                "StandardWorkingDays" = @standardWorkingDays,
                "DailySalary" = @dailySalary,
                "HourlySalary" = @hourlySalary,
                "UpdatedAtUtc" = @updatedAtUtc
            WHERE "Id" = @id;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("basicSalary", sourceRow.BasicSalary);
        command.Parameters.AddWithValue("standardWorkingDays", sourceRow.StandardWorkingDays);
        command.Parameters.AddWithValue("dailySalary", sourceRow.DailySalary);
        command.Parameters.AddWithValue("hourlySalary", sourceRow.HourlySalary);
        command.Parameters.AddWithValue("updatedAtUtc", synchronizedAtUtc);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ExecuteTargetSetupSqlAsync(
        NpgsqlConnection targetConnection,
        NpgsqlTransaction targetTransaction,
        string tableName,
        string targetTable,
        string targetSetupSql,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetSetupSql))
        {
            return;
        }

        _logger.LogInformation(
            "Preparing target table before sync. Name={TableName}; Target={TargetTable}.",
            tableName,
            targetTable);

        await using var setupCommand = new NpgsqlCommand(
            targetSetupSql,
            targetConnection,
            targetTransaction)
        {
            CommandTimeout = commandTimeoutSeconds
        };

        await setupCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static NpgsqlCommand CreateInsertCommand(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string commandText,
        IReadOnlyList<ResolvedColumn> columns,
        int commandTimeoutSeconds)
    {
        var command = new NpgsqlCommand(commandText, connection, transaction)
        {
            CommandTimeout = commandTimeoutSeconds
        };

        for (var index = 0; index < columns.Count; index++)
        {
            command.Parameters.Add(new NpgsqlParameter
            {
                ParameterName = $"p{index}",
                DataTypeName = columns[index].DataTypeName,
                Value = DBNull.Value
            });
        }

        return command;
    }

    private async Task<int> SyncDevicesAsync(
        NpgsqlDataReader reloadReader,
        IReadOnlyList<ResolvedColumn> resolvedColumns,
        NpgsqlCommand insertCommand,
        NpgsqlConnection targetConnection,
        NpgsqlTransaction targetTransaction,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        var deviceRows = await LoadDeviceRowsAsync(reloadReader, resolvedColumns, cancellationToken);
        if (deviceRows.Count == 0)
        {
            return 0;
        }

        var targetDeviceIdBySerial = await LoadTargetDeviceIdBySerialAsync(
            targetConnection,
            targetTransaction,
            commandTimeoutSeconds,
            cancellationToken);
        var rowsToUpsert = BuildCanonicalDeviceRows(deviceRows, targetDeviceIdBySerial);

        foreach (var rowValues in rowsToUpsert)
        {
            for (var index = 0; index < rowValues.Length; index++)
            {
                insertCommand.Parameters[index].Value = rowValues[index];
            }

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        return rowsToUpsert.Count;
    }

    private async Task<List<DeviceSyncRow>> LoadDeviceRowsAsync(
        NpgsqlDataReader reloadReader,
        IReadOnlyList<ResolvedColumn> resolvedColumns,
        CancellationToken cancellationToken)
    {
        var deviceColumnIndexes = ResolveDeviceColumnIndexes(resolvedColumns);
        var idColumnIndex = deviceColumnIndexes.Id;
        var serialColumnIndex = deviceColumnIndexes.SerialNumber;

        if (idColumnIndex < 0 || serialColumnIndex < 0)
        {
            throw new InvalidOperationException(
                "Device sync requires mapped target columns 'Id' and 'SerialNumber'.");
        }

        var rows = new List<DeviceSyncRow>();
        while (await reloadReader.ReadAsync(cancellationToken))
        {
            var values = new object?[resolvedColumns.Count];
            for (var index = 0; index < resolvedColumns.Count; index++)
            {
                var column = resolvedColumns[index];
                values[index] = reloadReader.IsDBNull(column.Ordinal)
                    ? DBNull.Value
                    : reloadReader.GetValue(column.Ordinal);
            }

            var sourceId = GetRequiredGuid(values[idColumnIndex], "devices.Id");
            var originalSerial = ConvertToNullableString(values[serialColumnIndex]);
            var normalizedSerial = NormalizeDeviceSerial(originalSerial);
            values[serialColumnIndex] = ToDbValue(normalizedSerial);

            rows.Add(new DeviceSyncRow(
                sourceId,
                originalSerial,
                normalizedSerial,
                values,
                deviceColumnIndexes));
        }

        return rows;
    }

    private async Task<Dictionary<string, Guid>> LoadTargetDeviceIdBySerialAsync(
        NpgsqlConnection targetConnection,
        NpgsqlTransaction targetTransaction,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        var targetDeviceIdBySerial = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        await using var command = new NpgsqlCommand(
            """
            SELECT "Id", "SerialNumber"
            FROM public.devices
            WHERE "SerialNumber" IS NOT NULL
              AND btrim("SerialNumber") <> '';
            """,
            targetConnection,
            targetTransaction)
        {
            CommandTimeout = commandTimeoutSeconds
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var deviceId = reader.GetGuid(0);
            var normalizedSerial = NormalizeDeviceSerial(reader.GetValue(1));
            if (string.IsNullOrWhiteSpace(normalizedSerial))
            {
                continue;
            }

            targetDeviceIdBySerial[normalizedSerial] = deviceId;
        }

        return targetDeviceIdBySerial;
    }

    private List<object?[]> BuildCanonicalDeviceRows(
        IReadOnlyList<DeviceSyncRow> deviceRows,
        IDictionary<string, Guid> targetDeviceIdBySerial)
    {
        var rowsToUpsert = new List<object?[]>(deviceRows.Count);
        var duplicateGroupCount = 0;
        var normalizedDeviceCount = deviceRows.Count(static row => row.IsSerialNormalized);
        var remappedSourceDeviceCount = 0;
        var reusedTargetDeviceCount = 0;

        foreach (var row in deviceRows.Where(static row => string.IsNullOrWhiteSpace(row.NormalizedSerial)))
        {
            _deviceIdMappings[row.SourceId] = row.SourceId;
            rowsToUpsert.Add(row.Values);
        }

        foreach (var group in deviceRows
                     .Where(static row => !string.IsNullOrWhiteSpace(row.NormalizedSerial))
                     .GroupBy(static row => row.NormalizedSerial!, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() > 1)
            {
                duplicateGroupCount++;
            }

            var keeper = SelectDeviceKeeper(group);
            var targetDeviceId = targetDeviceIdBySerial.TryGetValue(group.Key, out var existingTargetDeviceId)
                ? existingTargetDeviceId
                : keeper.SourceId;

            if (targetDeviceId != keeper.SourceId)
            {
                reusedTargetDeviceCount++;
            }

            keeper.Values[keeper.IdColumnIndex] = targetDeviceId;
            rowsToUpsert.Add(keeper.Values);

            foreach (var row in group)
            {
                if (row.SourceId != targetDeviceId)
                {
                    remappedSourceDeviceCount++;
                }

                _deviceIdMappings[row.SourceId] = targetDeviceId;
            }
        }

        _logger.LogInformation(
            "Prepared canonical device rows. SourceRows={SourceRows}; UpsertRows={UpsertRows}; NormalizedSerialRows={NormalizedSerialRows}; DuplicateSerialGroups={DuplicateSerialGroups}; RemappedSourceDeviceIds={RemappedSourceDeviceIds}; ReusedTargetDeviceIds={ReusedTargetDeviceIds}.",
            deviceRows.Count,
            rowsToUpsert.Count,
            normalizedDeviceCount,
            duplicateGroupCount,
            remappedSourceDeviceCount,
            reusedTargetDeviceCount);

        return rowsToUpsert;
    }

    private static DeviceSyncRow SelectDeviceKeeper(IEnumerable<DeviceSyncRow> duplicateGroup) =>
        duplicateGroup
            .OrderByDescending(static row => row.IsInUse)
            .ThenByDescending(static row => row.LastActivityAt)
            .ThenByDescending(static row => row.CompletenessScore)
            .ThenByDescending(static row => row.UpdatedAtUtc ?? row.CreatedAtUtc)
            .ThenByDescending(static row => row.CreatedAtUtc)
            .ThenByDescending(static row => row.SourceId)
            .First();

    private object ResolveParameterValue(
        string tableName,
        ResolvedColumn column,
        NpgsqlDataReader reloadReader)
    {
        if (reloadReader.IsDBNull(column.Ordinal))
        {
            return DBNull.Value;
        }

        var value = reloadReader.GetValue(column.Ordinal);
        if (IsAttendanceLogsTable(tableName)
            && string.Equals(column.TargetColumn, "DeviceId", StringComparison.OrdinalIgnoreCase)
            && TryGetGuid(value, out var sourceDeviceId)
            && _deviceIdMappings.TryGetValue(sourceDeviceId, out var targetDeviceId))
        {
            return targetDeviceId;
        }

        return value;
    }

    private static int FindColumnIndex(
        IReadOnlyList<ResolvedColumn> resolvedColumns,
        string targetColumn)
    {
        for (var index = 0; index < resolvedColumns.Count; index++)
        {
            if (string.Equals(resolvedColumns[index].TargetColumn, targetColumn, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static DeviceColumnIndexes ResolveDeviceColumnIndexes(
        IReadOnlyList<ResolvedColumn> resolvedColumns) =>
        new(
            FindColumnIndex(resolvedColumns, "Id"),
            FindColumnIndex(resolvedColumns, "SerialNumber"),
            FindColumnIndex(resolvedColumns, "Name"),
            FindColumnIndex(resolvedColumns, "IpAddress"),
            FindColumnIndex(resolvedColumns, "ActivationCode"),
            FindColumnIndex(resolvedColumns, "VendorName"),
            FindColumnIndex(resolvedColumns, "DeviceModel"),
            FindColumnIndex(resolvedColumns, "MacAddress"),
            FindColumnIndex(resolvedColumns, "Location"),
            FindColumnIndex(resolvedColumns, "IsInUse"),
            FindColumnIndex(resolvedColumns, "LastRequestTime"),
            FindColumnIndex(resolvedColumns, "CreatedAtUtc"),
            FindColumnIndex(resolvedColumns, "UpdatedAtUtc"));

    private static Guid GetRequiredGuid(object? value, string columnName)
    {
        if (TryGetGuid(value, out var guid))
        {
            return guid;
        }

        throw new InvalidOperationException($"Column '{columnName}' must contain a valid GUID value.");
    }

    private static bool TryGetGuid(object? value, out Guid guid)
    {
        switch (value)
        {
            case Guid typedGuid:
                guid = typedGuid;
                return true;

            case string text when Guid.TryParse(text, out var parsedGuid):
                guid = parsedGuid;
                return true;

            default:
                guid = Guid.Empty;
                return false;
        }
    }

    private static string? NormalizeDeviceSerial(object? value)
    {
        var rawValue = ConvertToNullableString(value);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var normalized = new string(rawValue
            .Trim()
            .ToUpperInvariant()
            .Where(static character => character is >= 'A' and <= 'Z' or >= '0' and <= '9')
            .ToArray());

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? ConvertToNullableString(object? value)
    {
        if (value is null || value is DBNull)
        {
            return null;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static object ToDbValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    private static bool IsDevicesTable(string tableName) =>
        string.Equals(tableName, DevicesTableName, StringComparison.OrdinalIgnoreCase);

    private static bool IsAttendanceLogsTable(string tableName) =>
        string.Equals(tableName, AttendanceLogsTableName, StringComparison.OrdinalIgnoreCase);

    private static List<ResolvedColumn> ResolveColumns(
        NpgsqlDataReader sourceReader,
        PostgresTableSyncOptions table)
    {
        var availableOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var ordinal = 0; ordinal < sourceReader.FieldCount; ordinal++)
        {
            availableOrdinals[sourceReader.GetName(ordinal)] = ordinal;
        }

        if (table.ColumnMappings.Count == 0)
        {
            var fallbackColumns = new List<ResolvedColumn>(sourceReader.FieldCount);
            for (var ordinal = 0; ordinal < sourceReader.FieldCount; ordinal++)
            {
                var columnName = sourceReader.GetName(ordinal);
                fallbackColumns.Add(new ResolvedColumn(
                    ordinal,
                    columnName,
                    columnName,
                    sourceReader.GetDataTypeName(ordinal)));
            }

            return fallbackColumns;
        }

        var resolvedColumns = new List<ResolvedColumn>(table.ColumnMappings.Count);
        foreach (var mapping in table.ColumnMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.SourceColumn))
            {
                throw new InvalidOperationException(
                    $"Table mapping '{table.Name}' contains an empty SourceColumn entry.");
            }

            if (!availableOrdinals.TryGetValue(mapping.SourceColumn, out var ordinal))
            {
                throw new InvalidOperationException(
                    $"Source query for table mapping '{table.Name}' does not contain column '{mapping.SourceColumn}'.");
            }

            var targetColumn = string.IsNullOrWhiteSpace(mapping.TargetColumn)
                ? mapping.SourceColumn
                : mapping.TargetColumn;

            resolvedColumns.Add(new ResolvedColumn(
                ordinal,
                mapping.SourceColumn,
                targetColumn,
                sourceReader.GetDataTypeName(ordinal)));
        }

        return resolvedColumns;
    }

    private static void ValidateConflictKeys(
        IReadOnlyList<string> conflictKeys,
        IReadOnlyList<ResolvedColumn> resolvedColumns)
    {
        if (conflictKeys.Count == 0)
        {
            return;
        }

        var targetColumns = resolvedColumns
            .Select(column => column.TargetColumn)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var conflictKey in conflictKeys)
        {
            if (!targetColumns.Contains(conflictKey))
            {
                throw new InvalidOperationException(
                    $"Conflict key '{conflictKey}' is not present in the mapped target columns.");
            }
        }
    }

    private static string BuildInsertCommandText(
        string targetTable,
        IReadOnlyList<string> targetColumns,
        IReadOnlyList<string> conflictKeys)
    {
        if (targetColumns.Count == 0)
        {
            throw new InvalidOperationException("At least one target column is required for PostgreSQL sync.");
        }

        var quotedColumns = targetColumns
            .Select(SqlIdentifier.QuoteIdentifier)
            .ToArray();
        var parameterNames = Enumerable.Range(0, targetColumns.Count)
            .Select(index => $"@p{index}")
            .ToArray();

        var commandText =
            $"INSERT INTO {SqlIdentifier.QuoteQualifiedIdentifier(targetTable)} ({string.Join(", ", quotedColumns)}) " +
            $"VALUES ({string.Join(", ", parameterNames)})";

        if (conflictKeys.Count == 0)
        {
            return $"{commandText};";
        }

        var quotedConflictKeys = conflictKeys
            .Select(SqlIdentifier.QuoteIdentifier)
            .ToArray();
        var nonKeyColumns = targetColumns
            .Where(column =>
                !conflictKeys.Contains(column, StringComparer.OrdinalIgnoreCase)
                && !string.Equals(column, "Id", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (nonKeyColumns.Length == 0)
        {
            return $"{commandText} ON CONFLICT ({string.Join(", ", quotedConflictKeys)}) DO NOTHING;";
        }

        var setClauses = nonKeyColumns
            .Select(column =>
            {
                var quotedColumn = SqlIdentifier.QuoteIdentifier(column);
                return $"{quotedColumn} = EXCLUDED.{quotedColumn}";
            })
            .ToArray();

        return
            $"{commandText} ON CONFLICT ({string.Join(", ", quotedConflictKeys)}) " +
            $"DO UPDATE SET {string.Join(", ", setClauses)};";
    }

    private static void ValidateWorkerOptions(
        PostgresSyncOptions options,
        string sourceConnectionString,
        string targetConnectionString)
    {
        if (string.IsNullOrWhiteSpace(sourceConnectionString))
        {
            throw new InvalidOperationException(
                "Missing source PostgreSQL connection string. Configure PostgresSync:SourceConnectionString, ConnectionStrings:SourcePostgres, or VNTA_POSTGRES_SYNC_SOURCE.");
        }

        if (string.IsNullOrWhiteSpace(targetConnectionString))
        {
            throw new InvalidOperationException(
                "Missing target PostgreSQL connection string. Configure PostgresSync:TargetConnectionString, ConnectionStrings:TargetPostgres, or VNTA_POSTGRES_SYNC_TARGET.");
        }

        if (options.PollIntervalSeconds < 1)
        {
            throw new InvalidOperationException("PostgresSync:PollIntervalSeconds must be greater than or equal to 1.");
        }
    }

    private string ResolveConnectionString(
        string? configuredValue,
        string connectionStringName,
        string environmentVariableName)
    {
        return string.IsNullOrWhiteSpace(configuredValue)
            ? _configuration.GetConnectionString(connectionStringName)
                ?? Environment.GetEnvironmentVariable(environmentVariableName)
                ?? string.Empty
            : configuredValue
            ;
    }

    private string ResolveTargetConnectionString(string? configuredValue)
    {
        var connectionString = ResolveConnectionString(
            configuredValue,
            "TargetPostgres",
            "VNTA_POSTGRES_SYNC_TARGET");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            JifengHrmTargetDatabaseValidator.Validate(connectionString);
        }

        return connectionString;
    }

    private static string ResolveSourceQuery(PostgresTableSyncOptions table, string tableName)
    {
        if (!string.IsNullOrWhiteSpace(table.SourceQuery))
        {
            return table.SourceQuery;
        }

        return $"SELECT * FROM {SqlIdentifier.QuoteQualifiedIdentifier(tableName)};";
    }

    private static string ResolveTargetTable(PostgresTableSyncOptions table, string tableName)
    {
        return string.IsNullOrWhiteSpace(table.TargetTable)
            ? tableName
            : table.TargetTable;
    }

    private static string ResolveTableName(PostgresTableSyncOptions table)
    {
        if (string.IsNullOrWhiteSpace(table.Name) && string.IsNullOrWhiteSpace(table.TargetTable))
        {
            throw new InvalidOperationException(
                "Each PostgreSQL table mapping must define Name or TargetTable.");
        }

        return string.IsNullOrWhiteSpace(table.Name)
            ? table.TargetTable
            : table.Name;
    }

    private static string ResolveTokens(
        string sql,
        IReadOnlyDictionary<string, string>? tokens)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);

        var resolvedSql = sql;

        if (tokens is not null)
        {
            foreach (var token in tokens)
            {
                resolvedSql = resolvedSql.Replace(
                    $"{{{{{token.Key}}}}}",
                    token.Value,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        return resolvedSql
            .Replace("{{today}}", today.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{yesterday}}", yesterday.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{tomorrow}}", tomorrow.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{today_start}}", $"{today:yyyy-MM-dd} 00:00:00", StringComparison.OrdinalIgnoreCase)
            .Replace("{{tomorrow_start}}", $"{tomorrow:yyyy-MM-dd} 00:00:00", StringComparison.OrdinalIgnoreCase)
            .Replace("{{yesterday_start}}", $"{yesterday:yyyy-MM-dd} 00:00:00", StringComparison.OrdinalIgnoreCase)
            .Replace("{{from_date}}", today.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{to_date}}", today.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{to_exclusive_date}}", tomorrow.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{from_start}}", $"{today:yyyy-MM-dd} 00:00:00", StringComparison.OrdinalIgnoreCase)
            .Replace("{{to_exclusive_start}}", $"{tomorrow:yyyy-MM-dd} 00:00:00", StringComparison.OrdinalIgnoreCase)
            .Replace("{{family_filter}}", "TRUE", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct ResolvedColumn(
        int Ordinal,
        string SourceColumn,
        string TargetColumn,
        string DataTypeName);

    public readonly record struct PayrollBasicSalarySyncResult(
        int SourceMonth,
        int SourceYear,
        int TargetMonth,
        int TargetYear,
        int SourceRecordCount,
        int CreatedRecordCount,
        int UpdatedRecordCount,
        int UnchangedRecordCount,
        DateTime SynchronizedAtUtc);

    public readonly record struct PayrollOtherAllowanceSyncResult(
        int PayrollMonth,
        int PayrollYear,
        int SourceRecordCount,
        int CreatedRecordCount,
        int UpdatedRecordCount,
        int UnchangedRecordCount,
        int SkippedLockedRecordCount,
        int NormalizedToFixedSnapshotRecordCount,
        decimal SourceTotalAmount,
        decimal SynchronizedTotalAmount,
        int UpdatedSummaryCount);

    public readonly record struct PayrollInsuranceDeductionSyncResult(
        int PayrollMonth,
        int PayrollYear,
        int SourceRecordCount,
        int CreatedRecordCount,
        int UpdatedRecordCount,
        int UnchangedRecordCount,
        int SkippedLockedRecordCount,
        decimal SourceTotalDeductionAmount,
        decimal SynchronizedTotalDeductionAmount);

    public readonly record struct PayrollResponsibilityAllowanceSyncResult(
        int PayrollMonth,
        int PayrollYear,
        int SourceRecordCount,
        int CreatedRecordCount,
        int UpdatedRecordCount,
        int UnchangedRecordCount,
        int SkippedLockedRecordCount,
        int SkippedMissingParentRecordCount,
        decimal SourceTotalAmount,
        decimal SynchronizedTotalAmount,
        int UpdatedSummaryCount);

    private readonly record struct PayrollResponsibilityAllowanceSourceRow(
        Guid SourceId, Guid EmployeeId, string EmployeeCode, string EmployeeName, Guid? PositionId, string PositionName,
        Guid? GradeId, string? GradeCode, string GradeName, int Year, int Month, decimal ActualWorkDays, decimal StandardWorkDays,
        string AbcRating, decimal MonthlyPerformanceBonusAmount, decimal StandardResponsibilityAllowanceAmount,
        decimal ActualResponsibilityAllowanceAmount, bool IsLocked, DateTime CalculatedAtUtc, string? CalculatedBy,
        DateTime? UpdatedAtUtc, string? UpdatedBy, DateTime? LockedAtUtc, string? LockedBy, string? Note, bool IsPerformanceBonusExcluded);

    private readonly record struct PayrollResponsibilitySummaryTargetRow(Guid Id, Guid EmployeeId, bool IsLocked, string? DepartmentName);

    private readonly record struct ExistingPayrollResponsibilityRow(
        Guid SummaryId, Guid EmployeeId, string EmployeeCode, string EmployeeName, string? DepartmentName, Guid? PositionId,
        string PositionName, Guid? GradeId, string? GradeCode, string GradeName, int Year, int Month, decimal ActualWorkDays,
        decimal StandardWorkDays, string? AbcRating, decimal MonthlyPerformanceBonusAmount, bool IsPerformanceBonusExcluded,
        decimal StandardResponsibilityAllowanceAmount, decimal ActualResponsibilityAllowanceAmount, bool IsLocked, DateTime CreatedAtUtc,
        DateTime? CalculatedAtUtc, string? CalculatedBy, DateTime? UpdatedAtUtc, string? UpdatedBy, DateTime? LockedAtUtc,
        string? LockedBy, string? Note);

    private readonly record struct PayrollInsuranceDeductionSourceRow(
        Guid EmployeeId,
        decimal InsuranceSalaryBaseAmount,
        decimal SocialInsuranceRate,
        decimal HealthInsuranceRate,
        decimal UnemploymentInsuranceRate,
        decimal SocialInsuranceAmount,
        decimal HealthInsuranceAmount,
        decimal UnemploymentInsuranceAmount,
        decimal TotalDeductionAmount,
        bool? IsParticipating,
        short? ParticipationChangeType,
        DateOnly? EffectiveDate,
        bool IsLocked,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        string? Note,
        string SourceParticipationStatus,
        string SourceChangeType)
    {
        public decimal TotalInsuranceRate =>
            decimal.Round(
                SocialInsuranceRate + HealthInsuranceRate + UnemploymentInsuranceRate,
                4,
                MidpointRounding.AwayFromZero);
    }

    private readonly record struct PayrollDeductionSummaryTargetRow(
        Guid Id,
        Guid EmployeeId,
        bool IsLocked);

    private readonly record struct ExistingPayrollInsuranceDeductionRow(
        Guid PayrollDeductionSummaryRecordId,
        decimal InsuranceSalaryBaseAmount,
        decimal SocialInsuranceRate,
        decimal HealthInsuranceRate,
        decimal UnemploymentInsuranceRate,
        decimal TotalInsuranceRate,
        decimal SocialInsuranceAmount,
        decimal HealthInsuranceAmount,
        decimal UnemploymentInsuranceAmount,
        decimal TotalDeductionAmount,
        bool IsParticipating,
        short ParticipationChangeType,
        DateOnly? EffectiveDate,
        bool IsLocked,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        string? InsuranceNote);

    private readonly record struct PayrollOtherAllowanceSourceRow(
        Guid Id,
        Guid EmployeeId,
        string AllowanceName,
        bool SourceIsFixedAmount,
        decimal AllowanceAmount,
        string? Note,
        DateTime CreatedAtUtc,
        string? CreatedBy,
        DateTime? UpdatedAtUtc,
        string? UpdatedBy)
    {
        // The legacy table is a monthly calculated snapshot. The target model only
        // permits a stored amount for fixed lines, so a positive historical amount
        // is represented as a fixed snapshot without altering the amount itself.
        public bool IsFixedAmount => SourceIsFixedAmount || AllowanceAmount > 0m;
    }

    private readonly record struct PayrollAllowanceSummaryTargetRow(
        Guid Id,
        Guid EmployeeId,
        bool IsLocked);

    private readonly record struct ExistingPayrollOtherAllowanceRow(
        Guid Id,
        Guid PayrollAllowanceSummaryRecordId,
        string AllowanceName,
        bool IsFixedAmount,
        decimal AllowanceAmount,
        string? Note,
        bool IsLocked,
        DateTime CreatedAtUtc,
        string CreatedBy,
        DateTime? UpdatedAtUtc,
        string? UpdatedBy);

    private readonly record struct PayrollBasicSalarySnapshot(
        Guid EmployeeId,
        decimal BasicSalary,
        decimal StandardWorkingDays,
        decimal DailySalary,
        decimal HourlySalary);

    private readonly record struct ExistingPayrollBasicSalaryRow(
        Guid Id,
        Guid EmployeeId,
        decimal BasicSalary,
        decimal StandardWorkingDays,
        decimal DailySalary,
        decimal HourlySalary);

    private readonly record struct DeviceColumnIndexes(
        int Id,
        int SerialNumber,
        int Name,
        int IpAddress,
        int ActivationCode,
        int VendorName,
        int DeviceModel,
        int MacAddress,
        int Location,
        int IsInUse,
        int LastRequestTime,
        int CreatedAtUtc,
        int UpdatedAtUtc);

    private sealed class DeviceSyncRow
    {
        public DeviceSyncRow(
            Guid sourceId,
            string? originalSerial,
            string? normalizedSerial,
            object?[] values,
            DeviceColumnIndexes columnIndexes)
        {
            SourceId = sourceId;
            OriginalSerial = originalSerial;
            NormalizedSerial = normalizedSerial;
            Values = values;
            IdColumnIndex = columnIndexes.Id;
            IsInUse = GetBoolean(values, columnIndexes.IsInUse);
            LastRequestTime = GetNullableDateTime(values, columnIndexes.LastRequestTime);
            UpdatedAtUtc = GetNullableDateTime(values, columnIndexes.UpdatedAtUtc);
            CreatedAtUtc = GetNullableDateTime(values, columnIndexes.CreatedAtUtc);
            CompletenessScore = CalculateCompletenessScore(values, columnIndexes, IsInUse, LastRequestTime.HasValue);
        }

        public Guid SourceId { get; }

        public string? OriginalSerial { get; }

        public string? NormalizedSerial { get; }

        public object?[] Values { get; }

        public int IdColumnIndex { get; }

        public bool IsInUse { get; }

        public DateTime? LastRequestTime { get; }

        public DateTime? UpdatedAtUtc { get; }

        public DateTime? CreatedAtUtc { get; }

        public DateTime? LastActivityAt => LastRequestTime ?? UpdatedAtUtc ?? CreatedAtUtc;

        public int CompletenessScore { get; }

        public bool IsSerialNormalized =>
            !string.Equals(OriginalSerial, NormalizedSerial, StringComparison.Ordinal);

        private static int CalculateCompletenessScore(
            IReadOnlyList<object?> values,
            DeviceColumnIndexes columnIndexes,
            bool isInUse,
            bool hasLastRequestTime)
        {
            var score = 0;
            score += hasLastRequestTime ? 8 : 0;
            score += isInUse ? 4 : 0;
            score += HasMeaningfulValue(values, columnIndexes.Name) ? 4 : 0;
            score += HasValue(values, columnIndexes.DeviceModel) ? 2 : 0;
            score += HasValue(values, columnIndexes.IpAddress) ? 2 : 0;
            score += HasValue(values, columnIndexes.MacAddress) ? 2 : 0;
            score += HasValue(values, columnIndexes.Location) ? 1 : 0;
            score += HasValue(values, columnIndexes.VendorName) ? 1 : 0;
            score += HasValue(values, columnIndexes.ActivationCode) ? 1 : 0;
            return score;
        }

        private static bool HasMeaningfulValue(
            IReadOnlyList<object?> values,
            int index)
        {
            if (index < 0)
            {
                return false;
            }

            var value = ConvertToNullableString(values[index]);
            return !string.IsNullOrWhiteSpace(value)
                && !string.Equals(value.Trim(), "VNTA-Devices", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasValue(
            IReadOnlyList<object?> values,
            int index) =>
            index >= 0
            && !string.IsNullOrWhiteSpace(ConvertToNullableString(values[index]));

        private static bool GetBoolean(
            IReadOnlyList<object?> values,
            int index)
        {
            if (index < 0)
            {
                return false;
            }

            var value = values[index];
            return value is bool typedBoolean && typedBoolean;
        }

        private static DateTime? GetNullableDateTime(
            IReadOnlyList<object?> values,
            int index)
        {
            if (index < 0)
            {
                return null;
            }

            var value = values[index];
            return value switch
            {
                DateTime typedDateTime => typedDateTime,
                string text when DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime) => parsedDateTime,
                _ => null
            };
        }
    }
}
