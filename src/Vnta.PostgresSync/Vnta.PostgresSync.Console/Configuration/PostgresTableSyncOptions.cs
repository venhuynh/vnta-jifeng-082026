namespace Vnta.PostgresSync.Console.Configuration;

public sealed class PostgresTableSyncOptions
{
    public bool Enabled { get; set; } = true;

    public SyncPhase Phase { get; set; } = SyncPhase.MasterData;

    public int Order { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SourceQuery { get; set; } = string.Empty;

    public string TargetTable { get; set; } = string.Empty;

    public string TargetSetupSql { get; set; } = string.Empty;

    public bool ClearTargetBeforeInsert { get; set; }

    public int CommandTimeoutSeconds { get; set; } = 120;

    public List<string> ConflictKeys { get; set; } = [];

    public List<PostgresTableSyncColumnOptions> ColumnMappings { get; set; } = [];
}
