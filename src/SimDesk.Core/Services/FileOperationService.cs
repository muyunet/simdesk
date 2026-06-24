using SimDesk.Core.Models;

namespace SimDesk.Core.Services;

/// <summary>
/// 文件操作服务接口
/// </summary>
public interface IFileOperationService
{
    /// <summary>将文件/目录拖入收纳盒</summary>
    List<string> AddFiles(StorageBox box, IEnumerable<string> sourcePaths, DragOperation operation);

    /// <summary>从收纳盒中删除文件</summary>
    void RemoveFiles(StorageBox box, IEnumerable<string> boxRelativePaths);

    /// <summary>解散收纳盒：将所有文件还原到原始位置</summary>
    DissolveResult Dissolve(StorageBox box);

    /// <summary>物理删除收纳盒目录中的所有内容（常规盒删除时调用）</summary>
    void DeleteAllContents(StorageBox box);
}

/// <summary>
/// 解散结果
/// </summary>
public class DissolveResult
{
    public List<DissolveFileResult> Files { get; set; } = new();
}

/// <summary>
/// 单个文件的解散结果
/// </summary>
public class DissolveFileResult
{
    public string BoxRelativePath { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public string RestoredToPath { get; set; } = string.Empty;
    public string Status { get; set; } = "success"; // success, skipped, conflict_renamed
    public string? Error { get; set; }
}

/// <summary>
/// 文件操作服务
/// </summary>
public class FileOperationService : IFileOperationService
{
    private readonly ISymlinkService _symlinkService;
    private readonly IFileTrackingService _trackingService;

    public FileOperationService(ISymlinkService symlinkService, IFileTrackingService trackingService)
    {
        _symlinkService = symlinkService;
        _trackingService = trackingService;
    }

    /// <summary>
    /// 将文件/目录拖入收纳盒
    /// </summary>
    public List<string> AddFiles(StorageBox box, IEnumerable<string> sourcePaths, DragOperation operation)
    {
        var addedRelativePaths = new List<string>();
        var boxDir = box.Directory;

        Directory.CreateDirectory(boxDir);

        foreach (var sourcePath in sourcePaths)
        {
            var sourceFullPath = Path.GetFullPath(sourcePath);

            if (Directory.Exists(sourceFullPath))
            {
                var dirName = Path.GetFileName(sourceFullPath);
                var destDir = Path.Combine(boxDir, dirName);

                if (box.Type == BoxType.Link)
                {
                    // 链接盒：创建目录符号链接
                    _symlinkService.CreateDirectorySymlink(destDir, sourceFullPath);
                    addedRelativePaths.Add(dirName);
                }
                else
                {
                    // 常规盒：复制整个目录
                    CopyDirectoryRecursive(sourceFullPath, destDir);
                    addedRelativePaths.Add(dirName);
                }

                // 递归追踪目录内所有文件
                if (Directory.Exists(sourceFullPath))
                {
                    var allFiles = Directory.GetFiles(sourceFullPath, "*", SearchOption.AllDirectories);
                    foreach (var file in allFiles)
                    {
                        var relativeToSource = Path.GetRelativePath(sourceFullPath, file);
                        var boxRelative = Path.Combine(dirName, relativeToSource).Replace('\\', '/');
                        _trackingService.TrackFile(box.FileOrigins, boxRelative, file);
                    }
                }
            }
            else if (File.Exists(sourceFullPath))
            {
                var fileName = Path.GetFileName(sourceFullPath);
                var destPath = Path.Combine(boxDir, fileName);

                if (box.Type == BoxType.Link)
                {
                    // 链接盒：创建文件符号链接
                    _symlinkService.CreateFileSymlink(destPath, sourceFullPath);
                }
                else
                {
                    // 常规盒：复制或移动
                    if (operation == DragOperation.Copy)
                    {
                        File.Copy(sourceFullPath, destPath, overwrite: false);
                    }
                    else
                    {
                        File.Move(sourceFullPath, destPath, overwrite: false);
                    }
                }

                addedRelativePaths.Add(fileName);
                _trackingService.TrackFile(box.FileOrigins, fileName, sourceFullPath);
            }
        }

        return addedRelativePaths;
    }

    /// <summary>
    /// 从收纳盒中删除文件
    /// </summary>
    public void RemoveFiles(StorageBox box, IEnumerable<string> boxRelativePaths)
    {
        var boxDir = box.Directory;

        foreach (var relativePath in boxRelativePaths)
        {
            var fullPath = Path.Combine(boxDir, relativePath);

            if (box.Type == BoxType.Regular)
            {
                // 常规盒：真删除
                if (Directory.Exists(fullPath))
                    Directory.Delete(fullPath, recursive: true);
                else if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            else
            {
                // 链接盒：只删符号链接
                if (_symlinkService.IsSymlink(fullPath) || File.Exists(fullPath))
                    File.Delete(fullPath);
                else if (Directory.Exists(fullPath))
                    Directory.Delete(fullPath, recursive: true);
            }

            // 清理追踪记录
            var keysToRemove = box.FileOrigins.Keys
                .Where(k => k.StartsWith(relativePath, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var key in keysToRemove)
                box.FileOrigins.Remove(key);
        }
    }

    /// <summary>
    /// 解散收纳盒：将所有文件还原到原始位置
    /// </summary>
    public DissolveResult Dissolve(StorageBox box)
    {
        var result = new DissolveResult();

        foreach (var (boxRelativePath, originalPath) in box.FileOrigins)
        {
            var boxFilePath = Path.Combine(box.Directory, boxRelativePath);
            var record = new DissolveFileResult
            {
                BoxRelativePath = boxRelativePath,
                OriginalPath = originalPath
            };

            if (!File.Exists(boxFilePath) && !Directory.Exists(boxFilePath))
            {
                record.Status = "skipped";
                record.Error = "文件已在盒中不存在";
                result.Files.Add(record);
                continue;
            }

            try
            {
                var restorePath = _trackingService.ResolveRestorePath(originalPath, boxFilePath);
                bool isConflict = restorePath != originalPath;
                record.RestoredToPath = restorePath;

                // 确保目标目录存在
                var destDir = Path.GetDirectoryName(restorePath)!;
                Directory.CreateDirectory(destDir);

                if (Directory.Exists(boxFilePath))
                {
                    CopyDirectoryRecursive(boxFilePath, restorePath);
                    Directory.Delete(boxFilePath, recursive: true);
                }
                else
                {
                    File.Move(boxFilePath, restorePath, overwrite: false);
                }

                record.Status = isConflict ? "conflict_renamed" : "success";
            }
            catch (Exception ex)
            {
                record.Status = "skipped";
                record.Error = ex.Message;
            }

            result.Files.Add(record);
        }

        // 清理空目录
        if (Directory.Exists(box.Directory))
        {
            try { Directory.Delete(box.Directory, recursive: true); }
            catch { /* 忽略清理失败 */ }
        }

        return result;
    }

    /// <summary>
    /// 物理删除收纳盒目录中的所有内容
    /// </summary>
    public void DeleteAllContents(StorageBox box)
    {
        if (Directory.Exists(box.Directory))
        {
            Directory.Delete(box.Directory, recursive: true);
        }
    }

    // ========== 私有辅助 ==========

    private static void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destDir, fileName), overwrite: false);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            CopyDirectoryRecursive(dir, Path.Combine(destDir, dirName));
        }
    }
}
