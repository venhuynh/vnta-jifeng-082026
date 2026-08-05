namespace Vnta.PostgresSync.Console.Configuration;

public sealed class PostgresTableSyncColumnOptions
{
    public string SourceColumn { get; set; } = string.Empty;

    public string TargetColumn { get; set; } = string.Empty;
}
