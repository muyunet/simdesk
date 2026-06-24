namespace SimDesk.Core.Models;

/// <summary>
/// 收纳盒实体
/// </summary>
public class StorageBox
{
    /// <summary>唯一标识</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>收纳盒名称</summary>
    public string Name { get; set; } = "新建收纳盒";

    /// <summary>收纳盒类型</summary>
    public BoxType Type { get; set; } = BoxType.Regular;

    /// <summary>盒内文件在磁盘上的存储目录路径</summary>
    public string Directory { get; set; } = string.Empty;

    /// <summary>窗口位置</summary>
    public WindowPosition WindowPosition { get; set; } = new();

    /// <summary>盒专属设置</summary>
    public BoxSettings Settings { get; set; } = new();

    /// <summary>文件来源映射 (盒内相对路径 → 原始路径)</summary>
    public Dictionary<string, string> FileOrigins { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>创建时间</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>软删除标记（解散后标记，30天内可恢复）</summary>
    public bool IsDeleted { get; set; } = false;
}
