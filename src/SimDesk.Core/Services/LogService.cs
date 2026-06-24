using SimDesk.Core.Data;
using SimDesk.Core.Models;

namespace SimDesk.Core.Services;

/// <summary>
/// 日志服务接口
/// </summary>
public interface ILogService
{
    /// <summary>记录收纳盒创建</summary>
    long LogCreate(StorageBox box);

    /// <summary>记录文件纳入收纳盒</summary>
    long LogAdd(StorageBox box, IEnumerable<string> addedPaths, IEnumerable<(string boxRel, string orig)> trackInfo);

    /// <summary>记录从收纳盒删除文件</summary>
    long LogRemove(StorageBox box, IEnumerable<string> removedPaths);

    /// <summary>记录解散收纳盒（含详细文件还原信息）</summary>
    long LogDissolve(StorageBox box, DissolveResult dissolveResult);

    /// <summary>记录删除收纳盒</summary>
    long LogDelete(StorageBox box);

    /// <summary>分页查询日志</summary>
    List<LogEvent> QueryEvents(int page = 1, int pageSize = 50,
        Guid? boxId = null, LogEventType? eventType = null);

    /// <summary>获取日志总数</summary>
    int GetEventCount(Guid? boxId = null, LogEventType? eventType = null);

    /// <summary>查询指定事件的文件操作明细</summary>
    List<FileOperationRecord> QueryFileOperations(long eventId);
}

/// <summary>
/// 日志服务实现
/// </summary>
public class LogService : ILogService
{
    private readonly SqliteLogStore _store;

    public LogService(SqliteLogStore store)
    {
        _store = store;
    }

    public long LogCreate(StorageBox box)
    {
        return _store.InsertEvent(new LogEvent
        {
            BoxId = box.Id,
            BoxName = box.Name,
            EventType = LogEventType.Create,
            Summary = $"创建{(box.Type == BoxType.Regular ? "常规" : "链接")}收纳盒"
        });
    }

    public long LogAdd(StorageBox box, IEnumerable<string> addedPaths,
        IEnumerable<(string boxRel, string orig)> trackInfo)
    {
        var pathList = addedPaths.ToList();
        var trackList = trackInfo.ToList();

        var logEvent = new LogEvent
        {
            BoxId = box.Id,
            BoxName = box.Name,
            EventType = LogEventType.Add,
            Summary = $"纳入 {pathList.Count} 个文件/目录",
            FileOperations = trackList.Select(t => new FileOperationRecord
            {
                FileName = Path.GetFileName(t.boxRel),
                BoxRelativePath = t.boxRel,
                OriginalPath = t.orig,
                Operation = "added",
                Status = "success"
            }).ToList()
        };

        return _store.InsertEvent(logEvent);
    }

    public long LogRemove(StorageBox box, IEnumerable<string> removedPaths)
    {
        var pathList = removedPaths.ToList();

        var logEvent = new LogEvent
        {
            BoxId = box.Id,
            BoxName = box.Name,
            EventType = LogEventType.Remove,
            Summary = $"删除 {pathList.Count} 个文件/目录",
            FileOperations = pathList.Select(p => new FileOperationRecord
            {
                FileName = Path.GetFileName(p),
                BoxRelativePath = p,
                Operation = "removed",
                Status = "success"
            }).ToList()
        };

        return _store.InsertEvent(logEvent);
    }

    public long LogDissolve(StorageBox box, DissolveResult dissolveResult)
    {
        var successCount = dissolveResult.Files.Count(f => f.Status == "success");
        var conflictCount = dissolveResult.Files.Count(f => f.Status == "conflict_renamed");
        var skippedCount = dissolveResult.Files.Count(f => f.Status == "skipped");

        var summaryParts = new List<string>();
        if (successCount > 0) summaryParts.Add($"{successCount} 个文件成功还原");
        if (conflictCount > 0) summaryParts.Add($"{conflictCount} 个文件因冲突重命名");
        if (skippedCount > 0) summaryParts.Add($"{skippedCount} 个文件跳过");

        var logEvent = new LogEvent
        {
            BoxId = box.Id,
            BoxName = box.Name,
            EventType = LogEventType.Dissolve,
            Summary = $"解散收纳盒: {string.Join("，", summaryParts)}",
            FileOperations = dissolveResult.Files.Select(f => new FileOperationRecord
            {
                FileName = Path.GetFileName(f.BoxRelativePath),
                BoxRelativePath = f.BoxRelativePath,
                OriginalPath = f.OriginalPath,
                RestoredToPath = f.RestoredToPath,
                Operation = "restored",
                Status = f.Status
            }).ToList()
        };

        return _store.InsertEvent(logEvent);
    }

    public long LogDelete(StorageBox box)
    {
        return _store.InsertEvent(new LogEvent
        {
            BoxId = box.Id,
            BoxName = box.Name,
            EventType = LogEventType.Delete,
            Summary = $"物理删除收纳盒及其所有文件"
        });
    }

    public List<LogEvent> QueryEvents(int page = 1, int pageSize = 50,
        Guid? boxId = null, LogEventType? eventType = null)
    {
        return _store.QueryEvents(page, pageSize, boxId, eventType);
    }

    public int GetEventCount(Guid? boxId = null, LogEventType? eventType = null)
    {
        return _store.GetEventCount(boxId, eventType);
    }

    public List<FileOperationRecord> QueryFileOperations(long eventId)
    {
        return _store.QueryFileOperations(eventId);
    }
}
