using Jellyfin.Plugin.ElArcaWrapped.Statistics;
using Xunit;

namespace Jellyfin.Plugin.ElArcaWrapped.Tests;

public sealed class WrappedReportDataFactoryTests
{
    [Fact]
    public void BuildsReportPayloadWithUserYearAndUtcGenerationTimestamp()
    {
        var stats = WrappedStatisticsCalculator.Calculate([], "user-123", 2026);
        var generated = new DateTimeOffset(2026, 11, 22, 15, 0, 0, TimeSpan.FromHours(1));

        var report = WrappedReportDataFactory.Create(stats, "  Usuario  ", generated);

        Assert.Equal("user-123", report.UserId);
        Assert.Equal("Usuario", report.DisplayName);
        Assert.Equal(2026, report.Year);
        Assert.Equal(generated.ToUniversalTime(), report.GeneratedAtUtc);
        Assert.Same(stats, report.Statistics);
        Assert.Contains("capturada", report.CoverageNote, StringComparison.OrdinalIgnoreCase);
    }
}
