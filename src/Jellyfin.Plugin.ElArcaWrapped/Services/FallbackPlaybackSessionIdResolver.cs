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
        if (!string.IsNullOrWhiteSpace(knownSessionId))
        {
            resolved = knownSessionId;
            _sessionIds[key] = resolved;
        }
        else if (isStart)
        {
            resolved = Guid.NewGuid().ToString("N");
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
