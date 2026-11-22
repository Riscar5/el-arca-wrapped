namespace Jellyfin.Plugin.ElArcaWrapped.Services;

/// <summary>
/// Estimates watched time between two Jellyfin playback reports.
/// Position deltas are used instead of wall time so buffering is not counted.
/// Large forward jumps are treated as seeks rather than viewing time.
/// </summary>
internal static class PlaybackTimeCalculator
{
    private const long DefaultPositionToleranceTicks = 3 * TimeSpan.TicksPerSecond;

    internal static long CalculateWatchedDeltaTicks(
        DateTimeOffset? previousEventAtUtc,
        long? previousPositionTicks,
        bool previousEventWasPaused,
        long? currentPositionTicks,
        DateTimeOffset currentEventAtUtc,
        long positionToleranceTicks = DefaultPositionToleranceTicks)
    {
        if (previousEventWasPaused ||
            previousEventAtUtc is not { } previousAt ||
            previousPositionTicks is not { } previousPosition ||
            currentPositionTicks is not { } currentPosition)
        {
            return 0;
        }

        var elapsedTicks = (currentEventAtUtc - previousAt).Ticks;
        var positionDeltaTicks = currentPosition - previousPosition;

        if (elapsedTicks <= 0 || positionDeltaTicks <= 0)
        {
            return 0;
        }

        // A forward position jump beyond elapsed time plus a small reporting tolerance
        // is almost certainly a seek. Do not count that interval as watched.
        var tolerance = Math.Max(0, positionToleranceTicks);
        if (positionDeltaTicks - elapsedTicks > tolerance)
        {
            return 0;
        }

        // Count actual media progress, bounded by wall-clock time. This excludes buffering
        // and prevents a delayed or malformed position report from inflating the total.
        return Math.Min(positionDeltaTicks, elapsedTicks);
    }
}
