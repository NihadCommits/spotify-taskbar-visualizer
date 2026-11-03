using static SpotifyTaskbarVisualizer.NativeMethods;

namespace SpotifyTaskbarVisualizer;

internal enum WidgetMode { Full, Compact, Hidden }

internal readonly record struct Placement(WidgetMode Mode, int Left);

internal static class PlacementEngine
{
    // All values in physical pixels. Prefers the gap next to the tray, falls back to the leftmost gap,
    // and picks the biggest mode that fits with the margin kept on both sides.
    public static Placement Choose(RECT bar, IEnumerable<RECT> occupied, int fullWidth, int compactWidth, int margin)
    {
        var gaps = new List<(int Left, int Right)>();
        var x = bar.Left;
        foreach (var r in occupied
            .Where(r => r.Right > bar.Left && r.Left < bar.Right && r.Bottom > bar.Top && r.Top < bar.Bottom)
            .OrderBy(r => r.Left))
        {
            if (r.Left > x) gaps.Add((x, r.Left));
            x = Math.Max(x, r.Right);
        }
        if (bar.Right > x) gaps.Add((x, bar.Right));
        if (gaps.Count == 0) return new Placement(WidgetMode.Hidden, 0);

        var candidates = new[] { (Gap: gaps[^1], AlignRight: true), (Gap: gaps[0], AlignRight: false) };
        foreach (var (mode, width) in new[] { (WidgetMode.Full, fullWidth), (WidgetMode.Compact, compactWidth) })
        {
            foreach (var (gap, alignRight) in candidates)
            {
                int left = gap.Left + margin, right = gap.Right - margin;
                if (right - left >= width) return new Placement(mode, alignRight ? right - width : left);
            }
        }
        return new Placement(WidgetMode.Hidden, 0);
    }
}
