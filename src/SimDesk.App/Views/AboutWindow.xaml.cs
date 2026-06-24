using System.Windows;

namespace SimDesk.App.Views;

/// <summary>
/// 关于窗口
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
