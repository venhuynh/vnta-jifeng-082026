using Vnta.Hrm.Infrastructure.Data;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.Data;

public sealed class DatabaseConnectionStringResolverTests
{
    [Fact]
    public void EnsureExpectedDatabase_accepts_jifeng_hrm()
    {
        DatabaseConnectionStringResolver.EnsureExpectedDatabase(
            "Host=localhost;Port=5432;Database=jifeng_hrm;Username=test;Password=test");
    }

    [Fact]
    public void EnsureExpectedDatabase_rejects_a_different_database()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DatabaseConnectionStringResolver.EnsureExpectedDatabase(
                "Host=localhost;Port=5432;Database=isolated_test_database;Username=test;Password=test"));

        Assert.Equal(
            "The configured database must be 'jifeng_hrm'. " +
            "Update ConnectionStrings:Postgres, ConnectionStrings:DefaultConnection, or VNTA_DB outside source control.",
            exception.Message);
    }
}
