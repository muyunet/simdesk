using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SimDesk.Core.Models;
using SimDesk.Core.Services;

namespace SimDesk.App.Views;

/// <summary>
/// 日志查看器 — 展示所有收纳盒操作记录
/// </summary>
public partial class LogViewerWindow : Window
{
    private readonly ILogService _logService;
    private readonly IBoxManager _boxManager;
    private int _currentPage = 1;
    private const int PageSize = 50;

    public LogViewerWindow(ILogService logService, IBoxManager boxManager)
    {
        _logService = logService;
        _boxManager = boxManager;
        InitializeComponent();
        LoadBoxFilter();
        LoadEvents();
    }

    private void LoadBoxFilter()
    {
        var boxes = _boxManager.GetAllBoxes();
        foreach (var box in boxes)
        {
            BoxFilter.Items.Add(new ComboBoxItem
            {
                Content = box.Name,
                Tag = box.Id.ToString("D")
            });
        }
    }

    private void LoadEvents()
    {
        LogTreeView.Items.Clear();

        Guid? boxId = null;
        if (BoxFilter.SelectedItem is ComboBoxItem boxItem && boxItem.Tag is string boxTag)
            boxId = Guid.Parse(boxTag);

        LogEventType? eventType = null;
        if (EventTypeFilter.SelectedItem is ComboBoxItem typeItem && typeItem.Tag is string typeTag && !string.IsNullOrEmpty(typeTag))
            eventType = Enum.Parse<LogEventType>(typeTag);

        var events = _logService.QueryEvents(_currentPage, PageSize, boxId, eventType);

        foreach (var evt in events)
        {
            var eventNode = CreateEventNode(evt);
            LogTreeView.Items.Add(eventNode);
        }

        var total = _logService.GetEventCount(boxId, eventType);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)total / PageSize));
        PageLabel.Text = $"第 {_currentPage} 页 / 共 {totalPages} 页";
    }

    private TreeViewItem CreateEventNode(LogEvent evt)
    {
        var icon = evt.EventType switch
        {
            LogEventType.Create => "🟢",
            LogEventType.Add => "🔵",
            LogEventType.Remove => "🔴",
            LogEventType.Dissolve => "🟠",
            LogEventType.Delete => "⛔",
            _ => "⚪"
        };

        var timeStr = evt.Timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");

        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
        headerPanel.Children.Add(new TextBlock
        {
            Text = $"{icon} [{timeStr}] ",
            Foreground = new SolidColorBrush(Colors.Gray)
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = $"「{evt.BoxName}」",
            FontWeight = FontWeights.Bold
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = $" — {evt.Summary}",
            Foreground = new SolidColorBrush(Colors.DimGray)
        });

        var node = new TreeViewItem { Header = headerPanel, Tag = evt };

        // 如果是解散事件，展开时加载文件详情
        if (evt.EventType == LogEventType.Dissolve)
        {
            // 添加"展开查看详情"占位节点
            node.Items.Add(new TreeViewItem { Header = "加载中...", Tag = "placeholder" });
            node.Expanded += OnDissolveNodeExpanded;
        }

        return node;
    }

    private void OnDissolveNodeExpanded(object sender, RoutedEventArgs e)
    {
        var node = (TreeViewItem)sender;
        if (node.Tag is not LogEvent evt) return;

        // 如果已加载，跳过
        if (node.Items.Count == 1 && node.Items[0] is TreeViewItem placeholder && placeholder.Tag?.ToString() == "loaded")
            return;

        node.Items.Clear();

        var fileOps = _logService.QueryFileOperations(evt.Id);
        foreach (var fo in fileOps)
        {
            var statusIcon = fo.Status switch
            {
                "success" => "✅",
                "conflict_renamed" => "⚠️",
                "skipped" => "❌",
                _ => "❓"
            };

            var filePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(16, 1, 0, 1) };

            // 源路径（灰色，不可点击）
            filePanel.Children.Add(new TextBlock
            {
                Text = $"{statusIcon} {fo.FileName}",
                FontWeight = FontWeights.SemiBold,
                Width = 150
            });

            // 还原到路径（蓝色，可点击定位）
            if (!string.IsNullOrEmpty(fo.RestoredToPath))
            {
                var link = new TextBlock
                {
                    Text = $"→ {fo.RestoredToPath}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x73, 0xE8)),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    TextDecorations = TextDecorations.Underline,
                    Tag = fo.RestoredToPath
                };
                link.MouseLeftButtonDown += (s, args) =>
                {
                    var path = (string)((TextBlock)s).Tag;
                    LocateFile(path);
                };
                filePanel.Children.Add(link);
            }
            else if (fo.Status == "skipped")
            {
                filePanel.Children.Add(new TextBlock
                {
                    Text = " (跳过: 文件不存在)",
                    Foreground = new SolidColorBrush(Colors.Red)
                });
            }

            var fileNode = new TreeViewItem { Header = filePanel };
            node.Items.Add(fileNode);
        }

        // 标记已加载
        node.Items.Add(new TreeViewItem { Header = "", Tag = "loaded", Visibility = Visibility.Collapsed });
    }

    /// <summary>
    /// 在资源管理器中定位文件
    /// </summary>
    private static void LocateFile(string path)
    {
        try
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else
            {
                MessageBox.Show($"文件不存在:\n{path}", "定位失败",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开资源管理器:\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== 分页 =====

    private void OnPrevPage(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            LoadEvents();
        }
    }

    private void OnNextPage(object sender, RoutedEventArgs e)
    {
        _currentPage++;
        LoadEvents();
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentPage = 1;
        LoadEvents();
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        LoadEvents();
    }
}
