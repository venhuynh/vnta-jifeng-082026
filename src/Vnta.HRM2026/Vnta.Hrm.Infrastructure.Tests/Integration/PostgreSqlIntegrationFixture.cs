using Npgsql;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.Integration;

/// <summary>
/// Provides opt-in access to the dedicated PostgreSQL database used by integration tests.
/// </summary>
public sealed class PostgreSqlIntegrationFixture : IAsyncLifetime
{
    public const string ConnectionStringEnvironmentVariable = "VNTA_HRM_TEST_POSTGRES_CONNECTION";

    public string? ConnectionString { get; private set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);

    public Task InitializeAsync()
    {
        ConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = ConnectionString
            ?? throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} before running PostgreSQL integration tests.");

        var connection = new NpgsqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlIntegrationCollection : ICollectionFixture<PostgreSqlIntegrationFixture>
{
    public const string Name = "PostgreSQL integration";
}
