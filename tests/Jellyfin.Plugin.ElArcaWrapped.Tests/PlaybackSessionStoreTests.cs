using Xunit;
using Jellyfin.Plugin.ElArcaWrapped.Services;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.ElArcaWrapped.Tests;

public sealed class PlaybackSessionStoreTests
{
    [Fact]
    public async Task EnsureDatabaseCreatesSessionTableAndIndex()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"el-arca-wrapped-{Guid.NewGuid():N}.db");
        try
        {
            var store = new PlaybackSessionStore(databasePath);
            await store.EnsureDatabaseAsync();

            Assert.True(File.Exists(databasePath));

            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString());
            await connection.OpenAsync();

            await using var tableCommand = connection.CreateCommand();
            tableCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='PlaybackSessions';";
            Assert.Equal("PlaybackSessions", await tableCommand.ExecuteScalarAsync());

            await using var indexCommand = connection.CreateCommand();
            indexCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='index' AND name='IX_PlaybackSessions_User_Started';";
            Assert.Equal("IX_PlaybackSessions_User_Started", await indexCommand.ExecuteScalarAsync());
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task EnsureDatabaseIsIdempotent()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"el-arca-wrapped-{Guid.NewGuid():N}.db");
        try
        {
            var store = new PlaybackSessionStore(databasePath);
            await store.EnsureDatabaseAsync();
            await store.EnsureDatabaseAsync();

            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString());
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='PlaybackSessions';";
            Assert.Equal(1L, await command.ExecuteScalarAsync());
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
    [Fact]
    public async Task AnnualQueryFiltersUserAndUtcYearAndKeepsMetadata()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"el-arca-wrapped-{Guid.NewGuid():N}.db");
        try
        {
            var store = new PlaybackSessionStore(databasePath);
            await store.EnsureDatabaseAsync();
            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWrite
            }.ToString());
            await connection.OpenAsync();

            await InsertAsync(connection, "session-1", "user-a", "Movie A", "Movie",
                "2026-06-01T12:00:00.0000000+00:00", "2026-06-01T12:45:00.0000000+00:00", 72000000000L, true);
            await InsertAsync(connection, "session-2", "user-b", "Movie B", "Movie",
                "2026-06-01T12:00:00.0000000+00:00", "2026-06-01T12:30:00.0000000+00:00", 72000000000L, false);
            await InsertAsync(connection, "session-3", "user-a", "Movie C", "Movie",
                "2025-12-31T23:30:00.0000000+02:00", "2026-01-01T00:10:00.0000000+00:00", 36000000000L, false);

            await using (var metadata = connection.CreateCommand())
            {
                metadata.CommandText = "UPDATE PlaybackSessions SET GenresJson = '[\"Sci-Fi\",\"Drama\"]', SeriesName = 'Series X', ProductionYear = 2024, SeasonNumber = 2, EpisodeNumber = 5, PrimaryImagePath = '/Items/abc/Images/Primary?maxWidth=600&quality=90' WHERE PlaySessionId = 'session-1';";
                await metadata.ExecuteNonQueryAsync();
            }

            var records = await store.GetRecordsAsync("user-a", 2026);

            Assert.Single(records);
            Assert.Equal("Movie A", records[0].ItemName);
            Assert.Equal("user-a", records[0].UserId);
            Assert.Equal(72000000000L, records[0].RuntimeTicks);
            Assert.True(records[0].PlayedToCompletion);
            Assert.Null(records[0].WatchedTicks);
            Assert.Equal(new[] { "Sci-Fi", "Drama" }, records[0].Genres);
            Assert.Equal("Series X", records[0].SeriesName);
            Assert.Equal(2024, records[0].ProductionYear);
            Assert.Equal(2, records[0].SeasonNumber);
            Assert.Equal(5, records[0].EpisodeNumber);
            Assert.Equal("/Items/abc/Images/Primary?maxWidth=600&quality=90", records[0].PrimaryImagePath);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private static async Task InsertAsync(
        SqliteConnection connection, string sessionId, string userId, string name, string type,
        string started, string ended, long runtime, bool completed)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO PlaybackSessions (
                PlaySessionId, UserId, Username, ItemId, ItemName, ItemType, StartedAtUtc,
                LastEventAtUtc, EndedAtUtc, LastPositionTicks, RuntimeTicks, IsPaused,
                PlayedToCompletion, ClientName, DeviceName, DeviceId
            ) VALUES ($session, $user, '', $itemId, $name, $type, $started, $started,
                      $ended, NULL, $runtime, 0, $completed, '', '', '');
            """;
        command.Parameters.AddWithValue("$session", sessionId);
        command.Parameters.AddWithValue("$user", userId);
        command.Parameters.AddWithValue("$itemId", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$type", type);
        command.Parameters.AddWithValue("$started", started);
        command.Parameters.AddWithValue("$ended", ended);
        command.Parameters.AddWithValue("$runtime", runtime);
        command.Parameters.AddWithValue("$completed", completed);
        await command.ExecuteNonQueryAsync();
    }


}
