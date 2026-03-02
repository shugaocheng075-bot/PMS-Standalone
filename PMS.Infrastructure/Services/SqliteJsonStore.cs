using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace PMS.Infrastructure.Services;

internal static class SqliteJsonStore
{
    private static readonly object SyncRoot = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string DbPath =
        Environment.GetEnvironmentVariable("PMS_SQLITE_PATH")
        ?? Path.Combine(Directory.GetCurrentDirectory(), "pms-data.db");

    private static bool _initialized;

    public static T LoadOrSeed<T>(string key, Func<T> seedFactory)
    {
        lock (SyncRoot)
        {
            EnsureInitialized();

            using var connection = CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT JsonValue FROM AppState WHERE StateKey = $key LIMIT 1";
            command.Parameters.AddWithValue("$key", key);

            var existing = command.ExecuteScalar() as string;
            if (!string.IsNullOrWhiteSpace(existing))
            {
                var restored = JsonSerializer.Deserialize<T>(existing, JsonOptions);
                if (restored is not null)
                {
                    return restored;
                }
            }

            var seeded = seedFactory();
            SaveInternal(connection, key, seeded);
            return seeded;
        }
    }

    public static void Save<T>(string key, T value)
    {
        lock (SyncRoot)
        {
            EnsureInitialized();

            using var connection = CreateConnection();
            connection.Open();
            SaveInternal(connection, key, value);
        }
    }

    private static void SaveInternal<T>(SqliteConnection connection, string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO AppState(StateKey, JsonValue, UpdatedAt)
VALUES($key, $json, $updatedAt)
ON CONFLICT(StateKey) DO UPDATE SET
    JsonValue = excluded.JsonValue,
    UpdatedAt = excluded.UpdatedAt;";

        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$json", json);
        command.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={DbPath}");
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS AppState(
    StateKey TEXT PRIMARY KEY,
    JsonValue TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);";
        command.ExecuteNonQuery();

        _initialized = true;
    }
}
