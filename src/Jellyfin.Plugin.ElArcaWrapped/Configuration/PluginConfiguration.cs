using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ElArcaWrapped.Configuration;

/// <summary>
/// Persistent configuration for El Arca Wrapped.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets whether playback tracking is enabled.
    /// Disabled by default until playback capture is validated on a live Jellyfin server.
    /// </summary>
    public bool TrackingEnabled { get; set; } = false;
}
