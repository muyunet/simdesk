using System.Runtime.InteropServices;

namespace SimDesk.Core.Services;

/// <summary>
/// 符号链接服务接口
/// </summary>
public interface ISymlinkService
{
    /// <summary>创建文件符号链接</summary>
    bool CreateFileSymlink(string linkPath, string targetPath);

    /// <summary>创建目录符号链接</summary>
    bool CreateDirectorySymlink(string linkPath, string targetPath);

    /// <summary>判断路径是否为符号链接/reparse point</summary>
    bool IsSymlink(string path);
}

/// <summary>
/// Windows 符号链接服务（P/Invoke CreateSymbolicLinkW）
/// </summary>
public class SymlinkService : ISymlinkService
{
    // dwFlags: 0 = 文件, 1 = 目录
    // SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x02 (Win10 1703+)
    private const int SYMBOLIC_LINK_FLAG_FILE = 0x00;
    private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x01;
    private const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x02;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateSymbolicLink(
        string lpSymlinkFileName,
        string lpTargetFileName,
        int dwFlags);

    /// <summary>
    /// 创建文件符号链接
    /// </summary>
    public bool CreateFileSymlink(string linkPath, string targetPath)
    {
        // 优先尝试非特权创建（Win10 1703+ 开发者模式）
        int flags = SYMBOLIC_LINK_FLAG_FILE | SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;
        if (CreateSymbolicLink(linkPath, targetPath, flags))
            return true;

        // 回退：不需要 AllowUnprivileged 标志
        flags = SYMBOLIC_LINK_FLAG_FILE;
        return CreateSymbolicLink(linkPath, targetPath, flags);
    }

    /// <summary>
    /// 创建目录符号链接
    /// </summary>
    public bool CreateDirectorySymlink(string linkPath, string targetPath)
    {
        int flags = SYMBOLIC_LINK_FLAG_DIRECTORY | SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;
        if (CreateSymbolicLink(linkPath, targetPath, flags))
            return true;

        flags = SYMBOLIC_LINK_FLAG_DIRECTORY;
        return CreateSymbolicLink(linkPath, targetPath, flags);
    }

    /// <summary>
    /// 判断路径是否为符号链接/reparse point
    /// </summary>
    public bool IsSymlink(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists && !Directory.Exists(path))
                return false;

            return fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return false;
        }
    }
}
