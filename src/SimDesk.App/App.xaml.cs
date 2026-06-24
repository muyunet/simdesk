using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace SimDesk.App;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private InstanceManager? _instanceManager;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // 单例检查 + IPC 启动
        _instanceManager = new InstanceManager();
        if (!_instanceManager.TryBecomeMainInstance(e.Args))
        {
            Shutdown();
            return;
        }

        // 在代码中创建托盘图标
        CreateTrayIcon();

        // 加载收纳盒列表
        InitializeBoxes();
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "SimDesk",
            MenuActivation = PopupActivationMode.RightClick,
            // IconSource = ... // TODO: 设置图标
        };

        // 构建托盘右键菜单
        var contextMenu = new System.Windows.Controls.ContextMenu();

        var autoStartItem = new System.Windows.Controls.MenuItem
        {
            Header = "开机自启",
            IsCheckable = true
        };
        contextMenu.Items.Add(autoStartItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        // 默认选项子菜单
        var defaultOptionsItem = new System.Windows.Controls.MenuItem { Header = "默认选项" };
        var dragOpItem = new System.Windows.Controls.MenuItem { Header = "拖入操作" };
        dragOpItem.Items.Add(new System.Windows.Controls.MenuItem { Header = "复制", IsCheckable = true });
        dragOpItem.Items.Add(new System.Windows.Controls.MenuItem { Header = "移动", IsCheckable = true });
        defaultOptionsItem.Items.Add(dragOpItem);

        var defaultTypeItem = new System.Windows.Controls.MenuItem { Header = "默认盒子类型" };
        defaultTypeItem.Items.Add(new System.Windows.Controls.MenuItem { Header = "常规收纳盒", IsCheckable = true });
        defaultTypeItem.Items.Add(new System.Windows.Controls.MenuItem { Header = "链接收纳盒", IsCheckable = true });
        defaultOptionsItem.Items.Add(defaultTypeItem);
        contextMenu.Items.Add(defaultOptionsItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        var logItem = new System.Windows.Controls.MenuItem { Header = "打开日志" };
        logItem.Click += (s, e) => OpenLogViewer();
        contextMenu.Items.Add(logItem);

        var aboutItem = new System.Windows.Controls.MenuItem { Header = "关于" };
        aboutItem.Click += (s, e) => OpenAbout();
        contextMenu.Items.Add(aboutItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "退出" };
        exitItem.Click += (s, e) => Shutdown();
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;
    }

    private void InitializeBoxes()
    {
        // TODO: Phase 2 — 从 Core 加载收纳盒并创建悬浮窗
    }

    private void OpenLogViewer()
    {
        // TODO: Phase 6 — 打开日志查看器窗口
    }

    private void OpenAbout()
    {
        // TODO: Phase 2 — 打开关于窗口
        MessageBox.Show("SimDesk v0.1.0\nWindows 桌面整理工具", "关于 SimDesk",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _instanceManager?.Dispose();
    }
}
