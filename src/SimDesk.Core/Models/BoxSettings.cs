namespace SimDesk.Core.Models;

/// <summary>
/// 单个收纳盒的设置（仅对该盒生效）
/// </summary>
public class BoxSettings
{
    // === 显示方式 ===
    public DisplayMode DisplayMode { get; set; } = DisplayMode.Icons;
    public int IconSize { get; set; } = 48;

    // === 排序方式 ===
    public SortBy SortBy { get; set; } = SortBy.Name;
    public bool SortAscending { get; set; } = true;

    // === 外观 ===
    public string BackgroundColor { get; set; } = "#F0F0F0";
    public string? BackgroundImagePath { get; set; } = null;
    public double Opacity { get; set; } = 0.92;

    // === 行为 ===
    public bool PositionLocked { get; set; } = false;
    public bool IsHidden { get; set; } = false;
}
