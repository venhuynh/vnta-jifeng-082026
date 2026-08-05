using Vnta.Hrm.Infrastructure.Tests.Integration;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests;

public sealed class TestProjectFoundationTests
{
    [Fact]
    public void PostgreSql_fixture_uses_a_dedicated_opt_in_connection_string()
    {
        Assert.Equal(
            "VNTA_HRM_TEST_POSTGRES_CONNECTION",
            PostgreSqlIntegrationFixture.ConnectionStringEnvironmentVariable);
    }
}
