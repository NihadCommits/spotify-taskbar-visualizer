using Microsoft.Win32;

namespace SpotifyTaskbarVisualizer;

// Per-user autostart entry, no admin rights needed. Points to the exe that is currently running.
internal static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SpotifyTaskbarVisualizer";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) != null;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value) key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
            else key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
