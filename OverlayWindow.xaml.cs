using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Media;
using static SpotifyTaskbarVisualizer.NativeMethods;

namespace SpotifyTaskbarVisualizer;

public partial class OverlayWindow : Window
{
    private const double IconMargin = 8;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _layoutTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private MediaService? _media;
    private ZOrderGuard? _zOrder;
    private TrayIcon? _trayIcon;
    private DebugOverlay? _debug;
    private List<RECT> _occupied = [];
    private WidgetMode _mode = WidgetMode.Full;
    private bool _scanning;

    public OverlayWindow()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => RenderPosition();
        _layoutTimer.Tick += async (_, _) =>
        {
            await ScanAsync();
            UpdatePlacement();
            _zOrder?.BringToFront(); // Fallback in case a foreground event was missed.
        };
        SourceInitialized += (_, _) =>
        {
            // Tool window: no Alt+Tab entry. No-activate: clicks never take focus from the active app.
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
            _zOrder = new ZOrderGuard(hwnd);
            _zOrder.ForegroundChanged += UpdatePlacement;
            UpdatePlacement();
        };
        Closed += (_, _) =>
        {
            _zOrder?.Dispose();
            _trayIcon?.Dispose();
        };
        SizeChanged += (_, _) => UpdatePlacement();
        DpiChanged += (_, _) => UpdatePlacement();
        Loaded += async (_, _) =>
        {
            // Close after the tray menu's click handler has returned, since closing disposes the menu.
            _trayIcon = new TrayIcon(ToggleDebug, () => Dispatcher.InvokeAsync(Close));
            await ScanAsync();
            UpdatePlacement();
            _layoutTimer.Start();
            _media = await MediaService.CreateAsync();
            _media.Changed += Render;
            Render();
        };
    }

    // Keeps the last known layout if a scan fails, e.g. while the taskbar is changing.
    private async Task ScanAsync()
    {
        if (_scanning || TaskbarLocator.Get() is not { } bar) return;
        _scanning = true;
        try { _occupied = await Task.Run(() => TaskbarLayoutScanner.Scan(bar.Handle)); }
        catch (Exception) { }
        finally { _scanning = false; }
    }

    // Picks mode and position from the free taskbar space. The only place where physical pixels
    // from Win32 are converted to WPF DIPs and back.
    private void UpdatePlacement()
    {
        var bar = TaskbarLocator.Get();
        if (bar is not { IsBottom: true, IsAutoHide: false } b || FullscreenDetector.IsActive(b.Handle))
        {
            Visibility = Visibility.Hidden;
            _debug?.Hide();
            return;
        }

        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
        int ToPixels(double dips) => (int)Math.Ceiling(dips * dpi.DpiScaleX);
        Root.Height = b.Bounds.Height / dpi.DpiScaleY;

        // TrayNotifyWnd counts as occupied in case UI Automation misses the tray buttons.
        var occupied = _occupied.Append(new RECT { Left = b.TrayLeft, Top = b.Bounds.Top, Right = b.Bounds.Right, Bottom = b.Bounds.Bottom }).ToList();
        var full = ToPixels(MeasureWidth(WidgetMode.Full));
        var compact = ToPixels(MeasureWidth(WidgetMode.Compact));
        var placement = PlacementEngine.Choose(b.Bounds, occupied, full, compact, ToPixels(IconMargin));

        _mode = placement.Mode;
        ApplyVisibility();
        Visibility = _mode == WidgetMode.Hidden ? Visibility.Hidden : Visibility.Visible;
        Top = b.Bounds.Top / dpi.DpiScaleY;
        Left = placement.Left / dpi.DpiScaleX;

        RECT? widget = _mode == WidgetMode.Hidden ? null : new RECT
        {
            Left = placement.Left, Top = b.Bounds.Top, Bottom = b.Bounds.Bottom,
            Right = placement.Left + (_mode == WidgetMode.Full ? full : compact),
        };
        _debug?.Draw(b.Bounds, occupied, widget, dpi.DpiScaleX);
    }

    private double MeasureWidth(WidgetMode mode)
    {
        _mode = mode;
        ApplyVisibility();
        Root.Measure(new Size(double.PositiveInfinity, Root.Height));
        return Root.DesiredSize.Width;
    }

    // Compact keeps only the title and previous/play/next.
    private void ApplyVisibility()
    {
        var hasSession = _media?.HasSession == true;
        var full = _mode == WidgetMode.Full;
        Player.Visibility = Show(hasSession);
        EmptyText.Visibility = Show(!hasSession);
        CoverImage.Visibility = ArtistText.Visibility = Show(full);
        PositionText.Visibility = SeekArea.Visibility = DurationText.Visibility = Show(full);
        ShuffleButton.Visibility = Show(full && _media?.CanShuffle == true);
        RepeatButton.Visibility = Show(full && _media?.CanRepeat == true);
    }

    private static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;

    private void ToggleDebug(bool on)
    {
        if (on)
        {
            _debug = new DebugOverlay();
            UpdatePlacement();
        }
        else
        {
            _debug?.Close();
            _debug = null;
        }
    }

    private void Render()
    {
        var media = _media!;
        ApplyVisibility();

        CoverImage.Source = media.Cover;
        TitleText.Text = media.Title;
        ArtistText.Text = media.Artist;
        PlayPauseButton.Content = media.IsPlaying ? "" : "";

        ShuffleButton.Opacity = media.IsShuffle ? 1 : 0.5;
        RepeatButton.Content = media.RepeatMode == MediaPlaybackAutoRepeatMode.Track ? "" : "";
        RepeatButton.Opacity = media.RepeatMode == MediaPlaybackAutoRepeatMode.None ? 0.5 : 1;

        DurationText.Text = Format(media.Duration);
        SeekArea.Cursor = media.CanSeek ? Cursors.Hand : null;
        _timer.IsEnabled = media.IsPlaying;
        RenderPosition();
    }

    private void RenderPosition()
    {
        var media = _media!;
        var position = media.GetPosition();
        PositionText.Text = Format(position);
        Progress.Value = media.Duration > TimeSpan.Zero ? position / media.Duration : 0;
    }

    private static string Format(TimeSpan time) => $"{(int)time.TotalMinutes}:{time.Seconds:00}";

    private void PlayPause_Click(object sender, RoutedEventArgs e) => _media?.TogglePlayPause();
    private void Next_Click(object sender, RoutedEventArgs e) => _media?.Next();
    private void Previous_Click(object sender, RoutedEventArgs e) => _media?.Previous();
    private void Shuffle_Click(object sender, RoutedEventArgs e) => _media?.ToggleShuffle();
    private void Repeat_Click(object sender, RoutedEventArgs e) => _media?.CycleRepeat();

    private void SeekArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        _media?.Seek(e.GetPosition(SeekArea).X / SeekArea.ActualWidth);

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
