using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace PMS.Infrastructure.Services;

/// <summary>
/// Per-entity relational table store.
/// Replaces full-collection JSON serialization (SqliteJsonStore) with per-row SQL operations.
/// Auto-migrates legacy JSON data from AppState on first use.
/// </summary>
internal static class SqliteTableStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> InitializedTables = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object InitLock = new();
    private static readonly Dictionary<Type, List<ColumnDef>> ColumnCache = [];

    private sealed record ColumnDef(string Name, string SqlType, PropertyInfo Prop, bool IsJson);

    // ────────────── Public API ──────────────

    /// <summary>
    /// Creates the table if needed, loads all rows.
    /// If <paramref name="legacyJsonKey"/> is set and the table is empty,
    /// auto-migrates data from the old AppState JSON blob.
    /// </summary>
    public static List<T> LoadAll<T>(string tableName, string? legacyJsonKey = null) where T : new()
    {
        EnsureTable<T>(tableName);

        using var conn = SqliteJsonStore.CreateConnection();
        conn.Open();

        var records = ReadAllRows<T>(conn, tableName);

        if (records.Count == 0 && !string.IsNullOrEmpty(legacyJsonKey))
        {
            records = MigrateFromJson<T>(conn, tableName, legacyJsonKey);
        }

        return records;
    }

    /// <summary>Insert a single entity row.</summary>
    public static bool Insert<T>(string tableName, T entity)
    {
        try
        {
            using var conn = SqliteJsonStore.CreateConnection();
            conn.Open();
            InsertRow(conn, tableName, entity);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SqliteTableStore] Insert failed for {tableName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>Update a single entity row by Id.</summary>
    public static bool Update<T>(string tableName, T entity, long id)
    {
        try
        {
            using var conn = SqliteJsonStore.CreateConnection();
            conn.Open();
            UpdateRow(conn, tableName, entity, id);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SqliteTableStore] Update failed for {tableName}/{id}: {ex.Message}");
            return false;
        }
    }

    /// <summary>Delete a single row by Id.</summary>
    public static bool Delete(string tableName, long id)
    {
        try
        {
            using var conn = SqliteJsonStore.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM [{tableName}] WHERE [Id] = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SqliteTableStore] Delete failed for {tableName}/{id}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Atomically replace all rows in a table (DELETE + INSERT in a transaction).
    /// Used for bulk imports / full replacements.
    /// </summary>
    public static bool ReplaceAll<T>(string tableName, List<T> entities)
    {
        try
        {
            EnsureTable<T>(tableName);
            using var conn = SqliteJsonStore.CreateConnection();
            conn.Open();

            using var tx = conn.BeginTransaction();

            using (var clearCmd = conn.CreateCommand())
            {
                clearCmd.CommandText = $"DELETE FROM [{tableName}]";
                clearCmd.ExecuteNonQuery();
            }

            var columns = GetColumns<T>();
            // Reuse a single command with parameters for all inserts (much faster)
            using var insertCmd = BuildInsertCommand(conn, tableName, columns);
            foreach (var entity in entities)
            {
                SetInsertParameters(insertCmd, columns, entity);
                insertCmd.ExecuteNonQuery();
            }

            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SqliteTableStore] ReplaceAll failed for {tableName}: {ex.Message}");
            return false;
        }
    }

    // ────────────── Table Schema ──────────────

    private static void EnsureTable<T>(string tableName)
    {
        lock (InitLock)
        {
            if (InitializedTables.Contains(tableName)) return;

            // Make sure WAL pragmas are applied
            SqliteJsonStore.EnsureReady();

            var columns = GetColumns<T>();
            var columnDefs = string.Join(",\n    ",
                columns.Select(c =>
                    c.Name == "Id"
                        ? $"[{c.Name}] {c.SqlType} PRIMARY KEY"
                        : $"[{c.Name}] {c.SqlType}"));

            using var conn = SqliteJsonStore.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE TABLE IF NOT EXISTS [{tableName}] (\n    {columnDefs}\n)";
            cmd.ExecuteNonQuery();

            InitializedTables.Add(tableName);
        }
    }

    private static List<ColumnDef> GetColumns<T>()
    {
        var type = typeof(T);
        lock (ColumnCache)
        {
            if (ColumnCache.TryGetValue(type, out var cached)) return cached;

            var columns = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Select(p =>
                {
                    var (sqlType, isJson) = MapPropertyType(p.PropertyType);
                    return new ColumnDef(p.Name, sqlType, p, isJson);
                })
                .ToList();

            ColumnCache[type] = columns;
            return columns;
        }
    }

    private static (string sqlType, bool isJson) MapPropertyType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(long) || underlying == typeof(int) || underlying == typeof(bool))
            return ("INTEGER", false);
        if (underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float))
            return ("REAL", false);
        if (underlying == typeof(string))
            return ("TEXT", false);
        if (underlying == typeof(DateTime))
            return ("TEXT", false);

        // Complex types (List<T>, Dictionary, nested objects) → JSON TEXT
        return ("TEXT", true);
    }

    // ────────────── Row I/O ──────────────

    private static List<T> ReadAllRows<T>(SqliteConnection conn, string tableName) where T : new()
    {
        var columns = GetColumns<T>();
        var result = new List<T>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM [{tableName}]";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var entity = new T();
            foreach (var col in columns)
            {
                try
                {
                    var ordinal = reader.GetOrdinal(col.Name);
                    if (reader.IsDBNull(ordinal)) continue;
                    SetPropertyValue(col, entity, reader.GetValue(ordinal));
                }
                catch (IndexOutOfRangeException)
                {
                    // Column doesn't exist in DB yet (entity gained a new property) — skip
                }
            }
            result.Add(entity);
        }

        return result;
    }

    private static SqliteCommand BuildInsertCommand(SqliteConnection conn, string tableName, List<ColumnDef> columns)
    {
        var colNames = string.Join(", ", columns.Select(c => $"[{c.Name}]"));
        var paramPlaceholders = string.Join(", ", columns.Select((_, i) => $"$p{i}"));

        var cmd = conn.CreateCommand();
        cmd.CommandText = $"INSERT INTO [{tableName}] ({colNames}) VALUES ({paramPlaceholders})";

        for (int i = 0; i < columns.Count; i++)
        {
            cmd.Parameters.Add(new SqliteParameter($"$p{i}", DBNull.Value));
        }

        return cmd;
    }

    private static void SetInsertParameters<T>(SqliteCommand cmd, List<ColumnDef> columns, T entity)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            cmd.Parameters[$"$p{i}"].Value = GetPropertyDbValue(columns[i], entity);
        }
    }

    private static void InsertRow<T>(SqliteConnection conn, string tableName, T entity)
    {
        var columns = GetColumns<T>();
        var colNames = string.Join(", ", columns.Select(c => $"[{c.Name}]"));
        var paramPlaceholders = string.Join(", ", columns.Select((_, i) => $"$p{i}"));

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"INSERT INTO [{tableName}] ({colNames}) VALUES ({paramPlaceholders})";

        for (int i = 0; i < columns.Count; i++)
        {
            cmd.Parameters.AddWithValue($"$p{i}", GetPropertyDbValue(columns[i], entity));
        }

        cmd.ExecuteNonQuery();
    }

    private static void UpdateRow<T>(SqliteConnection conn, string tableName, T entity, long id)
    {
        var columns = GetColumns<T>();
        var nonIdCols = columns.Where(c => c.Name != "Id").ToList();
        var setClauses = string.Join(", ", nonIdCols.Select((c, i) => $"[{c.Name}] = $p{i}"));

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"UPDATE [{tableName}] SET {setClauses} WHERE [Id] = $id";
        cmd.Parameters.AddWithValue("$id", id);

        for (int i = 0; i < nonIdCols.Count; i++)
        {
            cmd.Parameters.AddWithValue($"$p{i}", GetPropertyDbValue(nonIdCols[i], entity));
        }

        cmd.ExecuteNonQuery();
    }

    // ────────────── Value Mapping ──────────────

    private static object GetPropertyDbValue<T>(ColumnDef col, T entity)
    {
        var value = col.Prop.GetValue(entity);
        if (value is null) return DBNull.Value;

        if (col.IsJson)
            return JsonSerializer.Serialize(value, JsonOpts);

        var underlying = Nullable.GetUnderlyingType(col.Prop.PropertyType) ?? col.Prop.PropertyType;

        if (underlying == typeof(DateTime))
            return ((DateTime)value).ToString("O");
        if (underlying == typeof(bool))
            return (bool)value ? 1L : 0L;
        if (underlying == typeof(decimal))
            return (double)(decimal)value;

        return value;
    }

    private static void SetPropertyValue(ColumnDef col, object entity, object dbValue)
    {
        if (dbValue is DBNull) return;

        if (col.IsJson)
        {
            var jsonStr = dbValue.ToString();
            if (!string.IsNullOrEmpty(jsonStr))
            {
                var deserialized = JsonSerializer.Deserialize(jsonStr, col.Prop.PropertyType, JsonOpts);
                col.Prop.SetValue(entity, deserialized);
            }
            return;
        }

        var targetType = Nullable.GetUnderlyingType(col.Prop.PropertyType) ?? col.Prop.PropertyType;

        if (targetType == typeof(string))
            col.Prop.SetValue(entity, dbValue.ToString());
        else if (targetType == typeof(long))
            col.Prop.SetValue(entity, Convert.ToInt64(dbValue));
        else if (targetType == typeof(int))
            col.Prop.SetValue(entity, Convert.ToInt32(dbValue));
        else if (targetType == typeof(decimal))
            col.Prop.SetValue(entity, Convert.ToDecimal(dbValue));
        else if (targetType == typeof(double))
            col.Prop.SetValue(entity, Convert.ToDouble(dbValue));
        else if (targetType == typeof(float))
            col.Prop.SetValue(entity, Convert.ToSingle(dbValue));
        else if (targetType == typeof(bool))
            col.Prop.SetValue(entity, Convert.ToBoolean(dbValue));
        else if (targetType == typeof(DateTime))
        {
            if (DateTime.TryParse(dbValue.ToString(), out var dt))
                col.Prop.SetValue(entity, dt);
        }
    }

    // ────────────── Legacy Migration ──────────────

    private static List<T> MigrateFromJson<T>(SqliteConnection conn, string tableName, string legacyJsonKey)
        where T : new()
    {
        using var readCmd = conn.CreateCommand();
        readCmd.CommandText = "SELECT JsonValue FROM AppState WHERE StateKey = $key LIMIT 1";
        readCmd.Parameters.AddWithValue("$key", legacyJsonKey);

        var jsonStr = readCmd.ExecuteScalar() as string;
        if (string.IsNullOrWhiteSpace(jsonStr)) return [];

        List<T>? records;
        try
        {
            records = JsonSerializer.Deserialize<List<T>>(jsonStr, JsonOpts);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"[SqliteTableStore] Failed to deserialize legacy JSON for '{legacyJsonKey}': {ex.Message}");
            return [];
        }

        if (records is null || records.Count == 0) return [];

        Console.WriteLine($"[SqliteTableStore] Migrating {records.Count} records from '{legacyJsonKey}' → [{tableName}]");

        var columns = GetColumns<T>();
        using var tx = conn.BeginTransaction();

        using var insertCmd = BuildInsertCommand(conn, tableName, columns);
        foreach (var record in records)
        {
            SetInsertParameters(insertCmd, columns, record);
            insertCmd.ExecuteNonQuery();
        }

        // Remove legacy JSON key
        using (var delCmd = conn.CreateCommand())
        {
            delCmd.CommandText = "DELETE FROM AppState WHERE StateKey = $key";
            delCmd.Parameters.AddWithValue("$key", legacyJsonKey);
            delCmd.ExecuteNonQuery();
        }

        tx.Commit();
        Console.WriteLine($"[SqliteTableStore] Migration complete: [{tableName}] ← {records.Count} records");

        return records;
    }
}
