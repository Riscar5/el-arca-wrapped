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
            Record("u1", "m1", "Film A", "Movie", new DateTime(2026, 1, 1), 100, genres: ["Drama"]) with { PlayedToCompletion = true },
            Record("u1", "e1", "Episode 1", "Episode", new DateTime(2026, 1, 2), 40, "Serie X", "Drama", "Comedy"),
            Record("u1", "e2", "Episode 2", "Episode", new DateTime(2026, 1, 3), 45, "Serie X", "Comedy"),
            Record("u2", "m2", "Other user's film", "Movie", new DateTime(2026, 1, 4), 120, genres: ["Action"]),
            Record("u1", "m3", "Film next year", "Movie", new DateTime(2027, 1, 1), 90, genres: ["Action"])
        };

        var result = WrappedStatisticsCalculator.Calculate(records, "u1", 2026);

        Assert.Equal(3, result.TotalPlays);
        Assert.Equal(185, result.TotalMinutesWatched, 1);
        Assert.Equal(1, result.MoviePlays);
        Assert.Empty(result.MovieDecades);
        Assert.Equal(2, result.EpisodePlays);
        Assert.Equal(1, result.UniqueMovies);
        Assert.Equal(1, result.UniqueSeries);
        Assert.Equal(3, result.ActiveDays);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), result.CoverageStartUtc);
        Assert.Equal(new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero), result.CoverageEndUtc);
        Assert.Equal(1, result.MonthsWithActivity);
        Assert.Equal(3, result.LongestStreakDays);
        Assert.Equal(3, result.MonthlyActivity[0].Plays);
        Assert.Equal(0, result.MonthlyActivity[1].Plays);
        Assert.Equal("Serie X", Assert.Single(result.TopSeries).Name);
        Assert.Equal(85, result.TopSeries[0].MinutesWatched, 1);
        Assert.Equal(100, Assert.Single(result.TopMovies).MinutesWatched, 1);
        Assert.Equal("Drama", result.TopGenres[0].Genre);
        Assert.Equal(185d / 3, result.AverageMinutesPerPlay, 1);
        Assert.Equal(0, result.RepeatPlays);
        Assert.Equal(1, result.CompletedPlays);
        Assert.Equal(100d / 3, result.CompletionRatePercent, 1);
        Assert.Equal(100, result.LongestViewingDayMinutes, 1);
        Assert.Equal(100, result.LongestPlayMinutes, 1);
        Assert.Equal("Film A", result.LongestPlayItemName);
        Assert.Equal(3, result.HourlyActivityUtc[0].Plays);
        Assert.Equal(1, result.WeekdayActivityUtc[(int)DayOfWeek.Thursday].Plays);
    }

    [Fact]
    public void GroupsMoviesByDecadeUsingKnownProductionYear()
    {
        var records = new[]
        {
            Record("u1", "m1", "Film 1984", "Movie", new DateTime(2026, 1, 1), 90) with { ProductionYear = 1984 },
            Record("u1", "m2", "Film 1989", "Movie", new DateTime(2026, 1, 2), 100) with { ProductionYear = 1989 },
            Record("u1", "m3", "Film 2001", "Movie", new DateTime(2026, 1, 3), 120) with { ProductionYear = 2001 },
            Record("u1", "m4", "Unknown year", "Movie", new DateTime(2026, 1, 4), 80)
        };

        var result = WrappedStatisticsCalculator.Calculate(records, "u1", 2026);


        Assert.Equal(2, result.MovieDecades.Count);
        Assert.Equal(new DecadeActivity(1980, 2, 190), result.MovieDecades[0]);
        Assert.Equal(new DecadeActivity(2000, 1, 120), result.MovieDecades[1]);
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
        Assert.Null(result.CoverageStartUtc);
        Assert.Null(result.CoverageEndUtc);
        Assert.Equal(0, result.MonthsWithActivity);
        Assert.Equal(12, result.MonthlyActivity.Count);
        Assert.Equal(24, result.HourlyActivityUtc.Count);
        Assert.Equal(7, result.WeekdayActivityUtc.Count);
        Assert.Equal(0, result.AverageMinutesPerPlay);
        Assert.Equal(0, result.CompletionRatePercent);
        Assert.Equal(0, result.LongestViewingDayMinutes);
        Assert.Equal(0, result.LongestPlayMinutes);
        Assert.Null(result.LongestPlayItemName);
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
        Assert.Equal(1, result.RepeatPlays);
        Assert.Equal(60, result.LongestViewingDayMinutes, 1);
        Assert.Equal(60, result.LongestPlayMinutes, 1);
        Assert.Equal("Film", result.LongestPlayItemName);
    }
    [Fact]
    public void UsesUtcForYearMonthHourAndWeekdayBuckets()
    {
        var utcYearBoundary = Record("u1", "m1", "Film", "Movie", new DateTime(2027, 1, 1, 1, 0, 0), 30)
            with { StartedAtUtc = new DateTimeOffset(2027, 1, 1, 1, 0, 0, TimeSpan.FromHours(14)) };
        var nextUtcYear = Record("u1", "m2", "Film 2", "Movie", new DateTime(2026, 12, 31, 23, 0, 0), 30)
            with { StartedAtUtc = new DateTimeOffset(2026, 12, 31, 23, 0, 0, TimeSpan.FromHours(-5)) };

        var result = WrappedStatisticsCalculator.Calculate([utcYearBoundary, nextUtcYear], "u1", 2026);

        Assert.Equal(1, result.TotalPlays);
        Assert.Equal(1, result.MonthlyActivity[11].Plays);
        Assert.Equal(0, result.MonthlyActivity[0].Plays);
        Assert.Equal(1, result.HourlyActivityUtc[11].Plays);
        Assert.Equal(1, result.WeekdayActivityUtc[(int)DayOfWeek.Thursday].Plays);
    }

    [Fact]
    public void DoesNotUseElapsedFallbackWhenTrackedWatchedTimeIsZero()
    {
        var record = Record("u1", "m1", "Film", "Movie", new DateTime(2026, 5, 1), 60)
            with { WatchedTicks = 0 };

        var result = WrappedStatisticsCalculator.Calculate([record], "u1", 2026);

        Assert.Equal(0, result.TotalMinutesWatched);
    }

    [Fact]
    public void FallsBackToElapsedTimeAndCapsAtRuntime()
    {
        var record = Record("u1", "m1", "Film", "Movie", new DateTime(2026, 5, 1), 60)
            with { WatchedTicks = null, EndedAtUtc = new DateTimeOffset(2026, 5, 1, 2, 30, 0, TimeSpan.Zero) };

        var result = WrappedStatisticsCalculator.Calculate([record], "u1", 2026);

        Assert.Equal(60, result.TotalMinutesWatched, 1);
    }

}
