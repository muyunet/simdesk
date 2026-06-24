using Microsoft.Data.Sqlite;
using SimDesk.Core.Models;

namespace SimDesk.Core.Data;

/// <summary>
/// SQLite 日志存储
/// </summary>
public class SqliteLogStore : IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public SqliteLogStore(string appDataDir)
    {
        _dbPath = Path.Combine(appDataDir, "logs.db");
    }

    /// <summary>
    /// 初始化数据库和表结构
    /// </summary>
    public void Initialize()
    {
        var dir = Path.GetDirectoryName(_dbPath)!;
        Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS events (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                box_id TEXT NOT NULL,
                box_name TEXT NOT NULL,
                event_type TEXT NOT NULL,
                timestamp TEXT NOT NULL,
                summary TEXT
            );

            CREATE TABLE IF NOT EXISTS file_operations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                event_id INTEGER NOT NULL,
                file_name TEXT NOT NULL,
                box_relative_path TEXT NOT NULL,
                original_path TEXT,
                restored_to_path TEXT,
                operation TEXT NOT NULL,
                status TEXT DEFAULT 'success',
                FOREIGN KEY (event_id) REFERENCES events(id)
            );

            CREATE INDEX IF NOT EXISTS idx_events_box_id ON events(box_id);
            CREATE INDEX IF NOT EXISTS idx_events_type ON events(event_type);
            CREATE INDEX IF NOT EXISTS idx_events_timestamp ON events(timestamp);
            CREATE INDEX IF NOT EXISTS idx_file_ops_event ON file_operations(event_id);
        ";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 记录一个日志事件及其文件操作明细
    /// </summary>
    public long InsertEvent(LogEvent logEvent)
    {
        EnsureConnection();

        using var transaction = _connection!.BeginTransaction();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO events (box_id, box_name, event_type, timestamp, summary)
            VALUES (@box_id, @box_name, @event_type, @timestamp, @summary);
            SELECT last_insert_rowid();
        ";
        cmd.Parameters.AddWithValue("@box_id", logEvent.BoxId.ToString("D"));
        cmd.Parameters.AddWithValue("@box_name", logEvent.BoxName);
        cmd.Parameters.AddWithValue("@event_type", logEvent.EventType.ToString());
        cmd.Parameters.AddWithValue("@timestamp", logEvent.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("@summary", logEvent.Summary ?? (object)DBNull.Value);

        var eventId = (long)cmd.ExecuteScalar()!;

        // 插入文件操作明细
        foreach (var fo in logEvent.FileOperations)
        {
            using var foCmd = _connection.CreateCommand();
            foCmd.CommandText = @"
                INSERT INTO file_operations (event_id, file_name, box_relative_path, original_path, restored_to_path, operation, status)
                VALUES (@event_id, @file_name, @box_relative_path, @original_path, @restored_to_path, @operation, @status);
            ";
            foCmd.Parameters.AddWithValue("@event_id", eventId);
            foCmd.Parameters.AddWithValue("@file_name", fo.FileName);
            foCmd.Parameters.AddWithValue("@box_relative_path", fo.BoxRelativePath);
            foCmd.Parameters.AddWithValue("@original_path", fo.OriginalPath ?? (object)DBNull.Value);
            foCmd.Parameters.AddWithValue("@restored_to_path", fo.RestoredToPath ?? (object)DBNull.Value);
            foCmd.Parameters.AddWithValue("@operation", fo.Operation);
            foCmd.Parameters.AddWithValue("@status", fo.Status);

            foCmd.ExecuteNonQuery();
        }

        transaction.Commit();
        return eventId;
    }

    /// <summary>
    /// 分页查询日志事件（按时间倒序）
    /// </summary>
    public List<LogEvent> QueryEvents(int page = 1, int pageSize = 50,
        Guid? boxId = null, LogEventType? eventType = null)
    {
        EnsureConnection();

        var events = new List<LogEvent>();

        var sql = "SELECT id, box_id, box_name, event_type, timestamp, summary FROM events WHERE 1=1";
        if (boxId.HasValue)
            sql += " AND box_id = @box_id";
        if (eventType.HasValue)
            sql += " AND event_type = @event_type";
        sql += " ORDER BY timestamp DESC LIMIT @limit OFFSET @offset";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        if (boxId.HasValue)
            cmd.Parameters.AddWithValue("@box_id", boxId.Value.ToString("D"));
        if (eventType.HasValue)
            cmd.Parameters.AddWithValue("@event_type", eventType.Value.ToString());
        cmd.Parameters.AddWithValue("@limit", pageSize);
        cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            events.Add(new LogEvent
            {
                Id = reader.GetInt64(0),
                BoxId = Guid.Parse(reader.GetString(1)),
                BoxName = reader.GetString(2),
                EventType = Enum.Parse<LogEventType>(reader.GetString(3)),
                Timestamp = DateTimeOffset.Parse(reader.GetString(4)),
                Summary = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return events;
    }

    /// <summary>
    /// 查询指定事件的详细文件操作记录
    /// </summary>
    public List<FileOperationRecord> QueryFileOperations(long eventId)
    {
        EnsureConnection();

        var records = new List<FileOperationRecord>();

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            SELECT id, event_id, file_name, box_relative_path,
                   original_path, restored_to_path, operation, status
            FROM file_operations WHERE event_id = @event_id
            ORDER BY id
        ";
        cmd.Parameters.AddWithValue("@event_id", eventId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new FileOperationRecord
            {
                Id = reader.GetInt64(0),
                EventId = reader.GetInt64(1),
                FileName = reader.GetString(2),
                BoxRelativePath = reader.GetString(3),
                OriginalPath = reader.IsDBNull(4) ? null : reader.GetString(4),
                RestoredToPath = reader.IsDBNull(5) ? null : reader.GetString(5),
                Operation = reader.GetString(6),
                Status = reader.GetString(7)
            });
        }

        return records;
    }

    /// <summary>
    /// 获取日志总数
    /// </summary>
    public int GetEventCount(Guid? boxId = null, LogEventType? eventType = null)
    {
        EnsureConnection();

        var sql = "SELECT COUNT(*) FROM events WHERE 1=1";
        if (boxId.HasValue)
            sql += " AND box_id = @box_id";
        if (eventType.HasValue)
            sql += " AND event_type = @event_type";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        if (boxId.HasValue)
            cmd.Parameters.AddWithValue("@box_id", boxId.Value.ToString("D"));
        if (eventType.HasValue)
            cmd.Parameters.AddWithValue("@event_type", eventType.Value.ToString());

        return Convert.ToInt32(cmd.ExecuteScalar()!);
    }

    private void EnsureConnection()
    {
        if (_connection == null)
            throw new InvalidOperationException("数据库未初始化。请先调用 Initialize()。");
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }
}
