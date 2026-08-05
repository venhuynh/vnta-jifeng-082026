using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Vnta.Hrm.Infrastructure.Data;

public static class DatabaseConnectionStringResolver
{
    public const string ExpectedDatabaseName = "jifeng_hrm";

    public static string Resolve(IConfiguration configuration, bool requireExpectedDatabase = true)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("VNTA_DB");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing database connection string. Configure ConnectionStrings:Postgres or VNTA_DB outside source control.");
        }

        if (requireExpectedDatabase)
        {
            EnsureExpectedDatabase(connectionString);
        }

        return connectionString;
    }

    public static void EnsureExpectedDatabase(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (!string.Equals(databaseName, ExpectedDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The configured database must be '{ExpectedDatabaseName}'. " +
                "Update ConnectionStrings:Postgres, ConnectionStrings:DefaultConnection, or VNTA_DB outside source control.");
        }
    }
}
