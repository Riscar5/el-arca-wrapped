using Jellyfin.Plugin.ElArcaWrapped.Statistics;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ElArcaWrapped.Export;

/// <summary>Embeds small primary-image thumbnails into a standalone report when available.</summary>
public sealed class ArtworkEmbedder
{
    private const int MaxImageBytes = 400 * 1024;
    private readonly ILibraryManager _libraryManager;
    private readonly IImageProcessor _imageProcessor;
    private readonly ILogger<ArtworkEmbedder> _logger;

    public ArtworkEmbedder(ILibraryManager libraryManager, IImageProcessor imageProcessor, ILogger<ArtworkEmbedder> logger)
    {
        _libraryManager = libraryManager;
        _imageProcessor = imageProcessor;
        _logger = logger;
    }

    public async Task<WrappedReportData> EmbedAsync(WrappedReportData report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var movies = await EmbedItemsAsync(report.Statistics.TopMovies, preferSeriesArtwork: false).ConfigureAwait(false);
        var series = await EmbedItemsAsync(report.Statistics.TopSeries, preferSeriesArtwork: true).ConfigureAwait(false);
        return report with
        {
            Statistics = report.Statistics with
            {
                TopMovies = movies,
                TopSeries = series
            }
        };
    }

    private async Task<IReadOnlyList<RankedItem>> EmbedItemsAsync(IReadOnlyList<RankedItem> ranked, bool preferSeriesArtwork)
    {
        var result = new List<RankedItem>(ranked.Count);
        foreach (var item in ranked)
        {
            if (item.ArtworkItemId is not { } itemId)
            {
                result.Add(item);
                continue;
            }

            try
            {
                var libraryItem = _libraryManager.GetItemById(itemId);
                // Ranked series currently carry a representative episode ID; resolve the parent
                // series so the exported artwork is a series poster rather than episode still.
                if (preferSeriesArtwork && libraryItem is Episode episode && episode.Series is { } parentSeries)
                {
                    libraryItem = parentSeries;
                }
                var image = libraryItem?.GetImageInfo(ImageType.Primary, 0);
                if (libraryItem is null || image is null || string.IsNullOrWhiteSpace(image.Path))
                {
                    result.Add(item);
                    continue;
                }

                var options = new ImageProcessingOptions
                {
                    Item = libraryItem,
                    ItemId = libraryItem.Id,
                    Image = image,
                    MaxWidth = 320,
                    MaxHeight = 480,
                    Quality = 78,
                    SupportedOutputFormats = [ImageFormat.Jpg]
                };

                var processed = await _imageProcessor.ProcessImage(options).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(processed.Path) || !File.Exists(processed.Path))
                {
                    result.Add(item);
                    continue;
                }

                var fileInfo = new FileInfo(processed.Path);
                if (fileInfo.Length <= 0 || fileInfo.Length > MaxImageBytes)
                {
                    result.Add(item);
                    continue;
                }

                var bytes = await File.ReadAllBytesAsync(processed.Path).ConfigureAwait(false);
                if (bytes.Length > MaxImageBytes)
                {
                    result.Add(item);
                    continue;
                }

                var dataUri = "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
                result.Add(item with { ArtworkDataUri = dataUri });
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Could not embed artwork for Wrapped item {ItemName}", item.Name);
                result.Add(item);
            }
        }

        return result;
    }
}
