using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SimDesk.ShellExtension;

/// <summary>
/// COM 注册/注销辅助（SharpShell ComHost 模式）
/// </summary>
public static class ComRegistration
{
    private const string ShellExtensionGuid = "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}";

    /// <summary>
    /// 手动注册 Shell 扩展到相关注册表位置
    /// </summary>
    [ComRegisterFunction]
    public static void Register(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var guidString = type.GUID.ToString("B").ToUpper();

        // 注册到 Approved Extensions
        using var approvedKey = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", writable: true)
            ?? Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved");

        approvedKey?.SetValue(guidString, "SimDesk Context Menu Extension");
    }

    /// <summary>
    /// 注销 Shell 扩展
    /// </summary>
    [ComUnregisterFunction]
    public static void Unregister(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var guidString = type.GUID.ToString("B").ToUpper();

        try
        {
            using var approvedKey = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", writable: true);
            approvedKey?.DeleteValue(guidString, throwOnMissingValue: false);
        }
        catch
        {
            // 忽略注销失败
        }
    }

    /// <summary>
    /// 获取 shell 扩展的 GUID 字符串
    /// </summary>
    public static string GetGuid() => ShellExtensionGuid;
}
