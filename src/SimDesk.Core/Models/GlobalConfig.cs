namespace SimDesk.Core.Models;

/// <summary>
/// 全局配置（存储在 index.json 中）
/// </summary>
public class GlobalConfig
{
    public int Version { get; set; } = 2;
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public DragOperation DragOperation { get; set; } = DragOperation.Move;
    public BoxType DefaultBoxType { get; set; } = BoxType.Regular;
    public bool AutoStart { get; set; } = false;
    public List<BoxIndexEntry> Boxes { get; set; } = new();
}

/// <summary>
/// index.json 中的收纳盒索引条目
/// </summary>
public class BoxIndexEntry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BoxType Type { get; set; }
    public string Directory { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
