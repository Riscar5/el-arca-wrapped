using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.ElArcaWrapped.Services;

/// <summary>
/// Receives Jellyfin playback events and stores them when tracking is enabled.
/// </summary>
public sealed class PlaybackEventConsumer :
    IEventConsumer<PlaybackStartEventArgs>,
    IEventConsumer<PlaybackProgressEventArgs>,
    IEventConsumer<PlaybackStopEventArgs>
{
    private readonly PlaybackSessionStore _sessionStore;

    public PlaybackEventConsumer(PlaybackSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    public Task OnEvent(PlaybackStartEventArgs eventArgs)
    {
        return RecordIfEnabledAsync(eventArgs);
    }

    public Task OnEvent(PlaybackProgressEventArgs eventArgs)
    {
        return RecordIfEnabledAsync(eventArgs);
    }

    public Task OnEvent(PlaybackStopEventArgs eventArgs)
    {
        return RecordIfEnabledAsync(eventArgs, isStopped: true, playedToCompletion: eventArgs.PlayedToCompletion);
    }

    private Task RecordIfEnabledAsync(
        PlaybackProgressEventArgs eventArgs,
        bool isStopped = false,
        bool playedToCompletion = false)
    {
        if (Plugin.Instance?.Configuration.TrackingEnabled != true)
        {
            return Task.CompletedTask;
        }

        return _sessionStore.RecordAsync(eventArgs, isStopped, playedToCompletion);
    }
}
