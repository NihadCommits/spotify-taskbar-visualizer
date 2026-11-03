using System.Windows;
using System.Windows.Interop;

namespace SpotifyTaskbarVisualizer;

public partial class App : Application
{
    // Creates the overlay without showing it: it waits hidden in the background until Spotify plays.
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindow = new OverlayWindow();
        new WindowInteropHelper(MainWindow).EnsureHandle();
    }
}
