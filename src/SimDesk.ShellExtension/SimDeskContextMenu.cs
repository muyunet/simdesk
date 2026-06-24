using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace SimDesk.ShellExtension;

/// <summary>
/// SimDesk 右键菜单扩展（通过 SharpShell COM）
/// </summary>
[ComVisible(true)]
[Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")] // 生产环境需更换唯一 GUID
[COMServerAssociation(AssociationType.AllFiles)]
[COMServerAssociation(AssociationType.Directory)]
[COMServerAssociation(AssociationType.DirectoryBackground)]
public class SimDeskContextMenu : SharpContextMenu
{
    protected override bool CanShowMenu()
    {
        return true;
    }

    protected override ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        var explorer = new SimDeskContextMenuBuilder(menu);

        var selectedPaths = SelectedItemPaths.ToList();

        if (selectedPaths.Count == 0)
        {
            // 桌面空白处：新建空收纳盒
            explorer.AddItem("新建空收纳盒", CreateEmptyBox);
        }
        else if (selectedPaths.Count == 1 && Directory.Exists(selectedPaths[0]))
        {
            // 单选文件夹：作为新收纳盒
            explorer.AddItem("作为新收纳盒", () => CreateBoxFromFolder(selectedPaths[0]));
            explorer.AddSeparator();
            explorer.AddItem("新建空收纳盒", CreateEmptyBox);
        }
        else
        {
            // 单选或多选文件：新建收纳盒（含所选文件）
            var fileNames = string.Join("、", selectedPaths.Select(p => Path.GetFileName(p)).Take(3));
            var more = selectedPaths.Count > 3 ? $" 等{selectedPaths.Count}个" : "";
            explorer.AddItem($"新建收纳盒（含: {fileNames}{more}）",
                () => CreateBoxWithFiles(selectedPaths));

            if (selectedPaths.Count == 1 && Directory.Exists(selectedPaths[0]))
            {
                // 如果单选的是文件夹，也提供"作为新收纳盒"选项
            }
        }

        return menu;
    }

    private void CreateEmptyBox()
    {
        SendCommand("--action create-empty-box");
    }

    private void CreateBoxWithFiles(List<string> files)
    {
        var filesArg = string.Join("|", files);
        SendCommand($"--action create-box-with-files --files \"{filesArg}\"");
    }

    private void CreateBoxFromFolder(string folderPath)
    {
        SendCommand($"--action create-box-from-folder --path \"{folderPath}\"");
    }

    private void SendCommand(string arguments)
    {
        try
        {
            var appPath = GetAppPath();
            Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                Arguments = arguments,
                UseShellExecute = false
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法启动 SimDesk: {ex.Message}", "SimDesk",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string GetAppPath()
    {
        // 尝试常见安装位置
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SimDesk", "SimDesk.App.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimDesk", "SimDesk.App.exe"),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        // 回退到同目录
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimDesk.App.exe");
    }
}

/// <summary>
/// 简化 ContextMenuStrip 构建
/// </summary>
internal class SimDeskContextMenuBuilder
{
    private readonly ContextMenuStrip _menu;

    public SimDeskContextMenuBuilder(ContextMenuStrip menu)
    {
        _menu = menu;
    }

    public void AddItem(string text, Action action)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (s, e) => action();
        _menu.Items.Add(item);
    }

    public void AddSeparator()
    {
        _menu.Items.Add(new ToolStripSeparator());
    }
}
