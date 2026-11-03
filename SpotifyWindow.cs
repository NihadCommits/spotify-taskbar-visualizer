using System.Diagnostics;
using static SpotifyTaskbarVisualizer.NativeMethods;

namespace SpotifyTaskbarVisualizer;

internal static class SpotifyWindow
{
    // Called from a click on the widget: our process just received input, so Windows lets it set the foreground.
    public static void Activate()
    {
        var processes = Process.GetProcessesByName("Spotify");
        var window = processes.Select(p => p.MainWindowHandle).FirstOrDefault(h => h != IntPtr.Zero);
        foreach (var process in processes) process.Dispose();

        if (window != IntPtr.Zero)
        {
            if (IsIconic(window)) ShowWindow(window, SW_RESTORE);
            SetForegroundWindow(window);
            return;
        }

        // No visible window (closed to the tray): the protocol handler makes Spotify show itself.
        try { Process.Start(new ProcessStartInfo("spotify:") { UseShellExecute = true })?.Dispose(); }
        catch (Exception) { }
    }
}
