using System.Runtime.InteropServices;
using static SpotifyTaskbarVisualizer.NativeMethods;

namespace SpotifyTaskbarVisualizer;

// Primary taskbar geometry, all values in physical pixels.
internal readonly record struct TaskbarInfo(RECT Bounds, int TrayLeft);

internal static class TaskbarLocator
{
    public static TaskbarInfo? Get()
    {
        var data = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
        if (SHAppBarMessage(ABM_GETTASKBARPOS, ref data) == IntPtr.Zero) return null;

        var tray = FindWindowEx(FindWindow("Shell_TrayWnd", null), IntPtr.Zero, "TrayNotifyWnd", null);
        var trayLeft = tray != IntPtr.Zero && GetWindowRect(tray, out var rect) ? rect.Left : data.rc.Right;
        return new TaskbarInfo(data.rc, trayLeft);
    }
}
