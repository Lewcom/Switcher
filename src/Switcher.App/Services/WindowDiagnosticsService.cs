using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Switcher.App.Services;

internal static class WindowDiagnosticsService
{
    public static string DescribeWindowContext(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero)
        {
            return "target=0x0";
        }

        var targetClass = GetWindowClassName(targetWindow);
        var targetTitle = AppLogger.Preview(GetWindowTitle(targetWindow), 80);

        _ = GetWindowThreadProcessId(targetWindow, out var processId);
        var processName = GetProcessName(processId);

        var focusedHandle = IntPtr.Zero;
        var focusedClass = "<none>";
        var focusedTitle = "<none>";
        var threadId = GetWindowThreadProcessId(targetWindow, out _);
        var info = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };
        if (threadId != 0 && GetGUIThreadInfo(threadId, ref info))
        {
            focusedHandle = info.hwndFocus;
            if (focusedHandle != IntPtr.Zero)
            {
                focusedClass = GetWindowClassName(focusedHandle);
                focusedTitle = AppLogger.Preview(GetWindowTitle(focusedHandle), 80);
            }
        }

        return
            $"target=0x{targetWindow.ToInt64():X} class={targetClass} title=\"{targetTitle}\" " +
            $"proc={processName} pid={processId} thread={threadId} " +
            $"focus=0x{focusedHandle.ToInt64():X} focus_class={focusedClass} focus_title=\"{focusedTitle}\"";
    }

    private static string GetProcessName(uint processId)
    {
        try
        {
            if (processId == 0)
            {
                return "<unknown>";
            }

            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return "<unknown>";
        }
    }

    private static string GetWindowClassName(IntPtr hWnd)
    {
        var buffer = new StringBuilder(256);
        return GetClassName(hWnd, buffer, buffer.Capacity) > 0 ? buffer.ToString() : "<unknown>";
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(length + 1);
        _ = GetWindowText(hWnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public int cbSize;
        public uint flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT rcCaret;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
