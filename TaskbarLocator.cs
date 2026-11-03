using System.Runtime.InteropServices;
using static SpotifyTaskbarVisualizer.NativeMethods;

namespace SpotifyTaskbarVisualizer;

// Primary taskbar geometry, all values in physical pixels.
internal readonly record struct TaskbarInfo(IntPtr Handle, RECT Bounds, int TrayLeft, bool IsBottom, bool IsAutoHide);

internal static class TaskbarLocator
{
    public static TaskbarInfo? Get()
    {
        var data = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
        if (SHAppBarMessage(ABM_GETTASKBARPOS, ref data) == IntPtr.Zero) return null;
        var autoHide = ((long)SHAppBarMessage(ABM_GETSTATE, ref data) & ABS_AUTOHIDE) != 0;

        var taskbar = FindWindow("Shell_TrayWnd", null);
        var tray = FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        var trayLeft = tray != IntPtr.Zero && GetWindowRect(tray, out var rect) ? rect.Left : data.rc.Right;
        return new TaskbarInfo(taskbar, data.rc, trayLeft, data.uEdge == ABE_BOTTOM, autoHide);
    }
}
