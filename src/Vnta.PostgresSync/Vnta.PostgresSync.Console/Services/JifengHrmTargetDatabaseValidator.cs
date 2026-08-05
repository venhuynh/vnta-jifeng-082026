using Npgsql;

namespace Vnta.PostgresSync.Console.Services;

internal static class JifengHrmTargetDatabaseValidator
{
    internal const string RequiredDatabaseName = "jifeng_hrm";

    public static void Validate(string connectionString)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        var resolvedDatabaseName = connectionStringBuilder.Database;

        if (!string.Equals(resolvedDatabaseName, RequiredDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"PostgreSQL sync target must use database '{RequiredDatabaseName}', " +
                $"but resolved '{FormatDatabaseName(resolvedDatabaseName)}'.");
        }
    }

    private static string FormatDatabaseName(string? databaseName) =>
        string.IsNullOrWhiteSpace(databaseName) ? "<empty>" : databaseName;
}
