using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.Data;

public static class ApplicationSchemaGuard
{
    private const string DeviceSerialNumberUniqueIndexName = "ux_devices_serial_number_not_empty";
    private const string EmployeeCodeUniqueIndexName = "ux_employees_employee_code_active";

    public static async Task EnsureEmployeeAvatarColumnAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if(!await TableExistsAsync(dbContext, "employees", cancellationToken)) {
            return;
        }

        const string sql = """
            ALTER TABLE employees
            ADD COLUMN IF NOT EXISTS "Avatar" text;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public static async Task<DeviceSerialGuardResult> EnsureUniqueDeviceSerialNumbersAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
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

    public static async Task<EmployeeCodeGuardResult> EnsureUniqueActiveEmployeeCodesAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (!await TableExistsAsync(dbContext, "employees", cancellationToken))
        {
            return EmployeeCodeGuardResult.Empty;
        }

        var employees = await dbContext.Employees.ToListAsync(cancellationToken);
        var normalizedRowCount = 0;

        foreach (var employee in employees)
        {
            var normalizedCode = EmployeeCodeNormalizer.Normalize(employee.EmployeeCode) ?? string.Empty;
            if (string.Equals(employee.EmployeeCode, normalizedCode, StringComparison.Ordinal))
            {
                continue;
            }

            employee.EmployeeCode = normalizedCode;
            normalizedRowCount++;
        }

        if (normalizedRowCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var duplicateCodes = employees
            .Where(static employee => !employee.IsDeleted && !string.IsNullOrWhiteSpace(employee.EmployeeCode))
            .GroupBy(static employee => employee.EmployeeCode, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .OrderBy(static code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (duplicateCodes.Length > 0)
        {
            return new EmployeeCodeGuardResult(
                normalizedRowCount,
                duplicateCodes.Length,
                false,
                duplicateCodes.Take(5).ToArray());
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            $"""
            CREATE UNIQUE INDEX IF NOT EXISTS "{EmployeeCodeUniqueIndexName}"
            ON employees ("EmployeeCode")
            WHERE "IsDeleted" = FALSE
              AND "EmployeeCode" IS NOT NULL
              AND btrim("EmployeeCode") <> '';
            """,
            cancellationToken);

        return new EmployeeCodeGuardResult(
            normalizedRowCount,
            0,
            true,
            []);
    }

    private static async Task<bool> TableExistsAsync(
        ApplicationDbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;

        if(shouldCloseConnection) {
            await connection.OpenAsync(cancellationToken);
        }

        try {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public'
                      AND table_name = @table_name
                );
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "table_name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is bool exists && exists;
        }
        finally {
            if(shouldCloseConnection) {
                await connection.CloseAsync();
            }
        }
    }

    private static AttendanceDeviceRow SelectKeeper(
        IEnumerable<AttendanceDeviceRow> duplicateGroup) =>
        duplicateGroup
            .OrderByDescending(static device => device.IsInUse)
            .ThenByDescending(static device => device.LastRequestTime ?? device.UpdatedAtUtc ?? device.CreatedAtUtc)
            .ThenByDescending(GetDeviceCompletenessScore)
            .ThenByDescending(static device => device.UpdatedAtUtc ?? device.CreatedAtUtc)
            .ThenByDescending(static device => device.CreatedAtUtc)
            .ThenByDescending(static device => device.Id)
            .First();

    private static int GetDeviceCompletenessScore(
        AttendanceDeviceRow device)
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

        var normalized = AttendanceDeviceActivationCode.NormalizeSerial(serialNumber);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static bool IsGenericDeviceName(string? value) =>
        string.Equals(value?.Trim(), "VNTA-Devices", StringComparison.OrdinalIgnoreCase);
}

public sealed record DeviceSerialGuardResult(
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

public sealed record EmployeeCodeGuardResult(
    int NormalizedRowCount,
    int DuplicateGroupCount,
    bool UniqueIndexEnsured,
    IReadOnlyList<string> DuplicateCodes)
{
    public static EmployeeCodeGuardResult Empty { get; } = new(0, 0, false, []);

    public bool HasChanges =>
        NormalizedRowCount > 0 || UniqueIndexEnsured;
}
