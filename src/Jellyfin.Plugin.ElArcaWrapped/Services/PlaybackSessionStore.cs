using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.ElArcaWrapped.Services;

/// <summary>
/// Persists playback sessions locally, keeping one record per Jellyfin user and play session.
/// </summary>
public sealed class PlaybackSessionStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    public PlaybackSessionStore(IApplicationPaths applicationPaths)
    {
        var dataDirectory = Path.Combine(applicationPaths.DataPath, "el-arca-wrapped");
        Directory.CreateDirectory(dataDirectory);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataDirectory, "playback-sessions.db"),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public async Task RecordAsync(
        PlaybackProgressEventArgs eventArgs,
        bool isStopped = false,
        bool playedToCompletion = false)
    {
        if (eventArgs.Item is null || eventArgs.Item.IsThemeMedia || eventArgs.Users.Count == 0)
        {
            return;
        }

        var item = eventArgs.Item;
        var playSessionId = eventArgs.PlaySessionId;
        if (string.IsNullOrWhiteSpace(playSessionId))
        {
            playSessionId = eventArgs.Session?.Id;
        }

        if (string.IsNullOrWhiteSpace(playSessionId))
        {
            // Do not merge unrelated sessions when Jellyfin supplies no session identifier.
            playSessionId = Guid.NewGuid().ToString("N");
        }

        await EnsureDatabaseAsync().ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        foreach (var user in eventArgs.Users)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO PlaybackSessions (
                    PlaySessionId, UserId, Username, ItemId, ItemName, ItemType,
                    StartedAtUtc, LastEventAtUtc, EndedAtUtc, LastPositionTicks,
                    RuntimeTicks, IsPaused, PlayedToCompletion, ClientName,
                    DeviceName, DeviceId
                )
                VALUES (
                    $playSessionId, $userId, $username, $itemId, $itemName, $itemType,
                    $now, $now, $endedAt, $positionTicks,
                    $runtimeTicks, $isPaused, $completed, $clientName,
                    $deviceName, $deviceId
                )
                ON CONFLICT(PlaySessionId, UserId) DO UPDATE SET
                    Username = excluded.Username,
                    ItemId = excluded.ItemId,
                    ItemName = excluded.ItemName,
                    ItemType = excluded.ItemType,
                    LastEventAtUtc = excluded.LastEventAtUtc,
                    EndedAtUtc = COALESCE(excluded.EndedAtUtc, PlaybackSessions.EndedAtUtc),
                    LastPositionTicks = COALESCE(excluded.LastPositionTicks, PlaybackSessions.LastPositionTicks),
                    RuntimeTicks = COALESCE(excluded.RuntimeTicks, PlaybackSessions.RuntimeTicks),
                    IsPaused = excluded.IsPaused,
                    PlayedToCompletion = MAX(PlaybackSessions.PlayedToCompletion, excluded.PlayedToCompletion),
                    ClientName = excluded.ClientName,
                    DeviceName = excluded.DeviceName,
                    DeviceId = excluded.DeviceId;
                """;

            var now = DateTimeOffset.UtcNow.ToString("O");
            command.Parameters.AddWithValue("$playSessionId", playSessionId);
            command.Parameters.AddWithValue("$userId", user.Id.ToString("N"));
            command.Parameters.AddWithValue("$username", user.Username ?? string.Empty);
            command.Parameters.AddWithValue("$itemId", item.Id.ToString("N"));
            command.Parameters.AddWithValue("$itemName", item.Name ?? string.Empty);
            command.Parameters.AddWithValue("$itemType", item.GetType().Name);
            command.Parameters.AddWithValue("$now", now);
            command.Parameters.AddWithValue("$endedAt", isStopped ? now : DBNull.Value);
            command.Parameters.AddWithValue("$positionTicks", eventArgs.PlaybackPositionTicks is long position ? position : DBNull.Value);
            command.Parameters.AddWithValue("$runtimeTicks", item.RunTimeTicks is long runtime ? runtime : DBNull.Value);
            command.Parameters.AddWithValue("$isPaused", isStopped ? false : eventArgs.IsPaused);
            command.Parameters.AddWithValue("$completed", playedToCompletion);
            command.Parameters.AddWithValue("$clientName", eventArgs.ClientName ?? string.Empty);
            command.Parameters.AddWithValue("$deviceName", eventArgs.DeviceName ?? string.Empty);
            command.Parameters.AddWithValue("$deviceId", eventArgs.DeviceId ?? string.Empty);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }

    private async Task EnsureDatabaseAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS PlaybackSessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlaySessionId TEXT NOT NULL,
                    UserId TEXT NOT NULL,
                    Username TEXT NOT NULL,
                    ItemId TEXT NOT NULL,
                    ItemName TEXT NOT NULL,
                    ItemType TEXT NOT NULL,
                    StartedAtUtc TEXT NOT NULL,
                    LastEventAtUtc TEXT NOT NULL,
                    EndedAtUtc TEXT NULL,
                    LastPositionTicks INTEGER NULL,
                    RuntimeTicks INTEGER NULL,
                    IsPaused INTEGER NOT NULL DEFAULT 0,
                    PlayedToCompletion INTEGER NOT NULL DEFAULT 0,
                    ClientName TEXT NOT NULL DEFAULT '',
                    DeviceName TEXT NOT NULL DEFAULT '',
                    DeviceId TEXT NOT NULL DEFAULT '',
                    UNIQUE (PlaySessionId, UserId)
                );
                CREATE INDEX IF NOT EXISTS IX_PlaybackSessions_User_Started
                    ON PlaybackSessions (UserId, StartedAtUtc);
                """;
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
