using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Vnta.AttendanceGateway.Data;

public sealed class ZktecoDbContextFactory : IDesignTimeDbContextFactory<ZktecoDbContext>
{
    public ZktecoDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("VNTA_DB");
        }
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection is required for design-time operations. Set it in appsettings.Local.json, an environment variable, or VNTA_DB.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ZktecoDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ZktecoDbContext(optionsBuilder.Options);
    }
}
