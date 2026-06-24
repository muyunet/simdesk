using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SimDesk.App.Native;
using SimDesk.Core.Models;

namespace SimDesk.App.Views;

/// <summary>
/// 收纳盒悬浮窗 — 核心 UI
/// </summary>
public partial class StorageBoxWindow : Window
{
    private readonly StorageBox _box;
    private bool _isCollapsed;
    private double _expandedHeight;

    public StorageBoxWindow(StorageBox box)
    {
        _box = box;
        DataContext = this;
        InitializeComponent();

        _expandedHeight = box.WindowPosition.Height;
        ApplySettings();
    }

    private void ApplySettings()
    {
        Opacity = _box.Settings.Opacity;
        ApplyBackground();
    }

    public void ApplyBackground()
    {
        // 背景色
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(_box.Settings.BackgroundColor);
            BackgroundBrush = new SolidColorBrush(color);
        }
        catch
        {
            BackgroundBrush = new SolidColorBrush(Colors.WhiteSmoke);
        }

        // 背景图
        if (!string.IsNullOrEmpty(_box.Settings.BackgroundImagePath))
        {
            try
            {
                ContentBackgroundBrush = new ImageBrush(
                    new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(_box.Settings.BackgroundImagePath)));
            }
            catch
            {
                ContentBackgroundBrush = null;
            }
        }
        else
        {
            ContentBackgroundBrush = null;
        }
    }

    // ===== 依赖属性 =====

    public new string Name
    {
        get => _box.Name;
        set => _box.Name = value;
    }

    public WindowPosition WindowPosition => _box.WindowPosition;

    public static readonly DependencyProperty BackgroundBrushProperty =
        DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(StorageBoxWindow));
    public Brush? BackgroundBrush
    {
        get => (Brush?)GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    public static readonly DependencyProperty ContentBackgroundBrushProperty =
        DependencyProperty.Register(nameof(ContentBackgroundBrush), typeof(Brush), typeof(StorageBoxWindow));
    public Brush? ContentBackgroundBrush
    {
        get => (Brush?)GetValue(ContentBackgroundBrushProperty);
        set => SetValue(ContentBackgroundBrushProperty, value);
    }

    // ===== 窗口拖动 =====

    private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_box.Settings.PositionLocked)
            DragMove();
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_box.Settings.PositionLocked)
        {
            if (e.ClickCount == 2)
            {
                // 双击改名
                StartNameEdit();
            }
            else if (e.OriginalSource is not Button)
            {
                DragMove();
            }
        }
    }

    // ===== 名称编辑 =====

    private void StartNameEdit()
    {
        BoxNameEdit.Text = BoxNameText.Text;
        BoxNameText.Visibility = Visibility.Collapsed;
        BoxNameEdit.Visibility = Visibility.Visible;
        BoxNameEdit.Focus();
        BoxNameEdit.SelectAll();
    }

    private void OnNameEditKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            CommitNameEdit();
        else if (e.Key == Key.Escape)
            CancelNameEdit();
    }

    private void OnNameEditLostFocus(object sender, RoutedEventArgs e)
    {
        CommitNameEdit();
    }

    private void CommitNameEdit()
    {
        var newName = BoxNameEdit.Text.Trim();
        if (!string.IsNullOrEmpty(newName))
        {
            BoxNameText.Text = newName;
            // TODO: 调用 BoxManager.RenameBox()
        }
        BoxNameEdit.Visibility = Visibility.Collapsed;
        BoxNameText.Visibility = Visibility.Visible;
    }

    private void CancelNameEdit()
    {
        BoxNameEdit.Visibility = Visibility.Collapsed;
        BoxNameText.Visibility = Visibility.Visible;
    }

    // ===== 设置按钮 =====

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var popup = new SettingsPopup(_box, this)
        {
            PlacementTarget = sender as UIElement
        };
        popup.IsOpen = true;
    }

    // ===== 折叠/展开 =====

    private void OnCollapseClick(object sender, RoutedEventArgs e)
    {
        _isCollapsed = !_isCollapsed;

        if (_isCollapsed)
        {
            _expandedHeight = Height;
            var anim = new System.Windows.Media.Animation.DoubleAnimation(
                TitleBar.ActualHeight + 2,
                TimeSpan.FromMilliseconds(200));
            BeginAnimation(HeightProperty, anim);
            CollapseButton.Content = "▼";
        }
        else
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation(
                _expandedHeight,
                TimeSpan.FromMilliseconds(200));
            BeginAnimation(HeightProperty, anim);
            CollapseButton.Content = "▲";
        }
    }

    // ===== 拖放文件 =====

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // 通过 Win32 API 注册拖放（绕过 AllowsTransparency 的限制）
        var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        NativeMethods.DragAcceptFiles(handle, true);
        var source = System.Windows.Interop.HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_DROPFILES)
        {
            HandleDropFiles(wParam);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void HandleDropFiles(IntPtr hDrop)
    {
        var fileCount = NativeMethods.DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
        var files = new List<string>();

        var sb = new System.Text.StringBuilder(260);
        for (uint i = 0; i < fileCount; i++)
        {
            sb.Clear();
            NativeMethods.DragQueryFile(hDrop, i, sb, (uint)sb.Capacity);
            files.Add(sb.ToString());
        }

        NativeMethods.DragFinish(hDrop);

        // TODO: 调用 Core 添加文件
        System.Diagnostics.Debug.WriteLine($"拖入 {fileCount} 个文件");
    }
}
