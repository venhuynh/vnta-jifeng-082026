namespace Vnta.PostgresSync.Console.Services;

public static class SqlIdentifier
{
    public static string QuoteQualifiedIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new InvalidOperationException("A PostgreSQL identifier cannot be empty.");
        }

        return string.Join(
            ".",
            identifier
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(QuoteIdentifier));
    }

    public static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new InvalidOperationException("A PostgreSQL identifier cannot be empty.");
        }

        var trimmedIdentifier = identifier.Trim();
        if (trimmedIdentifier.StartsWith('"') && trimmedIdentifier.EndsWith('"'))
        {
            return trimmedIdentifier;
        }

        return $"\"{trimmedIdentifier.Replace("\"", "\"\"")}\"";
    }
}
