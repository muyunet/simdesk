using SimDesk.Core.Data;
using SimDesk.Core.Models;

namespace SimDesk.Core.Services;

/// <summary>
/// 盒状态持久化服务接口
/// </summary>
public interface IBoxPersistenceService
{
    /// <summary>加载全局配置</summary>
    GlobalConfig LoadGlobalConfig();

    /// <summary>保存全局配置</summary>
    void SaveGlobalConfig(GlobalConfig config);

    /// <summary>加载单个收纳盒</summary>
    StorageBox? LoadBox(Guid boxId);

    /// <summary>保存单个收纳盒</summary>
    void SaveBox(StorageBox box);

    /// <summary>删除收纳盒的 .boxmeta</summary>
    void DeleteBox(Guid boxId);

    /// <summary>获取盒在磁盘上的存储目录路径</summary>
    string GetBoxDirectory(Guid boxId);
}

/// <summary>
/// 盒状态持久化服务
/// </summary>
public class BoxPersistenceService : IBoxPersistenceService
{
    private readonly JsonDataStore _store;
    private readonly string _boxesDir;

    public BoxPersistenceService(string appDataDir)
    {
        _store = new JsonDataStore(appDataDir);
        _boxesDir = Path.Combine(appDataDir, "boxes");
        _store.EnsureDirectories();
    }

    public GlobalConfig LoadGlobalConfig() => _store.LoadGlobalConfig();

    public void SaveGlobalConfig(GlobalConfig config) => _store.SaveGlobalConfig(config);

    public StorageBox? LoadBox(Guid boxId) => _store.LoadBoxMeta(boxId);

    public void SaveBox(StorageBox box) => _store.SaveBoxMeta(box);

    public void DeleteBox(Guid boxId) => _store.DeleteBoxMeta(boxId);

    /// <summary>
    /// 获取盒在磁盘上的存储目录路径
    /// </summary>
    public string GetBoxDirectory(Guid boxId)
    {
        return Path.Combine(_boxesDir, boxId.ToString("D"));
    }
}
