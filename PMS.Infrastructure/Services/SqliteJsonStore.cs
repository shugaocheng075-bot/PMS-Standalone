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

    private static readonly string DbPath = ResolveDbPath();
    private static readonly string ConnectionString =
        $"Data Source={DbPath};Cache=Shared";

    private static bool _initialized;

    private static string ResolveDbPath()
    {
        var fromEnv = Environment.GetEnvironmentVariable("PMS_SQLITE_PATH");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return Path.GetFullPath(fromEnv.Trim());
        }

        var apiProjectCandidate = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "pms-data.db"));

        var apiProjectDir = Path.GetDirectoryName(apiProjectCandidate);
        if (!string.IsNullOrWhiteSpace(apiProjectDir) && Directory.Exists(apiProjectDir))
        {
            return apiProjectCandidate;
        }

        return Path.Combine(AppContext.BaseDirectory, "pms-data.db");
    }

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

    /// <summary>
    /// Saves data to SQLite. Returns true on success, false on failure.
    /// On failure the caller's in-memory state may be inconsistent — callers
    /// should pre-serialize or handle the false return to roll back.
    /// </summary>
    public static bool Save<T>(string key, T value)
    {
        lock (SyncRoot)
        {
            try
            {
                EnsureInitialized();

                using var connection = CreateConnection();
                connection.Open();
                SaveInternal(connection, key, value);
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SqliteJsonStore] Save failed for key '{key}': {ex.Message}");
                return false;
            }
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

    internal static SqliteConnection CreateConnection()
    {
        return new SqliteConnection(ConnectionString);
    }

    /// <summary>
    /// Ensures the AppState table exists and WAL mode is configured.
    /// Called by SqliteTableStore to guarantee pragmas are applied early.
    /// </summary>
    internal static void EnsureReady() => EnsureInitialized();

    private static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        using var connection = CreateConnection();
        connection.Open();

        // Enable WAL mode for better concurrent read/write performance and crash recovery
        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
            pragma.ExecuteNonQuery();
        }

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
