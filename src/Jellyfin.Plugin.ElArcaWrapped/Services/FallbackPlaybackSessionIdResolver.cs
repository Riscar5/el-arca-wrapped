using System.Collections.Concurrent;

namespace Jellyfin.Plugin.ElArcaWrapped.Services;

/// <summary>Groups start/progress/stop events when Jellyfin omits a playback-session ID.</summary>
internal sealed class FallbackPlaybackSessionIdResolver
{
    private readonly ConcurrentDictionary<string, string> _sessionIds = new(StringComparer.Ordinal);

    internal string Resolve(string key, string? knownSessionId, bool isStart, bool isStopped)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        string resolved;
        if (isStart)
        {
            // A new start always begins a new playback identity, even if a stale
            // fallback entry remains after a client disconnected without a stop event.
            resolved = !string.IsNullOrWhiteSpace(knownSessionId)
                ? knownSessionId
                : Guid.NewGuid().ToString("N");
            _sessionIds[key] = resolved;
        }
        else if (_sessionIds.TryGetValue(key, out var existingSessionId))
        {
            // Keep the identity chosen at start if later events begin supplying a
            // PlaySessionId. Switching IDs mid-playback would split one viewing
            // session into two database records and inflate play counts.
            resolved = existingSessionId;
        }
        else if (!string.IsNullOrWhiteSpace(knownSessionId))
        {
            resolved = knownSessionId;
            _sessionIds[key] = resolved;
        }
        else
        {
            resolved = _sessionIds.GetOrAdd(key, _ => Guid.NewGuid().ToString("N"));
        }

        if (isStopped)
        {
            _sessionIds.TryRemove(key, out _);
        }

        return resolved;
    }
}
