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
}
