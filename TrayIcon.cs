using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace SpotifyTaskbarVisualizer;

// Always reachable, even while the widget itself is hidden.
internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;

    public TrayIcon(Action<bool> toggleDebug, Action exit)
    {
        var autostart = new ToolStripMenuItem("Start with Windows") { CheckOnClick = true, Checked = Autostart.IsEnabled };
        autostart.CheckedChanged += (_, _) => Autostart.IsEnabled = autostart.Checked;

        var debug = new ToolStripMenuItem("Debug overlay") { CheckOnClick = true };
        debug.CheckedChanged += (_, _) => toggleDebug(debug.Checked);

        var menu = new ContextMenuStrip();
        menu.Items.Add(autostart);
        menu.Items.Add(debug);
        menu.Items.Add("Exit", null, (_, _) => exit());

        _icon = new NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "Spotify Taskbar Visualizer",
            ContextMenuStrip = menu,
            Visible = true,
        };
    }

    public void Dispose()
    {
        _icon.Visible = false; // Removes the icon right away instead of leaving a ghost until hovered.
        _icon.ContextMenuStrip?.Dispose();
        var icon = _icon.Icon;
        _icon.Dispose();
        icon?.Dispose();
    }

    // Green circle with a white double note at tray size, using the same icon font as the widget buttons.
    private static Icon CreateIcon()
    {
        var size = SystemInformation.SmallIconSize.Width;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using var circle = new SolidBrush(Color.FromArgb(0x1E, 0xD7, 0x60));
            g.FillEllipse(circle, 0, 0, size, size);

            var family = FontFamily.Families.Any(f => f.Name == "Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";
            using var font = new Font(family, size * 0.7f, GraphicsUnit.Pixel);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(char.ConvertFromUtf32(0xE8D6), font, Brushes.White, new RectangleF(0, 0, size, size), format);
        }

        // The clone owns its own handle, so the temporary one can be released right away.
        var handle = bitmap.GetHicon();
        try { return (Icon)Icon.FromHandle(handle).Clone(); }
        finally { NativeMethods.DestroyIcon(handle); }
    }
}
