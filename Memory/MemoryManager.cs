using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace PrivateAIOverlay.Memory;

public sealed class MemoryManager
{
    private readonly string _dbPath;

    public MemoryManager(string? dbPath = null)
    {
        _dbPath = dbPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrivateAIOverlay", "memory.db");
        Initialize();
    }

    public void Initialize()
    {
        var directory = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS memories (
                id TEXT PRIMARY KEY,
                content TEXT NOT NULL,
                category TEXT NOT NULL DEFAULT 'General',
                scope TEXT NOT NULL DEFAULT 'Global',
                importance INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                source TEXT NOT NULL DEFAULT 'user',
                active INTEGER NOT NULL DEFAULT 1,
                metadata TEXT NOT NULL DEFAULT '{}'
            );";
        command.ExecuteNonQuery();
    }

    public bool Save(MemoryRecord record)
    {
        if (record is null || string.IsNullOrWhiteSpace(record.Content))
        {
            return false;
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO memories(id, content, category, scope, importance, created_at, updated_at, source, active, metadata)
            VALUES (@id, @content, @category, @scope, @importance, @createdAt, @updatedAt, @source, @active, @metadata)
            ON CONFLICT(id) DO UPDATE SET
                content = excluded.content,
                category = excluded.category,
                scope = excluded.scope,
                importance = excluded.importance,
                updated_at = excluded.updated_at,
                source = excluded.source,
                active = excluded.active,
                metadata = excluded.metadata;";

        command.Parameters.AddWithValue("@id", record.Id);
        command.Parameters.AddWithValue("@content", record.Content);
        command.Parameters.AddWithValue("@category", record.Category);
        command.Parameters.AddWithValue("@scope", record.Scope);
        command.Parameters.AddWithValue("@importance", record.Importance);
        command.Parameters.AddWithValue("@createdAt", record.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("@source", record.Source);
        command.Parameters.AddWithValue("@active", record.Active ? 1 : 0);
        command.Parameters.AddWithValue("@metadata", record.Metadata);

        return command.ExecuteNonQuery() > 0;
    }

    public IReadOnlyList<MemoryRecord> GetRelevant(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<MemoryRecord>();
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, content, category, scope, importance, created_at, updated_at, source, active, metadata
            FROM memories
            WHERE active = 1 AND (
                content LIKE '%' || @query || '%' OR
                category LIKE '%' || @query || '%' OR
                scope LIKE '%' || @query || '%')
            ORDER BY importance DESC, updated_at DESC
            LIMIT 10;";
        command.Parameters.AddWithValue("@query", query);

        using var reader = command.ExecuteReader();
        var results = new List<MemoryRecord>();
        while (reader.Read())
        {
            results.Add(new MemoryRecord
            {
                Id = reader.GetString(0),
                Content = reader.GetString(1),
                Category = reader.GetString(2),
                Scope = reader.GetString(3),
                Importance = reader.GetInt32(4),
                CreatedAt = DateTime.Parse(reader.GetString(5)),
                UpdatedAt = DateTime.Parse(reader.GetString(6)),
                Source = reader.GetString(7),
                Active = reader.GetInt32(8) == 1,
                Metadata = reader.GetString(9)
            });
        }

        return results;
    }

    public bool Delete(string id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM memories WHERE id = @id;";
        command.Parameters.AddWithValue("@id", id);
        return command.ExecuteNonQuery() > 0;
    }

    public void ClearAll()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM memories;";
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<MemoryRecord> GetAll()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"SELECT id, content, category, scope, importance, created_at, updated_at, source, active, metadata FROM memories ORDER BY updated_at DESC;";

        using var reader = command.ExecuteReader();
        var results = new List<MemoryRecord>();
        while (reader.Read())
        {
            results.Add(new MemoryRecord
            {
                Id = reader.GetString(0),
                Content = reader.GetString(1),
                Category = reader.GetString(2),
                Scope = reader.GetString(3),
                Importance = reader.GetInt32(4),
                CreatedAt = DateTime.Parse(reader.GetString(5)),
                UpdatedAt = DateTime.Parse(reader.GetString(6)),
                Source = reader.GetString(7),
                Active = reader.GetInt32(8) == 1,
                Metadata = reader.GetString(9)
            });
        }

        return results;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        return connection;
    }
}
