using Jellyfin.Plugin.ElArcaWrapped.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ElArcaWrapped;

/// <summary>
/// Main entry point for El Arca Wrapped.
/// </summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    public override string Name => "El Arca Wrapped";

    // Stable plugin identifier. Do not change after publishing.
    public override Guid Id => Guid.Parse("9f5b2d8e-4c31-4a76-9d12-6b8e0f3a51c7");
}
