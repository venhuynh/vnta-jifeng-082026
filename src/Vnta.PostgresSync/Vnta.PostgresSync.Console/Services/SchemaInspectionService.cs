using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Vnta.PostgresSync.Console.Services;

public sealed class SchemaInspectionService
{
    private const string TableQuery = """
        select
            t.table_schema,
            t.table_name,
            coalesce(pk.primary_key_columns, '') as primary_key_columns
        from information_schema.tables t
        left join (
            select
                tc.table_schema,
                tc.table_name,
                string_agg(kcu.column_name, ', ' order by kcu.ordinal_position) as primary_key_columns
            from information_schema.table_constraints tc
            join information_schema.key_column_usage kcu
                on tc.constraint_name = kcu.constraint_name
               and tc.table_schema = kcu.table_schema
               and tc.table_name = kcu.table_name
            where tc.constraint_type = 'PRIMARY KEY'
            group by tc.table_schema, tc.table_name
        ) pk
            on t.table_schema = pk.table_schema
           and t.table_name = pk.table_name
        where t.table_type = 'BASE TABLE'
          and t.table_schema not in ('pg_catalog', 'information_schema')
        order by t.table_schema, t.table_name;
        """;

    private const string ColumnQuery = """
        select
            c.table_schema,
            c.table_name,
            c.ordinal_position,
            c.column_name,
            c.data_type,
            c.udt_name,
            c.is_nullable
        from information_schema.columns c
        where c.table_schema not in ('pg_catalog', 'information_schema')
        order by c.table_schema, c.table_name, c.ordinal_position;
        """;

    private readonly IConfiguration _configuration;

    public SchemaInspectionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task WriteSchemaComparisonAsync(CancellationToken cancellationToken)
    {
        var sourceConnectionString = ResolveConnectionString("SourcePostgres", "VNTA_POSTGRES_SYNC_SOURCE");
        var targetConnectionString = ResolveTargetConnectionString();

        var sourceTables = await LoadTablesAsync(sourceConnectionString, cancellationToken);
        var targetTables = await LoadTablesAsync(targetConnectionString, cancellationToken);

        WriteDatabaseSummary("SOURCE", sourceTables);
        WriteDatabaseSummary("TARGET", targetTables);
        WriteOverlapSummary(sourceTables, targetTables);
    }

    private async Task<List<TableSchemaSnapshot>> LoadTablesAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var tableMap = new Dictionary<string, TableSchemaSnapshot>(StringComparer.OrdinalIgnoreCase);

        await using (var tableCommand = new NpgsqlCommand(TableQuery, connection))
        await using (var tableReader = await tableCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await tableReader.ReadAsync(cancellationToken))
            {
                var schema = tableReader.GetString(0);
                var name = tableReader.GetString(1);
                var primaryKeyColumns = tableReader.GetString(2);
                var key = BuildTableKey(schema, name);

                tableMap[key] = new TableSchemaSnapshot(
                    schema,
                    name,
                    string.IsNullOrWhiteSpace(primaryKeyColumns)
                        ? []
                        : primaryKeyColumns
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList(),
                    []);
            }
        }

        await using (var columnCommand = new NpgsqlCommand(ColumnQuery, connection))
        await using (var columnReader = await columnCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await columnReader.ReadAsync(cancellationToken))
            {
                var schema = columnReader.GetString(0);
                var tableName = columnReader.GetString(1);
                var ordinal = columnReader.GetInt32(2);
                var columnName = columnReader.GetString(3);
                var dataType = columnReader.GetString(4);
                var udtName = columnReader.GetString(5);
                var isNullable = string.Equals(columnReader.GetString(6), "YES", StringComparison.OrdinalIgnoreCase);
                var key = BuildTableKey(schema, tableName);

                if (!tableMap.TryGetValue(key, out var snapshot))
                {
                    continue;
                }

                snapshot.Columns.Add(new ColumnSchemaSnapshot(
                    ordinal,
                    columnName,
                    dataType,
                    udtName,
                    isNullable));
            }
        }

        return tableMap.Values
            .OrderBy(table => table.Schema, StringComparer.OrdinalIgnoreCase)
            .ThenBy(table => table.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void WriteDatabaseSummary(string label, IReadOnlyList<TableSchemaSnapshot> tables)
    {
        System.Console.WriteLine($"===== {label} DATABASE =====");
        System.Console.WriteLine($"Total tables: {tables.Count}");

        foreach (var table in tables)
        {
            var pk = table.PrimaryKeyColumns.Count == 0
                ? "(no primary key)"
                : string.Join(", ", table.PrimaryKeyColumns);

            System.Console.WriteLine($"{table.Schema}.{table.Name} | PK: {pk}");

            foreach (var column in table.Columns)
            {
                var nullableLabel = column.IsNullable ? "NULL" : "NOT NULL";
                System.Console.WriteLine(
                    $"  - {column.Ordinal:00}. {column.Name} | {column.DataType} ({column.UdtName}) | {nullableLabel}");
            }
        }

        System.Console.WriteLine();
    }

    private static void WriteOverlapSummary(
        IReadOnlyList<TableSchemaSnapshot> sourceTables,
        IReadOnlyList<TableSchemaSnapshot> targetTables)
    {
        var sourceMap = sourceTables.ToDictionary(
            table => BuildTableKey(table.Schema, table.Name),
            StringComparer.OrdinalIgnoreCase);
        var targetMap = targetTables.ToDictionary(
            table => BuildTableKey(table.Schema, table.Name),
            StringComparer.OrdinalIgnoreCase);

        var sharedKeys = sourceMap.Keys
            .Intersect(targetMap.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var sourceOnlyKeys = sourceMap.Keys
            .Except(targetMap.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var targetOnlyKeys = targetMap.Keys
            .Except(sourceMap.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        System.Console.WriteLine("===== OVERLAP SUMMARY =====");
        System.Console.WriteLine($"Shared tables: {sharedKeys.Length}");
        foreach (var key in sharedKeys)
        {
            System.Console.WriteLine($"  - {key}");
        }

        System.Console.WriteLine($"Source-only tables: {sourceOnlyKeys.Length}");
        foreach (var key in sourceOnlyKeys)
        {
            System.Console.WriteLine($"  - {key}");
        }

        System.Console.WriteLine($"Target-only tables: {targetOnlyKeys.Length}");
        foreach (var key in targetOnlyKeys)
        {
            System.Console.WriteLine($"  - {key}");
        }

        System.Console.WriteLine();

        var candidateAttendanceTables = sourceTables
            .Concat(targetTables)
            .Where(table =>
                table.Name.Contains("attendance", StringComparison.OrdinalIgnoreCase)
                || table.Name.Contains("att", StringComparison.OrdinalIgnoreCase)
                || table.Name.Contains("checkin", StringComparison.OrdinalIgnoreCase)
                || table.Name.Contains("time", StringComparison.OrdinalIgnoreCase))
            .GroupBy(table => BuildTableKey(table.Schema, table.Name), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(table => table.Schema, StringComparer.OrdinalIgnoreCase)
            .ThenBy(table => table.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        System.Console.WriteLine("===== ATTENDANCE CANDIDATES =====");
        foreach (var table in candidateAttendanceTables)
        {
            var interestingColumns = table.Columns
                .Where(column =>
                    column.Name.Contains("date", StringComparison.OrdinalIgnoreCase)
                    || column.Name.Contains("time", StringComparison.OrdinalIgnoreCase)
                    || column.Name.Contains("employee", StringComparison.OrdinalIgnoreCase)
                    || column.Name.Contains("device", StringComparison.OrdinalIgnoreCase)
                    || column.Name.Contains("check", StringComparison.OrdinalIgnoreCase))
                .Select(column => column.Name)
                .ToArray();

            var summary = interestingColumns.Length == 0
                ? "(no obvious attendance columns)"
                : string.Join(", ", interestingColumns);

            System.Console.WriteLine($"  - {table.Schema}.{table.Name}: {summary}");
        }
    }

    private string ResolveConnectionString(string connectionStringName, string environmentVariableName)
    {
        return _configuration.GetConnectionString(connectionStringName)
            ?? Environment.GetEnvironmentVariable(environmentVariableName)
            ?? throw new InvalidOperationException(
                $"Missing connection string '{connectionStringName}' and environment variable '{environmentVariableName}'.");
    }

    private string ResolveTargetConnectionString()
    {
        var connectionString = ResolveConnectionString("TargetPostgres", "VNTA_POSTGRES_SYNC_TARGET");
        JifengHrmTargetDatabaseValidator.Validate(connectionString);
        return connectionString;
    }

    private static string BuildTableKey(string schema, string tableName)
    {
        return $"{schema}.{tableName}";
    }

    private sealed class TableSchemaSnapshot
    {
        public TableSchemaSnapshot(
            string schema,
            string name,
            List<string> primaryKeyColumns,
            List<ColumnSchemaSnapshot> columns)
        {
            Schema = schema;
            Name = name;
            PrimaryKeyColumns = primaryKeyColumns;
            Columns = columns;
        }

        public string Schema { get; }

        public string Name { get; }

        public List<string> PrimaryKeyColumns { get; }

        public List<ColumnSchemaSnapshot> Columns { get; }
    }

    private sealed record ColumnSchemaSnapshot(
        int Ordinal,
        string Name,
        string DataType,
        string UdtName,
        bool IsNullable);
}
