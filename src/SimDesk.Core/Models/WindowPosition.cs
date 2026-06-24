namespace SimDesk.Core.Models;

/// <summary>
/// 悬浮窗位置和尺寸
/// </summary>
public class WindowPosition
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;
    public int Width { get; set; } = 400;
    public int Height { get; set; } = 500;
    public bool IsCollapsed { get; set; } = false;
}
