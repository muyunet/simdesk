using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;

namespace SimDesk.App;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private InstanceManager? _instanceManager;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            WriteCrashLog(args.ExceptionObject as Exception);
        DispatcherUnhandledException += (s, args) =>
        {
            WriteCrashLog(args.Exception);
            args.Handled = true;
            Shutdown();
        };

        try
        {
            _instanceManager = new InstanceManager();
            if (!_instanceManager.TryBecomeMainInstance(e.Args))
            {
                Shutdown();
                return;
            }

            CreateTrayIcon();
        }
        catch (Exception ex)
        {
            WriteCrashLog(ex);
            MessageBox.Show($"启动失败:\n{ex.Message}\n\n详情:\n{GetAppDataPath()}\\crash.log",
                "SimDesk", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "SimDesk",
            MenuActivation = PopupActivationMode.RightClick,
            IconSource = CreateDefaultIcon()
        };

        var menu = new System.Windows.Controls.ContextMenu();

        var autoStart = new System.Windows.Controls.MenuItem { Header = "开机自启", IsCheckable = true };
        menu.Items.Add(autoStart);
        menu.Items.Add(new System.Windows.Controls.Separator());

        var defaultOpt = new System.Windows.Controls.MenuItem { Header = "默认选项" };
        var dragOp = new System.Windows.Controls.MenuItem { Header = "拖入操作" };
        dragOp.Items.Add(new System.Windows.Controls.MenuItem { Header = "复制", IsCheckable = true });
        dragOp.Items.Add(new System.Windows.Controls.MenuItem { Header = "移动", IsCheckable = true, IsChecked = true });
        defaultOpt.Items.Add(dragOp);
        var boxType = new System.Windows.Controls.MenuItem { Header = "默认盒子类型" };
        boxType.Items.Add(new System.Windows.Controls.MenuItem { Header = "常规收纳盒", IsCheckable = true, IsChecked = true });
        boxType.Items.Add(new System.Windows.Controls.MenuItem { Header = "链接收纳盒", IsCheckable = true });
        defaultOpt.Items.Add(boxType);
        menu.Items.Add(defaultOpt);
        menu.Items.Add(new System.Windows.Controls.Separator());

        var logItem = new System.Windows.Controls.MenuItem { Header = "打开日志" };
        logItem.Click += (_, _) => OpenLogViewer();
        menu.Items.Add(logItem);

        var aboutItem = new System.Windows.Controls.MenuItem { Header = "关于" };
        aboutItem.Click += (_, _) => MessageBox.Show(
            "SimDesk v0.1.0\nWindows 桌面整理工具\n\nhttps://github.com/muyunet/simdesk",
            "关于 SimDesk", MessageBoxButton.OK, MessageBoxImage.Information);
        menu.Items.Add(aboutItem);
        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "退出" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu = menu;
    }

    /// <summary>
    /// 用纯 WPF 渲染默认图标：蓝底白字 S
    /// </summary>
    private static ImageSource CreateDefaultIcon()
    {
        const int size = 32;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            // 蓝色圆形背景
            dc.DrawEllipse(
                new SolidColorBrush(Color.FromRgb(50, 150, 240)),
                null,
                new System.Windows.Point(size / 2.0, size / 2.0),
                size / 2.0 - 1, size / 2.0 - 1);

            // 白色 S 字母
            var text = new FormattedText("S",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                18,
                Brushes.White,
                pixelsPerDip: 1);
            dc.DrawText(text, new System.Windows.Point(
                (size - text.Width) / 2.0,
                (size - text.Height) / 2.0));
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private void OpenLogViewer()
    {
        var logPath = Path.Combine(GetAppDataPath(), "logs.db");
        if (File.Exists(logPath))
            Process.Start("explorer.exe", $"/select,\"{logPath}\"");
        else
            MessageBox.Show("暂无日志。", "SimDesk", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string GetAppDataPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SimDesk");
    }

    private static void WriteCrashLog(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            var dir = GetAppDataPath();
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
        }
        catch { }
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _instanceManager?.Dispose();
    }
}
