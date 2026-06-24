using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SimDesk.Core.Models;

namespace SimDesk.App.Views;

/// <summary>
/// 设置弹出面板 — 不弹新窗口，附着在设置按钮上
/// </summary>
public partial class SettingsPopup : Popup
{
    private readonly StorageBox _box;
    private readonly StorageBoxWindow _parentWindow;

    public SettingsPopup(StorageBox box, StorageBoxWindow parentWindow)
    {
        _box = box;
        _parentWindow = parentWindow;
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var s = _box.Settings;

        // 显示方式
        IconsModeRb.IsChecked = s.DisplayMode == DisplayMode.Icons;
        ListModeRb.IsChecked = s.DisplayMode == DisplayMode.List;
        IconSizeBox.Text = s.IconSize.ToString();

        // 外观
        OpacitySlider.Value = s.Opacity;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(s.BackgroundColor);
            ColorPreview.Background = new SolidColorBrush(color);
        }
        catch { }

        // 行为
        PositionLockedCb.IsChecked = s.PositionLocked;
    }

    // ===== 取色 =====

    private void OnPickColor(object sender, RoutedEventArgs e)
    {
        // 使用 WPF 内置颜色选择（简化版）
        // 生产环境可替换为 ColorPicker 控件
        var colors = new[]
        {
            "#F0F0F0", "#FFFFFF", "#E8F5E9", "#E3F2FD", "#FFF3E0",
            "#FFEBEE", "#F3E5F5", "#E0F7FA", "#FFF9C4", "#EFEBE9",
            "#FFCDD2", "#C8E6C9", "#BBDEFB", "#FFE0B2", "#D1C4E9",
            "#B2EBF2", "#F0F4C3", "#D7CCC8", "#90CAF9", "#A5D6A7"
        };

        var menu = new ContextMenu();
        var wrapPanel = new WrapPanel { Width = 200, Margin = new Thickness(4) };

        foreach (var hex in colors)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            var border = new Border
            {
                Width = 24,
                Height = 24,
                Background = new SolidColorBrush(color),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                Tag = hex
            };
            border.MouseLeftButtonDown += (s, args) =>
            {
                var selectedHex = (string)((Border)s!).Tag;
                var selectedColor = (Color)ColorConverter.ConvertFromString(selectedHex)!;
                ColorPreview.Background = new SolidColorBrush(selectedColor);
                _box.Settings.BackgroundColor = selectedHex;
                _parentWindow.ApplyBackground();
                menu.IsOpen = false;
            };
            wrapPanel.Children.Add(border);
        }

        var host = new Border { Child = wrapPanel, Padding = new Thickness(4) };
        menu.Items.Add(new MenuItem { Header = host, StaysOpenOnClick = true });
        menu.PlacementTarget = ColorPreview;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    // ===== 背景图 =====

    private void OnPickImage(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择背景图片",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            _box.Settings.BackgroundImagePath = dlg.FileName;
            _parentWindow.ApplyBackground();
        }
    }

    private void OnClearImage(object sender, RoutedEventArgs e)
    {
        _box.Settings.BackgroundImagePath = null;
        _parentWindow.ApplyBackground();
    }

    // ===== 透明度 =====

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var pct = (int)(e.NewValue * 100);
        OpacityLabel.Text = $"{pct}%";
        _box.Settings.Opacity = e.NewValue;
        _parentWindow.Opacity = e.NewValue;
    }

    // ===== 隐藏 =====

    private void OnHideBox(object sender, RoutedEventArgs e)
    {
        _box.Settings.IsHidden = true;
        _parentWindow.Hide();
    }

    // ===== 解散（二次确认）=====

    private void OnDissolveBox(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"确定要解散收纳盒「{_box.Name}」吗？\n\n" +
            "所有文件将被还原到原始位置，此操作不可撤销。",
            "确认解散",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result == MessageBoxResult.Yes)
        {
            // TODO: 调用 BoxManager.DissolveBox()
            _parentWindow.Close();
        }
    }

    // ===== 保持设置 =====

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        SaveSettings();
    }

    private void SaveSettings()
    {
        var s = _box.Settings;

        // 显示方式
        s.DisplayMode = IconsModeRb.IsChecked == true ? DisplayMode.Icons : DisplayMode.List;
        if (int.TryParse(IconSizeBox.Text, out var size) && size >= 16 && size <= 256)
            s.IconSize = size;
        s.PositionLocked = PositionLockedCb.IsChecked == true;

        // TODO: 调用 BoxManager.UpdateSettings()
    }
}
