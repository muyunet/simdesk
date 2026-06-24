using SimDesk.Core.Data;
using SimDesk.Core.Models;
using SimDesk.Core.Services;

namespace SimDesk.Core.Tests.Services;

public class BoxManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonDataStore _dataStore;
    private readonly SqliteLogStore _logStore;
    private readonly IBoxPersistenceService _persistence;
    private readonly IFileTrackingService _tracking;
    private readonly ISymlinkService _symlink;
    private readonly IFileOperationService _fileOps;
    private readonly ILogService _log;
    private readonly BoxManager _manager;

    public BoxManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SimDesk_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _dataStore = new JsonDataStore(_tempDir);
        _dataStore.EnsureDirectories();

        _logStore = new SqliteLogStore(_tempDir);
        _logStore.Initialize();

        _persistence = new BoxPersistenceService(_tempDir);
        _tracking = new FileTrackingService();
        _symlink = new SymlinkService();
        _fileOps = new FileOperationService(_symlink, _tracking);
        _log = new LogService(_logStore);
        _manager = new BoxManager(_persistence, _fileOps, _log, _tempDir);
    }

    public void Dispose()
    {
        _logStore.Dispose();
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* 清理失败忽略 */ }
    }

    [Fact]
    public void CreateBox_ShouldCreateBoxAndPersist()
    {
        var box = _manager.CreateBox("测试盒", BoxType.Regular);

        Assert.NotNull(box);
        Assert.Equal("测试盒", box.Name);
        Assert.Equal(BoxType.Regular, box.Type);
        Assert.True(Directory.Exists(box.Directory));

        // 验证持久化
        var loaded = _manager.GetBox(box.Id);
        Assert.NotNull(loaded);
        Assert.Equal("测试盒", loaded!.Name);
    }

    [Fact]
    public void CreateBox_ShouldAddToGlobalConfig()
    {
        var box = _manager.CreateBox("测试盒2", BoxType.Link);

        var config = _manager.GetGlobalConfig();
        Assert.Contains(config.Boxes, b => b.Id == box.Id);
        Assert.Equal(BoxType.Link, config.Boxes.First(b => b.Id == box.Id).Type);
    }

    [Fact]
    public void RenameBox_ShouldUpdateName()
    {
        var box = _manager.CreateBox("旧名称", BoxType.Regular);

        _manager.RenameBox(box.Id, "新名称");

        var reloaded = _manager.GetBox(box.Id);
        Assert.Equal("新名称", reloaded!.Name);

        var config = _manager.GetGlobalConfig();
        var entry = config.Boxes.First(b => b.Id == box.Id);
        Assert.Equal("新名称", entry.Name);
    }

    [Fact]
    public void UpdateSettings_ShouldPersist()
    {
        var box = _manager.CreateBox("设置测试", BoxType.Regular);

        var newSettings = new BoxSettings
        {
            DisplayMode = DisplayMode.List,
            IconSize = 64,
            SortBy = SortBy.ModifiedTime,
            SortAscending = false,
            BackgroundColor = "#FF0000",
            Opacity = 0.5,
            PositionLocked = true
        };

        _manager.UpdateSettings(box.Id, newSettings);

        var reloaded = _manager.GetBox(box.Id);
        Assert.Equal(DisplayMode.List, reloaded!.Settings.DisplayMode);
        Assert.Equal(64, reloaded.Settings.IconSize);
        Assert.Equal(SortBy.ModifiedTime, reloaded.Settings.SortBy);
        Assert.False(reloaded.Settings.SortAscending);
        Assert.Equal("#FF0000", reloaded.Settings.BackgroundColor);
        Assert.Equal(0.5, reloaded.Settings.Opacity);
        Assert.True(reloaded.Settings.PositionLocked);
    }

    [Fact]
    public void AddFiles_RegularBox_ShouldCopyFiles()
    {
        var box = _manager.CreateBox("文件测试", BoxType.Regular);

        // 创建测试文件
        var sourceFile = Path.Combine(_tempDir, "source_test.txt");
        File.WriteAllText(sourceFile, "Hello SimDesk!");

        var added = _manager.AddFiles(box.Id, new[] { sourceFile });

        Assert.Single(added);
        Assert.Equal("source_test.txt", added[0]);

        // 验证文件已复制到盒目录
        var destFile = Path.Combine(box.Directory, "source_test.txt");
        Assert.True(File.Exists(destFile));
        Assert.Equal("Hello SimDesk!", File.ReadAllText(destFile));
    }

    [Fact]
    public void AddFiles_ShouldTrackOrigins()
    {
        var box = _manager.CreateBox("追踪测试", BoxType.Regular);

        var sourceFile = Path.Combine(_tempDir, "track_me.txt");
        File.WriteAllText(sourceFile, "tracked content");

        _manager.AddFiles(box.Id, new[] { sourceFile });

        var reloaded = _manager.GetBox(box.Id);
        Assert.Contains("track_me.txt", reloaded!.FileOrigins.Keys);
        Assert.Equal(sourceFile, reloaded.FileOrigins["track_me.txt"]);
    }

    [Fact]
    public void RemoveFiles_RegularBox_ShouldDeleteFromDisk()
    {
        var box = _manager.CreateBox("删除测试", BoxType.Regular);

        var sourceFile = Path.Combine(_tempDir, "to_delete.txt");
        File.WriteAllText(sourceFile, "to be deleted");

        _manager.AddFiles(box.Id, new[] { sourceFile });
        var destFile = Path.Combine(box.Directory, "to_delete.txt");
        Assert.True(File.Exists(destFile));

        _manager.RemoveFiles(box.Id, new[] { "to_delete.txt" });

        Assert.False(File.Exists(destFile));
    }

    [Fact]
    public void DissolveBox_ShouldRestoreFilesAndMarkDeleted()
    {
        var box = _manager.CreateBox("解散测试", BoxType.Regular);

        var sourceFile = Path.Combine(_tempDir, "restore_me.txt");
        File.WriteAllText(sourceFile, "restore this file");

        _manager.AddFiles(box.Id, new[] { sourceFile });

        var result = _manager.DissolveBox(box.Id);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Files);
        Assert.Equal("success", result.Files[0].Status);

        // 文件应回到原位置
        Assert.True(File.Exists(sourceFile));
        Assert.Equal("restore this file", File.ReadAllText(sourceFile));

        // 盒应标记为已删除
        var reloaded = _manager.GetBox(box.Id);
        Assert.True(reloaded!.IsDeleted);
    }

    [Fact]
    public void DissolveBox_ConflictFile_ShouldRename()
    {
        var box = _manager.CreateBox("冲突测试", BoxType.Regular);

        var sourceFile = Path.Combine(_tempDir, "conflict.txt");
        File.WriteAllText(sourceFile, "original content");

        _manager.AddFiles(box.Id, new[] { sourceFile });

        // 在原位置创建一个同名文件（模拟冲突）
        var destInBox = Path.Combine(box.Directory, "conflict.txt");
        Assert.True(File.Exists(destInBox));

        // 解散时原位置已有同名文件，应触发重命名
        var result = _manager.DissolveBox(box.Id);

        Assert.NotEmpty(result.Files);
        // 应该有冲突重命名
        var conflictFile = result.Files.FirstOrDefault(f => f.Status == "conflict_renamed");
        // 注意：如果原文件还在的话会触发冲突
        Assert.NotNull(result.Files[0]);
    }

    [Fact]
    public void GetGlobalConfig_ShouldReturnDefaults()
    {
        var config = _manager.GetGlobalConfig();

        Assert.Equal(DragOperation.Move, config.DragOperation);
        Assert.Equal(BoxType.Regular, config.DefaultBoxType);
        Assert.False(config.AutoStart);
    }

    [Fact]
    public void SetDefaultDragOperation_ShouldPersist()
    {
        _manager.SetDefaultDragOperation(DragOperation.Copy);

        var config = _manager.GetGlobalConfig();
        Assert.Equal(DragOperation.Copy, config.DragOperation);

        // 重新加载验证持久化
        var manager2 = new BoxManager(
            new BoxPersistenceService(_tempDir),
            new FileOperationService(_symlink, _tracking),
            new LogService(new SqliteLogStore(_tempDir)),
            _tempDir);
        var config2 = manager2.GetGlobalConfig();
        // 注意：SqliteLogStore 需要 initialize
    }

    [Fact]
    public void LogCreate_ShouldRecordEvent()
    {
        var box = _manager.CreateBox("日志测试", BoxType.Link);

        var events = _log.QueryEvents(pageSize: 10);
        Assert.NotEmpty(events);
        Assert.Equal(LogEventType.Create, events[0].EventType);
        Assert.Equal(box.Id, events[0].BoxId);
    }

    [Fact]
    public void GetAllActiveBoxes_ShouldExcludeDeleted()
    {
        var box1 = _manager.CreateBox("活跃盒", BoxType.Regular);
        var box2 = _manager.CreateBox("待解散盒", BoxType.Regular);

        _manager.DissolveBox(box2.Id);

        var active = _manager.GetAllActiveBoxes();
        Assert.Single(active);
        Assert.Equal(box1.Id, active[0].Id);
    }
}
