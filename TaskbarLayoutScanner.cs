using System.Windows.Automation;
using static SpotifyTaskbarVisualizer.NativeMethods;

namespace SpotifyTaskbarVisualizer;

internal static class TaskbarLayoutScanner
{
    // Bounding rectangles (physical pixels) of every button on the taskbar: app icons, Start, search,
    // widgets and tray. Slow-ish and may throw while the taskbar changes: call off the UI thread.
    public static List<RECT> Scan(IntPtr taskbar)
    {
        var cache = new CacheRequest { AutomationElementMode = AutomationElementMode.None };
        cache.Add(AutomationElement.BoundingRectangleProperty);

        AutomationElementCollection buttons;
        using (cache.Activate())
        {
            buttons = AutomationElement.FromHandle(taskbar).FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        }

        var rects = new List<RECT>();
        foreach (AutomationElement button in buttons)
        {
            var r = button.Cached.BoundingRectangle;
            if (r.IsEmpty || r.Width <= 0) continue;
            rects.Add(new RECT { Left = (int)r.Left, Top = (int)r.Top, Right = (int)r.Right, Bottom = (int)r.Bottom });
        }
        return rects;
    }
}
