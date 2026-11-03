using System.Runtime.InteropServices;
using System.Text;
using static SpotifyTaskbarVisualizer.NativeMethods;

namespace SpotifyTaskbarVisualizer;

internal static class FullscreenDetector
{
    // True if the foreground window covers the whole monitor of the taskbar.
    // Maximized windows leave the taskbar free and don't count.
    public static bool IsActive(IntPtr taskbar)
    {
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero || IsDesktop(window)) return false;

        var monitor = MonitorFromWindow(taskbar, MONITOR_DEFAULTTONEAREST);
        if (MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST) != monitor) return false;

        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info) || !GetWindowRect(window, out var rect)) return false;
        var screen = info.rcMonitor;
        return rect.Left <= screen.Left && rect.Top <= screen.Top && rect.Right >= screen.Right && rect.Bottom >= screen.Bottom;
    }

    // The desktop window is monitor-sized too.
    private static bool IsDesktop(IntPtr window)
    {
        var name = new StringBuilder(32);
        GetClassName(window, name, name.Capacity);
        return name.ToString() is "Progman" or "WorkerW";
    }
}
