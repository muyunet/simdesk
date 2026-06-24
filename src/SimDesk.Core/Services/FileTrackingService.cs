namespace SimDesk.Core.Services;

/// <summary>
/// 文件来源追踪服务接口
/// </summary>
public interface IFileTrackingService
{
    /// <summary>记录文件来源映射</summary>
    void TrackFile(Dictionary<string, string> fileOrigins, string boxRelativePath, string originalFullPath);

    /// <summary>批量记录文件来源（自动递归处理目录）</summary>
    void TrackFiles(Dictionary<string, string> fileOrigins, string boxRootDir, IEnumerable<string> sourcePaths);

    /// <summary>解析还原时路径冲突，返回目标路径</summary>
    string ResolveRestorePath(string originalPath, string boxFilePath);
}

/// <summary>
/// 文件来源追踪服务
/// </summary>
public class FileTrackingService : IFileTrackingService
{
    /// <summary>
    /// 记录单个文件来源
    /// </summary>
    public void TrackFile(Dictionary<string, string> fileOrigins, string boxRelativePath, string originalFullPath)
    {
        fileOrigins[boxRelativePath] = originalFullPath;
    }

    /// <summary>
    /// 批量记录文件来源。对目录会递归遍历其下所有文件
    /// </summary>
    public void TrackFiles(Dictionary<string, string> fileOrigins, string boxRootDir, IEnumerable<string> sourcePaths)
    {
        foreach (var sourcePath in sourcePaths)
        {
            var sourceFullPath = Path.GetFullPath(sourcePath);

            if (Directory.Exists(sourceFullPath))
            {
                // 递归遍历目录
                var sourceDirName = Path.GetFileName(sourceFullPath);
                TrackDirectory(fileOrigins, boxRootDir, sourceFullPath, sourceDirName);
            }
            else if (File.Exists(sourceFullPath))
            {
                var fileName = Path.GetFileName(sourceFullPath);
                var boxRelative = fileName; // 单文件直接放在盒根
                TrackFile(fileOrigins, boxRelative, sourceFullPath);
            }
        }
    }

    private void TrackDirectory(Dictionary<string, string> fileOrigins, string boxRootDir, string sourceDir, string boxSubDir)
    {
        var allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

        foreach (var file in allFiles)
        {
            // 计算盒内相对路径
            var relativeToSource = Path.GetRelativePath(sourceDir, file);
            var boxRelative = Path.Combine(boxSubDir, relativeToSource).Replace('\\', '/');

            TrackFile(fileOrigins, boxRelative, file);
        }
    }

    /// <summary>
    /// 还原文件时处理路径冲突
    /// </summary>
    public string ResolveRestorePath(string originalPath, string boxFilePath)
    {
        // 如果原始路径不存在该文件，直接还原
        if (!File.Exists(originalPath) && !Directory.Exists(originalPath))
            return originalPath;

        // 否则生成带后缀的新路径
        var dir = Path.GetDirectoryName(originalPath) ?? "";
        var nameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
        var ext = Path.GetExtension(originalPath);
        var counter = 1;
        string newPath;
        do
        {
            newPath = Path.Combine(dir, $"{nameWithoutExt}.还原_冲突_{counter}{ext}");
            counter++;
        } while (File.Exists(newPath) || Directory.Exists(newPath));

        return newPath;
    }
}
