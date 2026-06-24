using System.Runtime.InteropServices;

namespace SimDesk.App.Native;

/// <summary>
/// Win32 API P/Invoke 声明
/// </summary>
internal static class NativeMethods
{
    // 窗口样式
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_LAYERED = 0x80000;
    public const int WS_EX_TRANSPARENT = 0x20;
    public const int WS_EX_TOOLWINDOW = 0x80;
    public const int WS_EX_APPWINDOW = 0x40000;

    // 窗口消息
    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int WM_DROPFILES = 0x233;
    public const int HTCAPTION = 2;

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("shell32.dll")]
    public static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);

    [DllImport("shell32.dll")]
    public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, System.Text.StringBuilder? lpszFile, uint cch);

    [DllImport("shell32.dll")]
    public static extern void DragFinish(IntPtr hDrop);

    [DllImport("shell32.dll")]
    public static extern bool DragQueryPoint(IntPtr hDrop, out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X, Y;
    }
}
