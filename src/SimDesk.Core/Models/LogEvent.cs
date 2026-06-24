namespace SimDesk.Core.Models;

/// <summary>
/// 日志事件类型
/// </summary>
public enum LogEventType
{
    Create,
    Add,
    Remove,
    Dissolve,
    Delete
}

/// <summary>
/// 日志事件
/// </summary>
public class LogEvent
{
    public long Id { get; set; }
    public Guid BoxId { get; set; }
    public string BoxName { get; set; } = string.Empty;
    public LogEventType EventType { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Summary { get; set; }
    public List<FileOperationRecord> FileOperations { get; set; } = new();
}

/// <summary>
/// 单文件操作记录
/// </summary>
public class FileOperationRecord
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BoxRelativePath { get; set; } = string.Empty;
    public string? OriginalPath { get; set; }
    public string? RestoredToPath { get; set; }
    public string Operation { get; set; } = string.Empty; // added, removed, restored, conflict
    public string Status { get; set; } = "success"; // success, skipped, conflict_renamed
}
