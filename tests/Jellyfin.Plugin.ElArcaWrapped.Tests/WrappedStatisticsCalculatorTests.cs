using Jellyfin.Plugin.ElArcaWrapped.Statistics;
using Xunit;

namespace Jellyfin.Plugin.ElArcaWrapped.Tests;

public sealed class WrappedStatisticsCalculatorTests
{
    private static PlaybackRecord Record(string user, string item, string name, string type, DateTime start,
        double minutes, string? series = null, params string[] genres) => new()
    {
        UserId = user,
        ItemId = item,
        ItemName = name,
        ItemType = type,
        StartedAtUtc = new DateTimeOffset(start, TimeSpan.Zero),
        EndedAtUtc = new DateTimeOffset(start.AddMinutes(minutes), TimeSpan.Zero),
        RuntimeTicks = TimeSpan.FromMinutes(minutes).Ticks,
        SeriesName = series,
        Genres = genres
    };

    [Fact]
    public void CalculatesAnnualTotalsAndSeparatesUsers()
    {
        var records = new[]
        {
            Record("u1", "m1", "Film A", "Movie", new DateTime(2026, 1, 1), 100, genres: ["Drama"]),
            Record("u1", "e1", "Episode 1", "Episode", new DateTime(2026, 1, 2), 40, "Serie X", "Drama", "Comedy"),
            Record("u1", "e2", "Episode 2", "Episode", new DateTime(2026, 1, 3), 45, "Serie X", "Comedy"),
            Record("u2", "m2", "Other user's film", "Movie", new DateTime(2026, 1, 4), 120, genres: ["Action"]),
            Record("u1", "m3", "Film next year", "Movie", new DateTime(2027, 1, 1), 90, genres: ["Action"])
        };

        var result = WrappedStatisticsCalculator.Calculate(records, "u1", 2026);

        Assert.Equal(3, result.TotalPlays);
        Assert.Equal(185, result.TotalMinutesWatched, 1);
        Assert.Equal(1, result.MoviePlays);
        Assert.Equal(2, result.EpisodePlays);
        Assert.Equal(1, result.UniqueMovies);
        Assert.Equal(1, result.UniqueSeries);
        Assert.Equal(3, result.ActiveDays);
        Assert.Equal(3, result.LongestStreakDays);
        Assert.Equal(3, result.MonthlyActivity[0].Plays);
        Assert.Equal(0, result.MonthlyActivity[1].Plays);
        Assert.Equal("Serie X", Assert.Single(result.TopSeries).Name);
        Assert.Equal(85, result.TopSeries[0].MinutesWatched, 1);
        Assert.Equal(100, Assert.Single(result.TopMovies).MinutesWatched, 1);
        Assert.Equal("Drama", result.TopGenres[0].Genre);
    }

    [Fact]
    public void CapsWatchedDurationAtKnownRuntime()
    {
        var record = Record("u1", "m1", "Film", "Movie", new DateTime(2026, 2, 1), 150);
        var overrun = record with { WatchedTicks = TimeSpan.FromMinutes(180).Ticks };

        var result = WrappedStatisticsCalculator.Calculate([overrun], "u1", 2026);

        Assert.Equal(150, result.TotalMinutesWatched, 1);
    }

    [Fact]
    public void EmptyYearReturnsZeroesAndTwelveMonths()
    {
        var result = WrappedStatisticsCalculator.Calculate([], "u1", 2026);

        Assert.Equal(0, result.TotalPlays);
        Assert.Equal(0, result.TotalMinutesWatched);
        Assert.Equal(0, result.LongestStreakDays);
        Assert.Equal(12, result.MonthlyActivity.Count);
    }

    [Fact]
    public void CountsRepeatedPlaysButUniqueItemsOnce()
    {
        var repeated = Record("u1", "m1", "Film", "Movie", new DateTime(2026, 3, 1), 60);
        var secondPlay = repeated with { StartedAtUtc = repeated.StartedAtUtc.AddDays(2) };

        var result = WrappedStatisticsCalculator.Calculate([repeated, secondPlay], "u1", 2026);

        Assert.Equal(2, result.TotalPlays);
        Assert.Equal(1, result.UniqueMovies);
        Assert.Equal(2, Assert.Single(result.TopMovies).Plays);
        Assert.Equal(2, result.ActiveDays);
        Assert.Equal(1, result.LongestStreakDays);
    }
}
