namespace SimDesk.Core.Models;

/// <summary>
/// 文件来源映射：记录盒内相对路径到原始绝对路径的对应关系
/// </summary>
public class FileOrigin
{
    /// <summary>文件在收纳盒内的相对路径</summary>
    public string BoxRelativePath { get; set; } = string.Empty;

    /// <summary>文件的原始绝对路径</summary>
    public string OriginalFullPath { get; set; } = string.Empty;

    /// <summary>纳入收纳盒的时间</summary>
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
