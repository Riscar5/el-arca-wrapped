namespace Jellyfin.Plugin.ElArcaWrapped.Statistics;

/// <summary>
/// A normalized playback record used by the statistics engine. This is deliberately
/// independent of Jellyfin event types so calculations can be tested with sample data.
/// </summary>
public sealed record PlaybackRecord
{
    public required string UserId { get; init; }
    public required string ItemId { get; init; }
    public required string ItemName { get; init; }
    public required string ItemType { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset? EndedAtUtc { get; init; }
    public long? RuntimeTicks { get; init; }
    public long? WatchedTicks { get; init; }
    public bool PlayedToCompletion { get; init; }
    public string? SeriesName { get; init; }
    public int? ProductionYear { get; init; }
    public int? SeasonNumber { get; init; }
    public int? EpisodeNumber { get; init; }
    /// <summary>Relative Jellyfin primary-image endpoint; export will embed thumbnails for offline use.</summary>
    public string? PrimaryImagePath { get; init; }
    public IReadOnlyList<string> Genres { get; init; } = Array.Empty<string>();
}

public sealed record RankedItem(string Name, int Plays, double MinutesWatched);
public sealed record MonthlyActivity(int Month, int Plays, double MinutesWatched);
public sealed record GenreActivity(string Genre, int Plays, double MinutesWatched);
public sealed record HourlyActivity(int HourUtc, int Plays, double MinutesWatched);
public sealed record WeekdayActivity(DayOfWeek Day, int Plays, double MinutesWatched);

public sealed record WrappedStatistics
{
    public required string UserId { get; init; }
    public required int Year { get; init; }
    public int TotalPlays { get; init; }
    public double TotalMinutesWatched { get; init; }
    public double AverageMinutesPerPlay { get; init; }
    public int RepeatPlays { get; init; }
    public double LongestViewingDayMinutes { get; init; }
    public int MoviePlays { get; init; }
    public int EpisodePlays { get; init; }
    public int UniqueMovies { get; init; }
    public int UniqueSeries { get; init; }
    public int ActiveDays { get; init; }
    public int LongestStreakDays { get; init; }
    public int CompletedPlays { get; init; }
    public double CompletionRatePercent { get; init; }
    public IReadOnlyList<MonthlyActivity> MonthlyActivity { get; init; } = Array.Empty<MonthlyActivity>();
    public IReadOnlyList<HourlyActivity> HourlyActivityUtc { get; init; } = Array.Empty<HourlyActivity>();
    public IReadOnlyList<WeekdayActivity> WeekdayActivityUtc { get; init; } = Array.Empty<WeekdayActivity>();
    public IReadOnlyList<RankedItem> TopMovies { get; init; } = Array.Empty<RankedItem>();
    public IReadOnlyList<RankedItem> TopSeries { get; init; } = Array.Empty<RankedItem>();
    public IReadOnlyList<GenreActivity> TopGenres { get; init; } = Array.Empty<GenreActivity>();
}
