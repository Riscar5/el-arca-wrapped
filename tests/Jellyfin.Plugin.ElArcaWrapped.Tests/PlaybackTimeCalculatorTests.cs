using Jellyfin.Plugin.ElArcaWrapped.Services;
using Xunit;

namespace Jellyfin.Plugin.ElArcaWrapped.Tests;

public sealed class PlaybackTimeCalculatorTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CountsNormalPlaybackProgress()
    {
        var watched = PlaybackTimeCalculator.CalculateWatchedDeltaTicks(
            Start, 0, false, TimeSpan.FromSeconds(10).Ticks, Start.AddSeconds(10));

        Assert.Equal(TimeSpan.FromSeconds(10).Ticks, watched);
    }

    [Fact]
    public void CountsTheFinalIntervalWhenTheCurrentEventPausesPlayback()
    {
        // The event marks the newly paused state; the preceding interval was still playing.
        var watched = PlaybackTimeCalculator.CalculateWatchedDeltaTicks(
            Start, 0, false, TimeSpan.FromSeconds(30).Ticks, Start.AddSeconds(30));

        Assert.Equal(TimeSpan.FromSeconds(30).Ticks, watched);
    }

    [Fact]
    public void DoesNotCountTimeAfterThePreviousEventWasPaused()
    {
        var watched = PlaybackTimeCalculator.CalculateWatchedDeltaTicks(
            Start, TimeSpan.FromSeconds(30).Ticks, true,
            TimeSpan.FromSeconds(90).Ticks, Start.AddMinutes(1));

        Assert.Equal(0, watched);
    }

    [Fact]
    public void DoesNotCountAForwardSeekAsWatchedTime()
    {
        var watched = PlaybackTimeCalculator.CalculateWatchedDeltaTicks(
            Start, TimeSpan.FromSeconds(10).Ticks, false,
            TimeSpan.FromSeconds(180).Ticks, Start.AddSeconds(10));

        Assert.Equal(0, watched);
    }

    [Fact]
    public void DoesNotCountABackwardSeek()
    {
        var watched = PlaybackTimeCalculator.CalculateWatchedDeltaTicks(
            Start, TimeSpan.FromSeconds(100).Ticks, false,
            TimeSpan.FromSeconds(20).Ticks, Start.AddSeconds(10));

        Assert.Equal(0, watched);
    }

    [Fact]
    public void AllowsSmallReportingDriftButNeverCountsMoreThanElapsedTime()
    {
        var watched = PlaybackTimeCalculator.CalculateWatchedDeltaTicks(
            Start, 0, false, TimeSpan.FromSeconds(12).Ticks, Start.AddSeconds(10));

        Assert.Equal(TimeSpan.FromSeconds(10).Ticks, watched);
    }

    [Fact]
    public void DoesNotCountWhenPositionOrTimestampIsMissingOrInvalid()
    {
        Assert.Equal(0, PlaybackTimeCalculator.CalculateWatchedDeltaTicks(
            Start, null, false, TimeSpan.FromSeconds(10).Ticks, Start.AddSeconds(10)));

        Assert.Equal(0, PlaybackTimeCalculator.CalculateWatchedDeltaTicks(
            Start, 0, false, null, Start.AddSeconds(10)));

        Assert.Equal(0, PlaybackTimeCalculator.CalculateWatchedDeltaTicks(
            Start.AddSeconds(10), 0, false, TimeSpan.FromSeconds(10).Ticks, Start));
    }

    [Fact]
    public void CountsTheFinalIntervalReportedByAStopEvent()
    {
        // PlaybackStopEventArgs inherits the same position and pause properties.
        var watched = PlaybackTimeCalculator.CalculateWatchedDeltaTicks(
            Start, TimeSpan.FromSeconds(45).Ticks, false,
            TimeSpan.FromSeconds(60).Ticks, Start.AddSeconds(15));

        Assert.Equal(TimeSpan.FromSeconds(15).Ticks, watched);
    }
}
