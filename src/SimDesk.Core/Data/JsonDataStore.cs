using System.Text.Json;
using SimDesk.Core.Models;

namespace SimDesk.Core.Data;

/// <summary>
/// JSON 数据持久化：管理 index.json 和各盒 .boxmeta 文件的读写
/// </summary>
public class JsonDataStore
{
    private readonly string _appDataDir;
    private readonly string _boxesDir;
    private readonly string _indexFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public JsonDataStore(string appDataDir)
    {
        _appDataDir = appDataDir;
        _boxesDir = Path.Combine(appDataDir, "boxes");
        _indexFilePath = Path.Combine(appDataDir, "index.json");
    }

    /// <summary>
    /// 确保数据目录存在
    /// </summary>
    public void EnsureDirectories()
    {
        Directory.CreateDirectory(_appDataDir);
        Directory.CreateDirectory(_boxesDir);
    }

    // ========== Global Config (index.json) ==========

    public GlobalConfig LoadGlobalConfig()
    {
        if (!File.Exists(_indexFilePath))
            return new GlobalConfig();

        var json = File.ReadAllText(_indexFilePath);
        return JsonSerializer.Deserialize<GlobalConfig>(json, JsonOptions) ?? new GlobalConfig();
    }

    public void SaveGlobalConfig(GlobalConfig config)
    {
        config.LastUpdated = DateTimeOffset.UtcNow;
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_indexFilePath, json);
    }

    // ========== Box Meta (.boxmeta) ==========

    public string GetBoxMetaPath(Guid boxId)
    {
        return Path.Combine(_boxesDir, boxId.ToString("D"), ".boxmeta");
    }

    public StorageBox? LoadBoxMeta(Guid boxId)
    {
        var metaPath = GetBoxMetaPath(boxId);
        if (!File.Exists(metaPath))
            return null;

        var json = File.ReadAllText(metaPath);
        return JsonSerializer.Deserialize<StorageBox>(json, JsonOptions);
    }

    public void SaveBoxMeta(StorageBox box)
    {
        var boxDir = Path.Combine(_boxesDir, box.Id.ToString("D"));
        Directory.CreateDirectory(boxDir);

        var metaPath = Path.Combine(boxDir, ".boxmeta");
        var json = JsonSerializer.Serialize(box, JsonOptions);
        File.WriteAllText(metaPath, json);
    }

    /// <summary>
    /// 删除盒的 .boxmeta（物理删除盒目录时调用）
    /// </summary>
    public void DeleteBoxMeta(Guid boxId)
    {
        var boxDir = Path.Combine(_boxesDir, boxId.ToString("D"));
        if (Directory.Exists(boxDir))
        {
            var metaPath = Path.Combine(boxDir, ".boxmeta");
            if (File.Exists(metaPath))
                File.Delete(metaPath);
        }
    }
}
