using Jellyfin.Plugin.ElArcaWrapped.Services;
using Jellyfin.Plugin.ElArcaWrapped.Export;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ElArcaWrapped;

/// <summary>
/// Registers the plugin's playback event consumers and persistence service.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<PlaybackSessionStore>();
        serviceCollection.AddSingleton<StandaloneHtmlRenderer>();
        serviceCollection.AddScoped<PlaybackEventConsumer>();
        serviceCollection.AddScoped<IEventConsumer<PlaybackStartEventArgs>>(
            provider => provider.GetRequiredService<PlaybackEventConsumer>());
        serviceCollection.AddScoped<IEventConsumer<PlaybackProgressEventArgs>>(
            provider => provider.GetRequiredService<PlaybackEventConsumer>());
        serviceCollection.AddScoped<IEventConsumer<PlaybackStopEventArgs>>(
            provider => provider.GetRequiredService<PlaybackEventConsumer>());
    }
}
