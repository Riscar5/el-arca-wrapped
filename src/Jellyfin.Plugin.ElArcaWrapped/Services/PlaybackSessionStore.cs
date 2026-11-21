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
    private readonly string _databasePath;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    public PlaybackSessionStore(IApplicationPaths applicationPaths)
        : this(Path.Combine(applicationPaths.DataPath, "el-arca-wrapped", "playback-sessions.db"))
    {
    }

    internal PlaybackSessionStore(string databasePath)
    {
        _databasePath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    internal string DatabasePath => _databasePath;

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
            // Keep progress updates grouped if a client omits Jellyfin's play-session identifier.
            // This fallback is intentionally best-effort and can merge repeated plays on the same device.
            var clientKey = eventArgs.DeviceId ?? eventArgs.DeviceName ?? eventArgs.ClientName ?? "unknown-client";
            playSessionId = $"fallback:{clientKey}:{item.Id:N}";
        }

        await EnsureDatabaseAsync().ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        foreach (var user in eventArgs.Users)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
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

        await transaction.CommitAsync().ConfigureAwait(false);
    }

    /// <summary>Reads a normalized, user-scoped annual snapshot for the statistics engine.</summary>
    public async Task<IReadOnlyList<Statistics.PlaybackRecord>> GetRecordsAsync(string userId, int year)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (year is < 1900 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));

        await EnsureDatabaseAsync().ConfigureAwait(false);
        var records = new List<Statistics.PlaybackRecord>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT UserId, ItemId, ItemName, ItemType, StartedAtUtc, EndedAtUtc,
                   RuntimeTicks, PlayedToCompletion
            FROM PlaybackSessions
            WHERE UserId = $userId
            ORDER BY StartedAtUtc;
            """;
        command.Parameters.AddWithValue("$userId", userId);
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var started = DateTimeOffset.Parse(reader.GetString(4), System.Globalization.CultureInfo.InvariantCulture);
            if (started.UtcDateTime.Year != year) continue;
            DateTimeOffset? ended = reader.IsDBNull(5) ? null :
                DateTimeOffset.Parse(reader.GetString(5), System.Globalization.CultureInfo.InvariantCulture);
            records.Add(new Statistics.PlaybackRecord
            {
                UserId = reader.GetString(0),
                ItemId = reader.GetString(1),
                ItemName = reader.GetString(2),
                ItemType = reader.GetString(3),
                StartedAtUtc = started,
                EndedAtUtc = ended,
                RuntimeTicks = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                // Position is not watched duration. The calculator uses elapsed time as a capped fallback.
                WatchedTicks = null,
                PlayedToCompletion = reader.GetBoolean(7)
            });
        }
        return records;
    }

    internal async Task EnsureDatabaseAsync()
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
