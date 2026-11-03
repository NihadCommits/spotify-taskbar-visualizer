using System.Drawing;
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
            Icon = SystemIcons.Application,
            Text = "Spotify Taskbar Visualizer",
            ContextMenuStrip = menu,
            Visible = true,
        };
    }

    public void Dispose()
    {
        _icon.Visible = false; // Removes the icon right away instead of leaving a ghost until hovered.
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
    }
}
