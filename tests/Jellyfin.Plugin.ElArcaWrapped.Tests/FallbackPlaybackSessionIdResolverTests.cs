using Jellyfin.Plugin.ElArcaWrapped.Services;
using Xunit;

namespace Jellyfin.Plugin.ElArcaWrapped.Tests;

public sealed class FallbackPlaybackSessionIdResolverTests
{
    [Fact]
    public void MissingSessionIdKeepsStartProgressAndStopGrouped()
    {
        var resolver = new FallbackPlaybackSessionIdResolver();
        var start = resolver.Resolve("user|device|item", null, isStart: true, isStopped: false);
        var progress = resolver.Resolve("user|device|item", null, isStart: false, isStopped: false);
        var stop = resolver.Resolve("user|device|item", null, isStart: false, isStopped: true);

        Assert.Equal(start, progress);
        Assert.Equal(start, stop);
    }

    [Fact]
    public void NewStartOnSameKeyGetsDistinctSessionAndStopClearsFallback()
    {
        var resolver = new FallbackPlaybackSessionIdResolver();
        var first = resolver.Resolve("key", null, isStart: true, isStopped: false);
        resolver.Resolve("key", null, isStart: false, isStopped: true);
        var second = resolver.Resolve("key", null, isStart: true, isStopped: false);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void KnownJellyfinSessionIdIsPreserved()
    {
        var resolver = new FallbackPlaybackSessionIdResolver();
        var known = "jellyfin-session";
        Assert.Equal(known, resolver.Resolve("key", known, isStart: true, isStopped: false));
        Assert.Equal(known, resolver.Resolve("key", null, isStart: false, isStopped: false));
    }

    [Fact]
    public void LateKnownSessionIdDoesNotSplitPlaybackStartedWithoutOne()
    {
        var resolver = new FallbackPlaybackSessionIdResolver();
        var fallback = resolver.Resolve("key", null, isStart: true, isStopped: false);

        var progressWithLateKnownId = resolver.Resolve(
            "key", "jellyfin-session", isStart: false, isStopped: false);
        var stopWithKnownId = resolver.Resolve(
            "key", "jellyfin-session", isStart: false, isStopped: true);

        Assert.Equal(fallback, progressWithLateKnownId);
        Assert.Equal(fallback, stopWithKnownId);
    }
}
