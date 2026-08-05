namespace Vnta.PostgresSync.Console.Configuration;

public sealed class PostgresSyncOptions
{
    public const string SectionName = "PostgresSync";

    public string? SourceConnectionString { get; set; }

    public string? TargetConnectionString { get; set; }

    public bool RunOnce { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 300;

    public List<PostgresTableSyncOptions> Tables { get; set; } = [];
}
