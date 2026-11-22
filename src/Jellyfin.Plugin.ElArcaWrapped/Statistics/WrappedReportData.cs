namespace Jellyfin.Plugin.ElArcaWrapped.Statistics;

/// <summary>Serializable, user-scoped data contract passed to the annual report renderer.</summary>
public sealed record WrappedReportData
{
    public required string UserId { get; init; }
    public required string DisplayName { get; init; }
    public required int Year { get; init; }
    public required DateTimeOffset GeneratedAtUtc { get; init; }
    public required WrappedStatistics Statistics { get; init; }
    public string CoverageNote { get; init; } = "Solo se incluye la actividad capturada por El Arca Wrapped; puede haber periodos sin datos.";
}

/// <summary>Creates report payloads from already calculated user-scoped statistics.</summary>
public static class WrappedReportDataFactory
{
    public static WrappedReportData Create(WrappedStatistics statistics, string displayName, DateTimeOffset generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new WrappedReportData
        {
            UserId = statistics.UserId,
            DisplayName = displayName.Trim(),
            Year = statistics.Year,
            GeneratedAtUtc = generatedAtUtc.ToUniversalTime(),
            Statistics = statistics
        };
    }
}
