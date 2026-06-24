using Microsoft.Win32;

namespace SimDesk.Core.Services;

/// <summary>
/// 开机自启服务接口
/// </summary>
public interface IStartupService
{
    /// <summary>是否已设置为开机自启</summary>
    bool IsAutoStartEnabled();

    /// <summary>启用开机自启</summary>
    void EnableAutoStart(string appPath);

    /// <summary>禁用开机自启</summary>
    void DisableAutoStart();
}

/// <summary>
/// 通过注册表 HKCU\...\Run 管理开机自启
/// </summary>
public class StartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SimDesk";

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) != null;
        }
        catch (PlatformNotSupportedException)
        {
            // 在 Linux 上运行时返回 false
            return false;
        }
    }

    public void EnableAutoStart(string appPath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(ValueName, $"\"{appPath}\" --minimized");
    }

    public void DisableAutoStart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key != null)
        {
            try { key.DeleteValue(ValueName, throwOnMissingValue: false); }
            catch { /* 忽略 */ }
        }
    }
}
