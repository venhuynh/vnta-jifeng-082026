using Npgsql;

namespace Vnta.AttendanceGateway.Data;

internal static class JifengHrmDatabaseTargetValidator
{
    internal const string RequiredDatabaseName = "jifeng_hrm";

    public static void Validate(string connectionString)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        var resolvedDatabaseName = connectionStringBuilder.Database;

        if (!string.Equals(resolvedDatabaseName, RequiredDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Attendance gateway must target PostgreSQL database '{RequiredDatabaseName}', " +
                $"but resolved '{FormatDatabaseName(resolvedDatabaseName)}'.");
        }
    }

    private static string FormatDatabaseName(string? databaseName) =>
        string.IsNullOrWhiteSpace(databaseName) ? "<empty>" : databaseName;
}
