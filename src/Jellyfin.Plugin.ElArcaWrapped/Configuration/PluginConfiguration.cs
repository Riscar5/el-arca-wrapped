using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ElArcaWrapped.Configuration;

/// <summary>
/// Persistent configuration for El Arca Wrapped.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets whether playback tracking is enabled.
    /// Disabled by default until the tracking service is implemented and tested.
    /// </summary>
    public bool TrackingEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of days after which incomplete sessions are considered stale.
    /// </summary>
    public int StaleSessionDays { get; set; } = 2;
}
