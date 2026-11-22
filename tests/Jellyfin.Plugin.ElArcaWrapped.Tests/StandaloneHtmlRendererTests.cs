using Jellyfin.Plugin.ElArcaWrapped.Export;
using Jellyfin.Plugin.ElArcaWrapped.Statistics;
using Xunit;

namespace Jellyfin.Plugin.ElArcaWrapped.Tests;

public sealed class StandaloneHtmlRendererTests
{
    [Fact]
    public void ProducesSelfContainedHtmlAndEncodesUserContent()
    {
        var record = new PlaybackRecord
        {
            UserId = "u1", ItemId = "movie1", ItemName = "Film <script>alert(1)</script>", ItemType = "Movie",
            StartedAtUtc = new DateTimeOffset(2026, 2, 1, 20, 0, 0, TimeSpan.Zero),
            EndedAtUtc = new DateTimeOffset(2026, 2, 1, 21, 0, 0, TimeSpan.Zero),
            RuntimeTicks = TimeSpan.FromHours(1).Ticks, WatchedTicks = TimeSpan.FromHours(1).Ticks,
            PlayedToCompletion = true, ProductionYear = 2024, Genres = ["Drama"]
        };
        var stats = WrappedStatisticsCalculator.Calculate([record], "u1", 2026) with
        {
            TopMovies = [new RankedItem("Film", 1, 60, Guid.Parse("00000000-0000-0000-0000-000000000001"), "data:image/jpeg;base64,AA==")]
        };
        var report = WrappedReportDataFactory.Create(stats, "User <img src=x onerror=alert(1)>", DateTimeOffset.UtcNow);
        var html = new StandaloneHtmlRenderer().Render(report);

        Assert.Contains("<!doctype html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<style>", html);
        Assert.Contains("2026", html);
        Assert.Contains("User &lt;img src=x onerror=alert(1)&gt;", html);
        Assert.Contains("Film &lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("Feb", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tus g", html);
        Assert.Contains("Drama", html);
        Assert.Contains("Tu horario", html);
        Assert.Contains("Tus d", html);
        Assert.Contains("poster", html);
        Assert.Contains("data:image/jpeg;base64,AA==", html);
    }
}
