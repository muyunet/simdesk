using SimDesk.Core.Models;

namespace SimDesk.Core.Services;

/// <summary>
/// 收纳盒管理服务接口
/// </summary>
public interface IBoxManager
{
    /// <summary>创建收纳盒</summary>
    StorageBox CreateBox(string name, BoxType type, string? fromFolderPath = null, IEnumerable<string>? initialFiles = null);

    /// <summary>解散收纳盒（文件还原 + 软删除）</summary>
    DissolveResult DissolveBox(Guid boxId);

    /// <summary>物理删除收纳盒（彻底删除所有文件）</summary>
    void DeleteBox(Guid boxId);

    /// <summary>重命名收纳盒</summary>
    void RenameBox(Guid boxId, string newName);

    /// <summary>更新收纳盒设置</summary>
    void UpdateSettings(Guid boxId, BoxSettings settings);

    /// <summary>更新窗口位置</summary>
    void UpdateWindowPosition(Guid boxId, WindowPosition position);

    /// <summary>向收纳盒添加文件（拖入）</summary>
    List<string> AddFiles(Guid boxId, IEnumerable<string> sourcePaths);

    /// <summary>从收纳盒删除文件</summary>
    void RemoveFiles(Guid boxId, IEnumerable<string> boxRelativePaths);

    /// <summary>获取收纳盒</summary>
    StorageBox? GetBox(Guid boxId);

    /// <summary>获取所有活跃的收纳盒</summary>
    List<StorageBox> GetAllActiveBoxes();

    /// <summary>获取所有收纳盒（含已解散的）</summary>
    List<StorageBox> GetAllBoxes();

    /// <summary>获取全局配置</summary>
    GlobalConfig GetGlobalConfig();

    /// <summary>更新全局配置</summary>
    void UpdateGlobalConfig(GlobalConfig config);

    /// <summary>设置拖入操作的默认行为</summary>
    void SetDefaultDragOperation(DragOperation operation);

    /// <summary>设置默认收纳盒类型</summary>
    void SetDefaultBoxType(BoxType type);
}

/// <summary>
/// 收纳盒管理服务 — 核心协调器
/// </summary>
public class BoxManager : IBoxManager
{
    private readonly IBoxPersistenceService _persistence;
    private readonly IFileOperationService _fileOps;
    private readonly ILogService _log;
    private readonly string _appDataDir;

    public BoxManager(IBoxPersistenceService persistence, IFileOperationService fileOps, ILogService log, string appDataDir)
    {
        _persistence = persistence;
        _fileOps = fileOps;
        _log = log;
        _appDataDir = appDataDir;
    }

    /// <summary>
    /// 创建收纳盒
    /// </summary>
    public StorageBox CreateBox(string name, BoxType type, string? fromFolderPath = null, IEnumerable<string>? initialFiles = null)
    {
        var box = new StorageBox
        {
            Name = name,
            Type = type,
            Directory = _persistence.GetBoxDirectory(Guid.NewGuid()) // 先用临时ID生成路径
        };

        // 用实际 ID 重新设置目录
        box.Directory = _persistence.GetBoxDirectory(box.Id);
        Directory.CreateDirectory(box.Directory);

        // 如果指定了文件夹路径，将该文件夹内容纳入收纳盒
        if (fromFolderPath != null && Directory.Exists(fromFolderPath))
        {
            box.Name = Path.GetFileName(fromFolderPath);
            _fileOps.AddFiles(box, new[] { fromFolderPath }, DragOperation.Copy);
        }
        else if (initialFiles != null)
        {
            _fileOps.AddFiles(box, initialFiles, DragOperation.Copy);
        }

        _persistence.SaveBox(box);

        // 更新全局索引
        var config = _persistence.LoadGlobalConfig();
        config.Boxes.Add(new BoxIndexEntry
        {
            Id = box.Id,
            Name = box.Name,
            Type = box.Type,
            Directory = box.Directory,
            CreatedAt = box.CreatedAt,
            IsDeleted = false
        });
        _persistence.SaveGlobalConfig(config);

        _log.LogCreate(box);

        return box;
    }

    /// <summary>
    /// 解散收纳盒 — 文件还原到原位，盒标记为已删除
    /// </summary>
    public DissolveResult DissolveBox(Guid boxId)
    {
        var box = GetBox(boxId)
            ?? throw new InvalidOperationException($"收纳盒 {boxId} 不存在");

        var result = _fileOps.Dissolve(box);

        // 软删除
        box.IsDeleted = true;
        box.FileOrigins.Clear();
        _persistence.SaveBox(box);

        // 更新全局索引
        var config = _persistence.LoadGlobalConfig();
        var entry = config.Boxes.FirstOrDefault(b => b.Id == boxId);
        if (entry != null)
            entry.IsDeleted = true;
        _persistence.SaveGlobalConfig(config);

        _log.LogDissolve(box, result);

        return result;
    }

    /// <summary>
    /// 物理删除收纳盒（彻底删除所有文件）
    /// </summary>
    public void DeleteBox(Guid boxId)
    {
        var box = GetBox(boxId)
            ?? throw new InvalidOperationException($"收纳盒 {boxId} 不存在");

        _fileOps.DeleteAllContents(box);
        _persistence.DeleteBox(boxId);

        // 从全局索引中移除
        var config = _persistence.LoadGlobalConfig();
        config.Boxes.RemoveAll(b => b.Id == boxId);
        _persistence.SaveGlobalConfig(config);

        _log.LogDelete(box);
    }

    public void RenameBox(Guid boxId, string newName)
    {
        var box = GetBox(boxId)
            ?? throw new InvalidOperationException($"收纳盒 {boxId} 不存在");

        box.Name = newName;
        _persistence.SaveBox(box);

        // 更新索引
        var config = _persistence.LoadGlobalConfig();
        var entry = config.Boxes.FirstOrDefault(b => b.Id == boxId);
        if (entry != null)
            entry.Name = newName;
        _persistence.SaveGlobalConfig(config);
    }

    public void UpdateSettings(Guid boxId, BoxSettings settings)
    {
        var box = GetBox(boxId)
            ?? throw new InvalidOperationException($"收纳盒 {boxId} 不存在");

        box.Settings = settings;
        _persistence.SaveBox(box);
    }

    public void UpdateWindowPosition(Guid boxId, WindowPosition position)
    {
        var box = GetBox(boxId)
            ?? throw new InvalidOperationException($"收纳盒 {boxId} 不存在");

        box.WindowPosition = position;
        _persistence.SaveBox(box);
    }

    public List<string> AddFiles(Guid boxId, IEnumerable<string> sourcePaths)
    {
        var box = GetBox(boxId)
            ?? throw new InvalidOperationException($"收纳盒 {boxId} 不存在");

        var config = _persistence.LoadGlobalConfig();
        var result = _fileOps.AddFiles(box, sourcePaths, config.DragOperation);

        _persistence.SaveBox(box);

        // 记录日志
        var trackInfo = result.Select(r => (r, box.FileOrigins.GetValueOrDefault(r, "未知")));
        _log.LogAdd(box, result, trackInfo);

        return result;
    }

    public void RemoveFiles(Guid boxId, IEnumerable<string> boxRelativePaths)
    {
        var box = GetBox(boxId)
            ?? throw new InvalidOperationException($"收纳盒 {boxId} 不存在");

        _fileOps.RemoveFiles(box, boxRelativePaths);
        _persistence.SaveBox(box);

        _log.LogRemove(box, boxRelativePaths);
    }

    public StorageBox? GetBox(Guid boxId)
    {
        return _persistence.LoadBox(boxId);
    }

    public List<StorageBox> GetAllActiveBoxes()
    {
        var config = _persistence.LoadGlobalConfig();
        var boxes = new List<StorageBox>();

        foreach (var entry in config.Boxes.Where(b => !b.IsDeleted))
        {
            var box = _persistence.LoadBox(entry.Id);
            if (box != null)
                boxes.Add(box);
        }

        return boxes;
    }

    public List<StorageBox> GetAllBoxes()
    {
        var config = _persistence.LoadGlobalConfig();
        var boxes = new List<StorageBox>();

        foreach (var entry in config.Boxes)
        {
            var box = _persistence.LoadBox(entry.Id);
            if (box != null)
                boxes.Add(box);
        }

        return boxes;
    }

    public GlobalConfig GetGlobalConfig()
    {
        return _persistence.LoadGlobalConfig();
    }

    public void UpdateGlobalConfig(GlobalConfig config)
    {
        _persistence.SaveGlobalConfig(config);
    }

    public void SetDefaultDragOperation(DragOperation operation)
    {
        var config = _persistence.LoadGlobalConfig();
        config.DragOperation = operation;
        _persistence.SaveGlobalConfig(config);
    }

    public void SetDefaultBoxType(BoxType type)
    {
        var config = _persistence.LoadGlobalConfig();
        config.DefaultBoxType = type;
        _persistence.SaveGlobalConfig(config);
    }
}
