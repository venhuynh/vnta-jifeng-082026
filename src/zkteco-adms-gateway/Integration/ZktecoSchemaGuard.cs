using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Domain;
using Vnta.AttendanceGateway.Security;
using Microsoft.EntityFrameworkCore;

namespace Vnta.AttendanceGateway.Integration;

internal static class ZktecoSchemaGuard
{
    private const string DeviceSerialNumberUniqueIndexName = "ux_devices_serial_number_not_empty";

    public static async Task EnsureEmployeeAvatarColumnAsync(
        ZktecoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(dbContext, "employees", cancellationToken))
        {
            return;
        }

        const string sql = """
            ALTER TABLE employees
            ADD COLUMN IF NOT EXISTS "Avatar" text;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public static async Task<int> BackfillEmployeeAvatarsAsync(
        ZktecoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(dbContext, "employees", cancellationToken))
        {
            return 0;
        }

        var updatedRows = 0;
        if (await TableExistsAsync(dbContext, "user_pictures", cancellationToken))
        {
            updatedRows += await BackfillEmployeeAvatarsFromTableAsync(dbContext, "user_pictures", cancellationToken);
        }

        if (await TableExistsAsync(dbContext, "bio_photos", cancellationToken))
        {
            updatedRows += await BackfillEmployeeAvatarsFromTableAsync(dbContext, "bio_photos", cancellationToken);
        }

        return updatedRows;
    }

    public static async Task<int> BackfillMissingEmployeeEmailsAsync(
        ZktecoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(dbContext, "employees", cancellationToken))
        {
            return 0;
        }

        const string sql = """
            UPDATE employees
            SET "Email" = CONCAT(
                COALESCE(
                    NULLIF(regexp_replace(lower(COALESCE("EmployeeCode", '')), '[^a-z0-9]+', '-', 'g'), ''),
                    'unknown-' || replace("Id"::text, '-', '')
                ),
                '@autogen.VNTA.local'
            )
            WHERE "Email" IS NULL
               OR btrim("Email") = '';
            """;

        return await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public static async Task<DeviceSerialGuardResult> EnsureUniqueDeviceSerialNumbersAsync(
        ZktecoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(dbContext, "devices", cancellationToken))
        {
            return DeviceSerialGuardResult.Empty;
        }

        var attendanceLogsTableExists = await TableExistsAsync(dbContext, "attendance_logs", cancellationToken);
        var devices = await dbContext.Devices.ToListAsync(cancellationToken);
        var normalizedRowCount = 0;

        foreach (var device in devices)
        {
            var normalizedSerial = NormalizeSerial(device.SerialNumber);
            if (string.Equals(device.SerialNumber, normalizedSerial, StringComparison.Ordinal))
            {
                continue;
            }

            device.SerialNumber = normalizedSerial;
            normalizedRowCount++;
        }

        var duplicateGroupCount = 0;
        var deletedDeviceCount = 0;
        var remappedAttendanceLogCount = 0;

        foreach (var duplicateGroup in devices
                     .Where(static device => !string.IsNullOrWhiteSpace(device.SerialNumber))
                     .GroupBy(device => device.SerialNumber!, StringComparer.OrdinalIgnoreCase)
                     .Where(static group => group.Count() > 1))
        {
            duplicateGroupCount++;
            var keeper = SelectKeeper(duplicateGroup);

            foreach (var duplicateDevice in duplicateGroup.Where(device => device.Id != keeper.Id))
            {
                if (attendanceLogsTableExists)
                {
                    remappedAttendanceLogCount += await dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"""UPDATE attendance_logs SET "DeviceId" = {keeper.Id} WHERE "DeviceId" = {duplicateDevice.Id};""",
                        cancellationToken);
                }

                dbContext.Devices.Remove(duplicateDevice);
                deletedDeviceCount++;
            }
        }

        if (normalizedRowCount > 0 || deletedDeviceCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            $"""
            CREATE UNIQUE INDEX IF NOT EXISTS "{DeviceSerialNumberUniqueIndexName}"
            ON devices ("SerialNumber")
            WHERE "SerialNumber" IS NOT NULL
              AND btrim("SerialNumber") <> '';
            """,
            cancellationToken);

        return new DeviceSerialGuardResult(
            normalizedRowCount,
            duplicateGroupCount,
            deletedDeviceCount,
            remappedAttendanceLogCount);
    }

    public static Task EnsureEmployeeReferenceIndexAsync(
        ZktecoDbContext dbContext,
        string tableName,
        string indexName,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            CREATE INDEX IF NOT EXISTS "{indexName}"
            ON "{tableName}" ("EmployeeId");
            """;

        return dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public static async Task EnsureEmployeeReferenceConstraintAsync(
        ZktecoDbContext dbContext,
        string tableName,
        string constraintName,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(dbContext, "employees", cancellationToken))
        {
            return;
        }

        var sql = $"""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = '{tableName}'
                      AND column_name = 'EmployeeId'
                ) AND NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = '{constraintName}'
                ) AND NOT EXISTS (
                    SELECT 1
                    FROM "{tableName}" source
                    LEFT JOIN employees target ON target."Id" = source."EmployeeId"
                    WHERE source."EmployeeId" IS NOT NULL
                      AND target."Id" IS NULL
                ) THEN
                    ALTER TABLE "{tableName}"
                    ADD CONSTRAINT "{constraintName}"
                    FOREIGN KEY ("EmployeeId") REFERENCES employees ("Id")
                    ON DELETE RESTRICT;
                END IF;
            END
            $$;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public static async Task EnsureOutboundSystemLogTableAsync(
        ZktecoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS outbound_system_logs (
                "Id" uuid PRIMARY KEY,
                "DeviceSn" character varying(50) NOT NULL,
                "ConnectionId" character varying(100) NOT NULL,
                "Direction" character varying(20) NOT NULL,
                "EventType" character varying(100) NOT NULL,
                "Message" text NOT NULL,
                "OccurredAtUtc" timestamp without time zone NOT NULL,
                "AttemptCount" integer NOT NULL DEFAULT 0,
                "Status" character varying(20) NOT NULL,
                "LastError" text NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NOT NULL,
                "LastAttemptAtUtc" timestamp without time zone NULL,
                "NextAttemptAtUtc" timestamp without time zone NULL,
                "DeliveredAtUtc" timestamp without time zone NULL,
                "FailedAtUtc" timestamp without time zone NULL
            );

            CREATE INDEX IF NOT EXISTS "ix_outbound_system_logs_status"
                ON outbound_system_logs ("Status");

            CREATE INDEX IF NOT EXISTS "ix_outbound_system_logs_next_attempt"
                ON outbound_system_logs ("NextAttemptAtUtc");

            CREATE INDEX IF NOT EXISTS "ix_outbound_system_logs_created_at"
                ON outbound_system_logs ("CreatedAtUtc");
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public static async Task EnsureOutboundAttendanceLogTableAsync(
        ZktecoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS outbound_attendance_logs (
                "Id" uuid PRIMARY KEY,
                "AttendanceLogId" uuid NOT NULL,
                "DeviceSn" character varying(50) NOT NULL,
                "EmployeeCode" character varying(50) NOT NULL,
                "TapTime" timestamp without time zone NOT NULL,
                "VerificationMode" integer NOT NULL,
                "InOutMode" integer NOT NULL,
                "AttemptCount" integer NOT NULL DEFAULT 0,
                "Status" character varying(20) NOT NULL,
                "LastError" text NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NOT NULL,
                "LastAttemptAtUtc" timestamp with time zone NULL,
                "NextAttemptAtUtc" timestamp with time zone NULL,
                "DeliveredAtUtc" timestamp with time zone NULL,
                "FailedAtUtc" timestamp with time zone NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "ix_outbound_attendance_logs_attendance_log_id"
                ON outbound_attendance_logs ("AttendanceLogId");

            CREATE INDEX IF NOT EXISTS "ix_outbound_attendance_logs_status"
                ON outbound_attendance_logs ("Status");

            CREATE INDEX IF NOT EXISTS "ix_outbound_attendance_logs_next_attempt"
                ON outbound_attendance_logs ("NextAttemptAtUtc");

            CREATE INDEX IF NOT EXISTS "ix_outbound_attendance_logs_created_at"
                ON outbound_attendance_logs ("CreatedAtUtc");
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public static async Task<bool> TableExistsAsync(
        ZktecoDbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = current_schema()
                  AND table_name = {0}
            ) AS "Value"
            """;

        var result = await dbContext.Database.SqlQueryRaw<bool>(sql, tableName).SingleAsync(cancellationToken);
        return result;
    }

    private static Task<int> BackfillEmployeeAvatarsFromTableAsync(
        ZktecoDbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            WITH latest_avatar AS (
                SELECT DISTINCT ON ("EmployeeId")
                    "EmployeeId",
                    btrim("Content") AS "Content"
                FROM "{tableName}"
                WHERE "EmployeeId" IS NOT NULL
                  AND NULLIF(btrim("Content"), '') IS NOT NULL
                ORDER BY "EmployeeId",
                         COALESCE("UpdatedAtUtc", "CreatedAtUtc") DESC,
                         "CreatedAtUtc" DESC,
                         "Id" DESC
            )
            UPDATE employees target
            SET "Avatar" = latest_avatar."Content"
            FROM latest_avatar
            WHERE target."Id" = latest_avatar."EmployeeId"
              AND (target."Avatar" IS NULL OR btrim(target."Avatar") = '');
            """;

        return dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static ZktecoDevice SelectKeeper(IEnumerable<ZktecoDevice> duplicateGroup) =>
        duplicateGroup
            .OrderByDescending(static device => device.IsInUse)
            .ThenByDescending(static device => device.LastRequestTime ?? device.UpdatedAtUtc ?? device.CreatedAtUtc)
            .ThenByDescending(GetDeviceCompletenessScore)
            .ThenByDescending(static device => device.UpdatedAtUtc ?? device.CreatedAtUtc)
            .ThenByDescending(static device => device.CreatedAtUtc)
            .ThenByDescending(static device => device.Id)
            .First();

    private static int GetDeviceCompletenessScore(ZktecoDevice device)
    {
        var score = 0;
        score += device.LastRequestTime.HasValue ? 8 : 0;
        score += device.IsInUse ? 4 : 0;
        score += HasValue(device.Name) && !IsGenericDeviceName(device.Name) ? 4 : 0;
        score += HasValue(device.DeviceModel) ? 2 : 0;
        score += HasValue(device.IpAddress) ? 2 : 0;
        score += HasValue(device.MacAddress) ? 2 : 0;
        score += HasValue(device.Location) ? 1 : 0;
        score += HasValue(device.VendorName) ? 1 : 0;
        score += HasValue(device.ActivationCode) ? 1 : 0;
        return score;
    }

    private static string? NormalizeSerial(string? serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return null;
        }

        var normalized = VntaCrypto.NormalizeSerial(serialNumber);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static bool IsGenericDeviceName(string? value) =>
        string.Equals(value?.Trim(), "VNTA-Devices", StringComparison.OrdinalIgnoreCase);
}

internal sealed record DeviceSerialGuardResult(
    int NormalizedRowCount,
    int DuplicateGroupCount,
    int DeletedDeviceCount,
    int RemappedAttendanceLogCount)
{
    public static DeviceSerialGuardResult Empty { get; } = new(0, 0, 0, 0);

    public bool HasChanges =>
        NormalizedRowCount > 0
        || DuplicateGroupCount > 0
        || DeletedDeviceCount > 0
        || RemappedAttendanceLogCount > 0;
}
