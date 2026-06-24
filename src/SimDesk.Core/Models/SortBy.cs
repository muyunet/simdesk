namespace SimDesk.Core.Models;

/// <summary>
/// 排序方式
/// </summary>
public enum SortBy
{
    /// <summary>按名称排序</summary>
    Name = 0,

    /// <summary>按文件大小排序</summary>
    Size = 1,

    /// <summary>按修改时间排序</summary>
    ModifiedTime = 2,

    /// <summary>按文件类型排序</summary>
    Type = 3
}
