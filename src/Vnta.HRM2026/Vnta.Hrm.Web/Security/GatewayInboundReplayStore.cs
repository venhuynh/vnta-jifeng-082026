using System.Collections.Concurrent;

namespace Vnta.Hrm.Web.Security;

public sealed class GatewayInboundReplayStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> acceptedNonces = new(StringComparer.Ordinal);
    private int cleanupRequested;

    public bool TryAccept(string keyId, string nonce, DateTimeOffset expiresAtUtc)
    {
        CleanupExpiredEntries();
        return acceptedNonces.TryAdd($"{keyId}:{nonce}", expiresAtUtc);
    }

    private void CleanupExpiredEntries()
    {
        if (Interlocked.Exchange(ref cleanupRequested, 1) != 0)
        {
            return;
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in acceptedNonces)
            {
                if (entry.Value <= now)
                {
                    acceptedNonces.TryRemove(entry);
                }
            }
        }
        finally
        {
            Volatile.Write(ref cleanupRequested, 0);
        }
    }
}
