using System.Globalization;

namespace Jellyfin.Plugin.ElArcaWrapped.Statistics;

/// <summary>Computes a first set of annual metrics from normalized, user-scoped playback records.</summary>
public static class WrappedStatisticsCalculator
{
    public static WrappedStatistics Calculate(IEnumerable<PlaybackRecord> source, string userId, int year)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (year is < 1900 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        // User isolation is enforced here too, even if the caller already filtered the query.
        var records = source
            .Where(r => string.Equals(r.UserId, userId, StringComparison.Ordinal))
            .Where(r => r.StartedAtUtc.Year == year)
            .OrderBy(r => r.StartedAtUtc)
            .ToArray();

        var durationMinutes = records.Select(GetMinutesWatched).ToArray();
        var movies = records.Where(IsMovie).ToArray();
        var episodes = records.Where(IsEpisode).ToArray();

        var monthly = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var indices = records.Select((record, index) => (record, index))
                    .Where(pair => pair.record.StartedAtUtc.Month == month)
                    .Select(pair => pair.index);
                var monthIndices = indices.ToArray();
                return new MonthlyActivity(month, monthIndices.Length,
                    monthIndices.Sum(index => durationMinutes[index]));
            }).ToArray();

        var topMovies = RankItems(movies.Select(record => (Record: record, Minutes: durationMinutes[Array.IndexOf(records, record)])), r => r.ItemName);
        var seriesRecords = episodes;
        var topSeries = RankItems(seriesRecords, durationMinutes,
            r => string.IsNullOrWhiteSpace(r.SeriesName) ? r.ItemName : r.SeriesName!);

        var genreCounts = new Dictionary<string, (int Plays, double Minutes)>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < records.Length; index++)
        {
            foreach (var genre in records[index].Genres.Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var existing = genreCounts.GetValueOrDefault(genre);
                genreCounts[genre] = (existing.Plays + 1, existing.Minutes + durationMinutes[index]);
            }
        }

        var topGenres = genreCounts
            .OrderByDescending(pair => pair.Value.Minutes)
            .ThenByDescending(pair => pair.Value.Plays)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(pair => new GenreActivity(pair.Key, pair.Value.Plays, pair.Value.Minutes))
            .ToArray();

        var activeDates = records.Select(r => r.StartedAtUtc.Date).Distinct().Order().ToArray();

        return new WrappedStatistics
        {
            UserId = userId,
            Year = year,
            TotalPlays = records.Length,
            TotalMinutesWatched = durationMinutes.Sum(),
            MoviePlays = movies.Length,
            EpisodePlays = episodes.Length,
            UniqueMovies = movies.Select(r => r.ItemId).Distinct(StringComparer.Ordinal).Count(),
            UniqueSeries = episodes.Select(r => string.IsNullOrWhiteSpace(r.SeriesName) ? r.ItemName : r.SeriesName!).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            ActiveDays = activeDates.Length,
            LongestStreakDays = GetLongestStreak(activeDates),
            CompletedPlays = records.Count(r => r.PlayedToCompletion),
            MonthlyActivity = monthly,
            TopMovies = topMovies,
            TopSeries = topSeries,
            TopGenres = topGenres
        };
    }

    private static double GetMinutesWatched(PlaybackRecord record)
    {
        long? ticks = record.WatchedTicks;
        if (ticks is null && record.EndedAtUtc is { } end && end > record.StartedAtUtc)
        {
            ticks = (end - record.StartedAtUtc).Ticks;
        }

        if (ticks is null || ticks <= 0)
        {
            return 0;
        }

        // A recorded play can exceed runtime due to pause/seek or event gaps. Cap at runtime when known.
        if (record.RuntimeTicks is > 0)
        {
            ticks = Math.Min(ticks.Value, record.RuntimeTicks.Value);
        }

        return TimeSpan.FromTicks(ticks.Value).TotalMinutes;
    }

    private static bool IsMovie(PlaybackRecord record) =>
        record.ItemType.Contains("Movie", StringComparison.OrdinalIgnoreCase);

    private static bool IsEpisode(PlaybackRecord record) =>
        record.ItemType.Contains("Episode", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<RankedItem> RankItems(
        IEnumerable<(PlaybackRecord Record, double Minutes)> items,
        Func<PlaybackRecord, string> nameSelector)
    {
        var aggregates = new Dictionary<string, (int Plays, double Minutes)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (record, minutes) in items)
        {
            var name = nameSelector(record);
            var current = aggregates.GetValueOrDefault(name);
            aggregates[name] = (current.Plays + 1, current.Minutes + minutes);
        }

        return aggregates
            .OrderByDescending(pair => pair.Value.Minutes)
            .ThenByDescending(pair => pair.Value.Plays)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(pair => new RankedItem(pair.Key, pair.Value.Plays, pair.Value.Minutes))
            .ToArray();
    }

    private static int GetLongestStreak(IReadOnlyList<DateTime> dates)
    {
        if (dates.Count == 0)
        {
            return 0;
        }

        var longest = 1;
        var current = 1;
        for (var index = 1; index < dates.Count; index++)
        {
            if (dates[index] == dates[index - 1].AddDays(1))
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 1;
            }
        }

        return longest;
    }
}
