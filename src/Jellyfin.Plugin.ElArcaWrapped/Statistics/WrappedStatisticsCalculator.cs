namespace Jellyfin.Plugin.ElArcaWrapped.Statistics;

/// <summary>Computes annual metrics from normalized, user-scoped playback records.</summary>
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

        // Enforce user isolation here even when the caller has already filtered the database query.
        var records = source
            .Where(r => string.Equals(r.UserId, userId, StringComparison.Ordinal))
            .Where(r => r.StartedAtUtc.UtcDateTime.Year == year)
            .OrderBy(r => r.StartedAtUtc.UtcDateTime)
            .ToArray();

        var indexed = records.Select((record, index) =>
            (Record: record, Minutes: GetMinutesWatched(record))).ToArray();
        var minutes = indexed.Select(item => item.Minutes).ToArray();
        var movies = indexed.Where(item => IsMovie(item.Record)).ToArray();
        var episodes = indexed.Where(item => IsEpisode(item.Record)).ToArray();

        var monthly = Enumerable.Range(1, 12).Select(month =>
        {
            var items = indexed.Where(item => item.Record.StartedAtUtc.UtcDateTime.Month == month).ToArray();
            return new MonthlyActivity(month, items.Length, items.Sum(item => item.Minutes));
        }).ToArray();

        var hourly = Enumerable.Range(0, 24).Select(hour =>
        {
            var items = indexed.Where(item => item.Record.StartedAtUtc.UtcDateTime.Hour == hour).ToArray();
            return new HourlyActivity(hour, items.Length, items.Sum(item => item.Minutes));
        }).ToArray();

        var weekdays = Enum.GetValues<DayOfWeek>().Select(day =>
        {
            var items = indexed.Where(item => item.Record.StartedAtUtc.UtcDateTime.DayOfWeek == day).ToArray();
            return new WeekdayActivity(day, items.Length, items.Sum(item => item.Minutes));
        }).ToArray();

        var genreCounts = new Dictionary<string, (int Plays, double Minutes)>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in indexed)
        {
            foreach (var genre in item.Record.Genres
                         .Where(g => !string.IsNullOrWhiteSpace(g))
                         .Select(g => g.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var current = genreCounts.GetValueOrDefault(genre);
                genreCounts[genre] = (current.Plays + 1, current.Minutes + item.Minutes);
            }
        }

        var movieDecades = movies
            .Where(item => item.Record.ProductionYear is >= 1900 and <= 9999)
            .GroupBy(item => (item.Record.ProductionYear!.Value / 10) * 10)
            .Select(group => new DecadeActivity(group.Key, group.Count(), group.Sum(item => item.Minutes)))
            .OrderByDescending(item => item.MinutesWatched)
            .ThenByDescending(item => item.Plays)
            .ThenBy(item => item.Decade)
            .ToArray();

        var topGenres = genreCounts
            .OrderByDescending(pair => pair.Value.Minutes)
            .ThenByDescending(pair => pair.Value.Plays)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(pair => new GenreActivity(pair.Key, pair.Value.Plays, pair.Value.Minutes))
            .ToArray();

        var activeDates = records.Select(r => r.StartedAtUtc.UtcDateTime.Date).Distinct().Order().ToArray();
        var distinctItems = records.Select(r => r.ItemId).Distinct(StringComparer.Ordinal).Count();
        var totalMinutes = minutes.Sum();
        var completed = records.Count(r => r.PlayedToCompletion);
        var longestPlay = indexed
            .OrderByDescending(item => item.Minutes)
            .ThenBy(item => item.Record.StartedAtUtc)
            .FirstOrDefault();
        var longestDay = indexed.GroupBy(item => item.Record.StartedAtUtc.UtcDateTime.Date)
            .Select(group => group.Sum(item => item.Minutes))
            .DefaultIfEmpty(0)
            .Max();

        return new WrappedStatistics
        {
            UserId = userId,
            Year = year,
            TotalPlays = records.Length,
            TotalMinutesWatched = totalMinutes,
            AverageMinutesPerPlay = records.Length == 0 ? 0 : totalMinutes / records.Length,
            RepeatPlays = Math.Max(0, records.Length - distinctItems),
            LongestViewingDayMinutes = longestDay,
            LongestPlayMinutes = longestPlay.Minutes,
            LongestPlayItemName = longestPlay.Record?.ItemName,
            MoviePlays = movies.Length,
            EpisodePlays = episodes.Length,
            UniqueMovies = movies.Select(item => item.Record.ItemId).Distinct(StringComparer.Ordinal).Count(),
            UniqueSeries = episodes.Select(item => string.IsNullOrWhiteSpace(item.Record.SeriesName)
                    ? item.Record.ItemName : item.Record.SeriesName!)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            ActiveDays = activeDates.Length,
            CoverageStartUtc = records.Length == 0 ? null : records[0].StartedAtUtc.ToUniversalTime(),
            CoverageEndUtc = records.Length == 0 ? null : records[^1].StartedAtUtc.ToUniversalTime(),
            MonthsWithActivity = monthly.Count(item => item.Plays > 0),
            LongestStreakDays = GetLongestStreak(activeDates),
            CompletedPlays = completed,
            CompletionRatePercent = records.Length == 0 ? 0 : completed * 100d / records.Length,
            MonthlyActivity = monthly,
            HourlyActivityUtc = hourly,
            WeekdayActivityUtc = weekdays,
            TopMovies = RankItems(movies, record => record.ItemName),
            TopSeries = RankItems(episodes,
                record => string.IsNullOrWhiteSpace(record.SeriesName) ? record.ItemName : record.SeriesName!),
            TopGenres = topGenres,
            MovieDecades = movieDecades
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

        // Cap implausible session duration at known runtime (e.g. server event gaps).
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
        foreach (var (record, watchedMinutes) in items)
        {
            var name = nameSelector(record);
            var current = aggregates.GetValueOrDefault(name);
            aggregates[name] = (current.Plays + 1, current.Minutes + watchedMinutes);
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
        if (dates.Count == 0) return 0;
        var longest = 1;
        var current = 1;
        for (var index = 1; index < dates.Count; index++)
        {
            if (dates[index] == dates[index - 1].AddDays(1))
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else current = 1;
        }
        return longest;
    }
}
